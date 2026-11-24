using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Sc.Game.Telemetry.V1;
using StarBridger.Contracts.Core.Shell.Definitions;
using StarBridger.Contracts.Enums;
using StarBridger.Contracts.Telemetry;
using StarBridger.Contracts.Telemetry.Game;
using StarBridger.Core.Managers;
using StarBridger.Core.Services;
using StarBridger.Telemetry.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;
using Game = Sc.Game.Telemetry.V1;

namespace StarBridger.Telemetry;

public sealed class StarCitizenTelemetryService : BackgroundService
{
  private const string GRPC_ADDRESS = "http://127.0.0.1:2942";
  private const int RETRY_DELAY_MS = 3000;

  private readonly ILogger<StarCitizenTelemetryService> _logger;
  private readonly StarBridgerShell _shell;
  private readonly object _extractLock = new();
  private VehicleFrame? _lastVehicleFrame;
  private ActorFrame? _lastActorFrame;
  private WeaponsFrame? _lastWeaponsFrame;
  private DateTimeOffset? _lastTelemetryAtUtc;

  private readonly object _reconnectLock = new();
  private CancellationTokenSource? _activeAttempt;

  private DateTimeOffset? _lastConnectedAtUtc;
  private DateTimeOffset _lastConnectionPublishAtUtc;


  public StateStore<VehicleTelemetry> Vehicle { get; }
  public StateStore<ActorTelemetry> Actor { get; }
  public StateStore<WeaponsTelemetry> Weapons { get; }
  public StateStore<StarCitizenConnectionInfo> Connection { get; }

  public StarCitizenTelemetryService(ILogger<StarCitizenTelemetryService> logger, StarBridgerShell shell)
  {
    _logger = logger;
    _shell = shell;
    Vehicle = new StateStore<VehicleTelemetry>(new VehicleTelemetry(), logger);
    Actor = new StateStore<ActorTelemetry>(new ActorTelemetry(), logger);
    Weapons = new StateStore<WeaponsTelemetry>(new WeaponsTelemetry(), logger);
    Connection = new StateStore<StarCitizenConnectionInfo>(
      new StarCitizenConnectionInfo
      {
        Status = StarCitizenConnectionStatus.Disconnected,
        Endpoint = GRPC_ADDRESS
      },
      logger);
    AddShellCommands();
  }

  #region Service Execution


  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      using CancellationTokenSource attempt = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

      lock (_reconnectLock)
      {
        _activeAttempt = attempt;
      }

      try
      {
        PublishConnectionStatus(StarCitizenConnectionStatus.Connecting);

        try
        {
          await StreamTelemetryAsync(attempt.Token);

          if (!attempt.IsCancellationRequested)
          {
            PublishConnectionStatus(StarCitizenConnectionStatus.Disconnected, "Telemetry stream ended.");
          }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
          // Normal host shutdown.
        }
        catch (RpcException) when (stoppingToken.IsCancellationRequested)
        {
          // Normal host shutdown.
        }
        catch (OperationCanceledException) when (attempt.IsCancellationRequested)
        {
          PublishConnectionStatus(StarCitizenConnectionStatus.Reconnecting);
        }
        catch (RpcException) when (attempt.IsCancellationRequested)
        {
          PublishConnectionStatus(StarCitizenConnectionStatus.Reconnecting);
        }
        catch (RpcException exception)
        {
          _logger.LogWarning("Star Citizen telemetry unavailable ({StatusCode}): {Detail}", exception.StatusCode, exception.Status.Detail);

          PublishConnectionStatus(StarCitizenConnectionStatus.Disconnected, exception.Status.Detail);
        }
        catch (Exception exception)
        {
          _logger.LogError(exception, "Star Citizen telemetry stream failed");

          PublishConnectionStatus(StarCitizenConnectionStatus.Disconnected, exception.Message);
        }

        if (!attempt.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
        {
          try
          {
            await Task.Delay(RETRY_DELAY_MS, attempt.Token);
          }
          catch (OperationCanceledException) when (attempt.IsCancellationRequested)
          {
            if (!stoppingToken.IsCancellationRequested)
            {
              PublishConnectionStatus(StarCitizenConnectionStatus.Reconnecting);
            }
          }
        }
      }
      finally
      {
        lock (_reconnectLock)
        {
          if (ReferenceEquals(_activeAttempt, attempt))
          {
            _activeAttempt = null;
          }
        }
      }
    }

    PublishConnectionStatus(StarCitizenConnectionStatus.Disconnected);
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    await base.StopAsync(cancellationToken);
    await Task.WhenAll(
      Vehicle.StopAsync(),
      Actor.StopAsync(),
      Weapons.StopAsync(),
      Connection.StopAsync());
  }

  #endregion

  #region Telemetry Streaming
  private async Task StreamTelemetryAsync(CancellationToken cancellationToken)
  {
    using GrpcChannel channel = GrpcChannel.ForAddress(GRPC_ADDRESS);

    Game.GameTelemetryService.GameTelemetryServiceClient client = new(channel);

    using AsyncServerStreamingCall<Game.TelemetryStreamResponse> call = client.TelemetryStream(
      new Game.TelemetryStreamRequest(),
      cancellationToken: cancellationToken);

    bool connected = false;

    while (await call.ResponseStream.MoveNext(cancellationToken))
    {
      if (!connected)
      {
        connected = true;
        _logger.LogInformation("Star Citizen telemetry connected");
      }

      Game.TelemetryStreamResponse frame = call.ResponseStream.Current;

      if (frame.Vehicle?.State is Game.VehicleState vehicle)
      {
        Vehicle.Publish(StarCitizenTelemetryMapper.MapVehicle(vehicle));
      }

      if (frame.Actor is Game.ActorFrame actor)
      {
        Actor.Publish(StarCitizenTelemetryMapper.MapActor(actor));
      }

      if (frame.Weapons is Game.WeaponsFrame weapons)
      {
        Weapons.Publish(StarCitizenTelemetryMapper.MapWeapons(weapons));
      }

      lock (_extractLock)
      {
        _lastVehicleFrame = frame.Vehicle?.Clone();
        _lastActorFrame = frame.Actor?.Clone();
        _lastWeaponsFrame = frame.Weapons?.Clone();

        _lastTelemetryAtUtc = DateTimeOffset.UtcNow;
      }

      DateTimeOffset now = DateTimeOffset.UtcNow;
      bool firstFrame = !connected;

      if (firstFrame)
      {
        connected = true;
        _lastConnectedAtUtc = now;

        _logger.LogInformation(
          "Star Citizen telemetry connected");
      }

      if (firstFrame || now - _lastConnectionPublishAtUtc >= TimeSpan.FromSeconds(5))
      {
        _lastConnectionPublishAtUtc = now;

        PublishConnectionStatus(StarCitizenConnectionStatus.Connected);
      }
    }
  }

  private void PublishConnectionStatus(StarCitizenConnectionStatus status, string? error = null)
  {
    DateTimeOffset? lastFrame;

    lock (_extractLock)
    {
      lastFrame = _lastTelemetryAtUtc;
    }

    Connection.Publish(new StarCitizenConnectionInfo
    {
      Status = status,
      Endpoint = GRPC_ADDRESS,
      LastConnectedAtUtc = _lastConnectedAtUtc,
      LastFrameAtUtc = lastFrame,
      LastError = error
    });
  }

  #endregion

  #region Telemetry Extract
  private string? CreateExtractJson()
  {
    VehicleFrame? vehicle;
    ActorFrame? actor;
    WeaponsFrame? weapons;
    DateTimeOffset? lastReceived;

    lock (_extractLock)
    {
      vehicle = _lastVehicleFrame;
      actor = _lastActorFrame;
      weapons = _lastWeaponsFrame;
      lastReceived = _lastTelemetryAtUtc;
    }

    if (vehicle is null && actor is null && weapons is null)
    {
      return null;
    }

    return IOUtils.WriteExtract(vehicle, actor, weapons, lastReceived);
  }

  private void AddShellCommands()
  {
    CommandDefinition extractCommand = new()
    {
      Name = "extract",
      Service = "telemetry", // won't use nameof because hell of a name it is
      Description = "Extract the latest telemetry data to a JSON file",
    };

    _shell.RegisterCommand(extractCommand, (_, output, cancellationToken) =>
    {
      cancellationToken.ThrowIfCancellationRequested();
      string? filePath = CreateExtractJson();
      if (filePath is null)
      {
        output?.Invoke("No telemetry data available to extract.");
        throw new InvalidOperationException("No telemetry data available to extract.");
      }

      output?.Invoke($"Writing telemetry extract to : {filePath}");
      return Task.CompletedTask;
    });

    CommandDefinition reconnectCommand = new()
    {
      Service = "telemetry",
      Name = "reconnect",
      Description = "Reconnects the Star Citizen telemetry stream."
    };

    _shell.RegisterCommand(reconnectCommand, (_, output, cancellationToken) =>
      {
        cancellationToken.ThrowIfCancellationRequested();

        bool requested = RequestReconnect();

        output?.Invoke(requested? "Star Citizen telemetry reconnection requested." : "No active attempt to restart, or reconnection already requested.");

        return Task.CompletedTask;
      });

    CommandDefinition statusCommand = new()
    {
      Service = "telemetry",
      Name = "status",
      Description = "Get the status of the Star Citizen telemetry connection."
    };

    _shell.RegisterCommand(statusCommand, (_, output, cancellationToken) =>
      {
        cancellationToken.ThrowIfCancellationRequested();

        StarCitizenConnectionInfo info = Connection.State;

        output?.Invoke($"Status: {info.Status}");
        output?.Invoke($"Endpoint: {info.Endpoint}");
        output?.Invoke($"Last Connected At: {info.LastConnectedAtUtc?.ToString("o") ?? "N/A"}");
        output?.Invoke($"Last Frame Received At: {info.LastFrameAtUtc?.ToString("o") ?? "N/A"}");
        output?.Invoke($"Last Error: {info.LastError ?? "N/A"}");

        return Task.CompletedTask;
      });

  }

  private bool RequestReconnect()
  {
    lock (_reconnectLock)
    {
      if (_activeAttempt is null ||
          _activeAttempt.IsCancellationRequested)
      {
        return false;
      }

      _activeAttempt.Cancel();

      return true;
    }
  }


  #endregion
}