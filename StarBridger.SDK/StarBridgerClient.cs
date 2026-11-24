
using StarBridger.Contracts.Core.Shell;
using StarBridger.Contracts.Enums;
using StarBridger.SDK.Clients;
using StarBridger.SDK.Controllers;
using StarBridger.SDK.Controllers.Shell;
using StarBridger.SDK.Exceptions;
using System.Security.Principal;

namespace StarBridger.SDK;

public sealed class StarBridgerClient : IAsyncDisposable
{
  public bool IsConnected => _starBridgerConnection.IsConnected && _starBridgerConnection.SessionId != null;

  public Intents Intents { get; private set 
    {
      field = value;
      Context?.Intents = value;
    } 
  }

  public TelemetryContext Context { get; }

  public StarBridgerShell Shell { get; }

  private readonly IStarBridgerConnection _starBridgerConnection; 

  public StarBridgerClient(StarBridgerClientOptions? options = null)
  {
    options ??= new StarBridgerClientOptions();

    Intents = options.Intents;

    _starBridgerConnection = options.Mode switch
    {
      ConnectionMode.Local => new LocalStarBridgerConnection(),
      ConnectionMode.Remote => throw new NotImplementedException("Remote connection mode is not implemented yet."),
      _ => throw new NotSupportedException($"The connection mode '{options.Mode}' is not supported."),
    };

    Shell = new StarBridgerShell(_starBridgerConnection);
    Context = new TelemetryContext(Intents, _starBridgerConnection);

    _starBridgerConnection.MainBusReceived += Context.PropagatePacketAsync;
  }

  public Task RunAsync(CancellationToken cancellationToken = default)
  {
    return _starBridgerConnection.ConnectAsync(cancellationToken);
  }

  public Task DisconnectAsync()
  {
    return _starBridgerConnection.DisconnectAsync();
  }

  public async Task SetIntentsAsync(Intents intents, CancellationToken cancellationToken = default)
  {
    if (!_starBridgerConnection.IsConnected)
    {
      Intents = intents;
      return;
    }

    if (_starBridgerConnection.SessionId is not Guid sessionId)
    {
      throw new InvalidOperationException(
        "MainBus session identity is not available.");
    }

    Command setIntents = new()
    {
      CommandText = "ipc.intents.set",
      Arguments =
      [
        new Argument("id", sessionId.ToString()),
        new Argument("intents", ((ulong)intents).ToString())
      ]
    };

    ShellCommandResult result = await Shell.ExecuteAsync(setIntents, cancellationToken);

    if (!result.Success)
    {
      throw new ShellCommandException(result.Error ?? "Could not set intents.");
    }

    Intents = intents;
  }

  public async ValueTask DisposeAsync()
  {
    await _starBridgerConnection.DisposeAsync();
  }
}
