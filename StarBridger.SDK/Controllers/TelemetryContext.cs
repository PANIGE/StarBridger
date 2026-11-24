using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StarBridger.Contracts.Core.Shell;
using StarBridger.Contracts.Enums;
using StarBridger.Contracts.IPC.MainBus;
using StarBridger.Contracts.IPC.Shell;
using StarBridger.Contracts.Telemetry;
using StarBridger.Contracts.Telemetry.Game;
using StarBridger.Contracts.Telemetry.Game.v1;
using StarBridger.SDK.Clients;
using StarBridger.SDK.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.SDK.Controllers;

public sealed class TelemetryContext
{
  internal Intents Intents { get; set; }
  private readonly IStarBridgerConnection _connexion;

  public ActorTelemetry? Actor { get => 
      Intents.HasFlag(Intents.Actor) ? field 
      : throw new IntentException($"Current Connection doesn't has {nameof(Intents.Actor)} Intents therefore can't access {nameof(TelemetryContext)}.{nameof(Actor)}"); 
    internal set; }

  public VehicleTelemetry? Vehicle
  {
    get =>
      Intents.HasFlag(Intents.Vehicle) ? field
      : throw new IntentException($"Current Connection doesn't has {nameof(Intents.Vehicle)} Intents therefore can't access {nameof(TelemetryContext)}.{nameof(Vehicle)}");
    internal set;
  }

  public WeaponsTelemetry? Weapons
  {
    get =>
      Intents.HasFlag(Intents.Weapons) ? field
      : throw new IntentException($"Current Connection doesn't has {nameof(Intents.Weapons)} Intents therefore can't access {nameof(TelemetryContext)}.{nameof(Weapons)}");
    internal set;
  }

  public StarCitizenConnectionInfo? GameConnection
  {
    get =>
      Intents.HasFlag(Intents.GameConnection) ? field
      : throw new IntentException($"Current Connection doesn't has {nameof(Intents.GameConnection)} Intents therefore can't access {nameof(TelemetryContext)}.{nameof(GameConnection)}");
    internal set;
  }

  public event Action<ActorTelemetry>? ActorUpdated;

  public event Action<VehicleTelemetry>? VehicleUpdated;

  public event Action<WeaponsTelemetry>? WeaponsUpdated;

  public event Action<StarCitizenConnectionInfo>? GameConnectionUpdated;

  internal TelemetryContext(Intents intents, IStarBridgerConnection starBridgerConnection)
  {
    Intents = intents;
    _connexion = starBridgerConnection;
  }

  internal async Task PropagatePacketAsync(Packet packet, CancellationToken cancellationToken = default)
  {
    JToken token = packet.Payload switch
    {
      JToken t => t,
      string s when Guid.TryParse(s, out _) => new JValue(s), 
      string s => JToken.Parse(s),                          
      _ => throw new NotImplementedException(),
    };

    if (token == null)
      return;

    switch (packet.Type)
    {
      case Intents.Actor:
        if (DeserializeTelemetry<ActorTelemetry>(token) is { } actor)
        {
          Actor = actor;
          ActorUpdated?.Invoke(actor);
        }

        break;

      case Intents.Vehicle:
        if (DeserializeTelemetry<VehicleTelemetry>(token) is { } vehicle)
        {
          Vehicle = vehicle;
          VehicleUpdated?.Invoke(vehicle);
        }

        break;

      case Intents.Weapons:
        if (DeserializeTelemetry<WeaponsTelemetry>(token) is { } weapons)
        {
          Weapons = weapons;
          WeaponsUpdated?.Invoke(weapons);
        }

        break;

      case Intents.GameConnection:
        if (DeserializeTelemetry<StarCitizenConnectionInfo>(token) is { } connection)
        {
          GameConnection = connection;
          GameConnectionUpdated?.Invoke(connection);
        }

        break;

      case Intents.Identity:
        Guid identity = token.ToObject<Guid>();

        _connexion.SessionId = identity;

        Command setVersion = new()
        {
          CommandText = "ipc.version.set",
          Arguments = [new Argument("id", identity.ToString()), new Argument("version", Constants.LatestVersion.ToString())]
        };

        await ExecuteCheckedAsync(setVersion, cancellationToken);

        Command setIntents = new()
        {
          CommandText = "ipc.intents.set",
          Arguments = [new Argument("id", identity.ToString()), new Argument("intents", ((ulong)Intents).ToString())]
        };

        await ExecuteCheckedAsync(setIntents, cancellationToken);

        break;
    }
  }
  private async Task ExecuteCheckedAsync(Command command, CancellationToken cancellationToken)
  {
    CompletedShellResponse? completion = null;

    await foreach (ShellResponse response in _connexion.ExecuteShellAsync(command, cancellationToken))
    {
      if (response is CompletedShellResponse completed)
        completion = completed;
    }

    if (completion?.Success != true)
    {
      throw new ShellCommandException(
        completion?.ErrorMessage ?? $"Command {command.CommandText} failed.");
    }
  }

  private T? DeserializeTelemetry<T>(JToken token) where T : class
  {
    try
    {
      return token.ToObject<T>();
    }
    catch (JsonException ex)
    {
      // incompatible or invalid telemetry payload.
      return null;
    }
  }
}
