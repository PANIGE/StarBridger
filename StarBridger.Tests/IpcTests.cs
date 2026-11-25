using Microsoft.Extensions.Logging.Abstractions;
using StarBridger.Contracts.Core.Shell;
using StarBridger.Contracts.Enums;
using StarBridger.Contracts.Telemetry.Game.v1;
using StarBridger.Core.Managers;
using StarBridger.Core.Services;
using StarBridger.IPC.Services;

namespace StarBridger.Tests;

public sealed class IpcTests
{
  [Fact]
  public async Task MainIpcBusCachesLatestStateAndUnsubscribesOnStop()
  {
    StarBridgerShell shell = new(NullLogger<StarBridgerShell>.Instance);
    MainIpcBusService bus = new(NullLogger<MainIpcBusService>.Instance, shell);
    TelemetryState initial = new("initial");
    StateStore<TelemetryState> store = new(initial, NullLogger.Instance);

    Assert.True(bus.AddEventListener(store, Intents.Actor));
    await WaitUntilAsync(() => bus.States.ContainsKey(Intents.Actor));
    Assert.Same(initial, bus.States[Intents.Actor]);

    TelemetryState updated = new("updated");
    store.Publish(updated);
    await WaitUntilAsync(() => ReferenceEquals(bus.States.GetValueOrDefault(Intents.Actor), updated));
    Assert.Same(updated, bus.States[Intents.Actor]);

    await bus.StopAsync(CancellationToken.None);
    Assert.Equal(0, store.SubscriberCount);
    await store.StopAsync();
    await shell.StopAsync(CancellationToken.None);
  }

  [Fact]
  public async Task MainIpcRegistersCommandsAndRejectsMissingArguments()
  {
    StarBridgerShell shell = new(NullLogger<StarBridgerShell>.Instance);
    MainIpcBusService bus = new(NullLogger<MainIpcBusService>.Instance, shell);
    List<string> output = [];

    Assert.Contains(shell.GetCommandDefinitions(), definition => definition.CommandText == "ipc.intents.set");
    Assert.Contains(shell.GetCommandDefinitions(), definition => definition.CommandText == "ipc.version.set");

    Assert.False(await shell.ExecuteCommand(
      new Command { CommandText = "ipc.intents.set" }, output.Add, null, CancellationToken.None));
    Assert.False(await shell.ExecuteCommand(
      new Command { CommandText = "ipc.version.set" }, output.Add, null, CancellationToken.None));
    Assert.Equal(2, output.Count(line => line.StartsWith("Invalid arguments", StringComparison.Ordinal)));

    await bus.StopAsync(CancellationToken.None);
    await shell.StopAsync(CancellationToken.None);
  }

  private static async Task WaitUntilAsync(Func<bool> condition)
  {
    using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(2));
    while (!condition())
      await Task.Delay(10, timeout.Token);
  }

  private sealed record TelemetryState(string Value) : ContractV1;
}
