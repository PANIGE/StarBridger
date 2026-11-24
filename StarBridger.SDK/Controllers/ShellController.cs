using StarBridger.Contracts.Core.Shell;
using StarBridger.Contracts.Host;
using StarBridger.SDK.Clients;
using StarBridger.SDK.Controllers.Shell;
using StarBridger.SDK.Exceptions;
using StarBridger.SDK.Utils;

namespace StarBridger.SDK.Controllers;

public sealed class StarBridgerShell
{
  private readonly IStarBridgerConnection _client;

  internal StarBridgerShell(IStarBridgerConnection client)
  {
    _client = client;
  }

  public async Task<HostStatus> GetHostStatusAsync(CancellationToken cancellationToken = default)
  {
    Command command = new()
    {
      CommandText = "host.status",
      Arguments = [ShellUtils.RawArgument]
    };

    ShellCommandResult result = await ExecuteAsync(command, cancellationToken);

    if (!result.Success)
    {
      throw new ShellCommandException(result.Error ?? "Failed to get host status.");
    }

    return result.GetData<HostStatus>().FirstOrDefault() ?? throw new ShellCommandException("Host did not return any status data.");
  }

  public Task ShutdownHostAsync(CancellationToken cancellationToken = default)
  {
    Command command = new()
    {
      CommandText = "host.shutdown"
    };

    return ExecuteCheckedAsync(command, cancellationToken);
  }

  public Task ReconnectTelemetryAsync(CancellationToken cancellationToken = default)
  {
    Command command = new()
    {
      CommandText = "telemetry.reconnect"
    };

    return ExecuteCheckedAsync(command, cancellationToken);
  }

  public async Task<string> ExtractTelemetryFrameAsync(CancellationToken cancellationToken = default)
  {
    Command command = new()
    {
      CommandText = "telemetry.extract",
      Arguments = [ShellUtils.RawArgument]
    };

    ShellCommandResult result = await ExecuteAsync(command, cancellationToken);

    if (!result.Success)
      throw new ShellCommandException(result.Error ?? "Failed to extract telemetry frame.");

    return result.GetData<string>().FirstOrDefault() 
      ?? throw new ShellCommandException("Telemetry extraction succeeded but returned no file path.");
  }

  public ShellCommandExecution ExecuteAsync(Command command,CancellationToken cancellationToken = default)
  {
    return new ShellCommandExecution(command, _client.ExecuteShellAsync(command, cancellationToken)); 
  }

  private async Task ExecuteCheckedAsync(Command command, CancellationToken cancellationToken)
  {
    ShellCommandResult result = await ExecuteAsync(command, cancellationToken);

    if (!result.Success)
    {
      throw new ShellCommandException(
        result.Error ?? $"Command {command.CommandText} failed.");
    }
  }
}
