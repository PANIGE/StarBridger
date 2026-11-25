using System.Threading.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using StarBridger.Contracts.Core.Shell;
using StarBridger.Contracts.Core.Shell.Definitions;
using StarBridger.Core.Managers;
using StarBridger.Core.Services;

namespace StarBridger.Tests;

public sealed class CoreTests
{
  [Fact]
  public async Task StateStorePublishesInitialAndUpdatedStateAndUnsubscribesOwner()
  {
    StateValue initial = new("initial");
    StateStore<StateValue> store = new(initial, NullLogger.Instance);
    Channel<StateValue> received = Channel.CreateUnbounded<StateValue>();
    Guid owner = Guid.NewGuid();

    Guid subscription = store.Subscribe(value => received.Writer.TryWrite(value), owner);

    Assert.Equal(initial, await received.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2)));
    StateValue updated = new("updated");
    Assert.Equal(1, store.Publish(updated));
    Assert.Equal(updated, await received.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2)));
    Assert.Same(updated, store.State);
    Assert.Equal(1, store.SubscriberCount);

    await store.UnsubscribeByOwnerAsync(owner);

    Assert.Equal(0, store.SubscriberCount);
    Assert.Equal(0, store.Publish(new StateValue("ignored")));
    store.Unsubscribe(subscription);
    await store.StopAsync();

    Assert.Equal(0, store.Publish(new StateValue("stopped")));
    Assert.Throws<InvalidOperationException>(() => store.Subscribe(_ => { }));
  }

  [Fact]
  public void StateStoreRejectsNullInitialStateAndInvalidSubscriptions()
  {
    Assert.Throws<ArgumentNullException>(() => new StateStore<StateValue>(null!, NullLogger.Instance));

    StateStore<StateValue> store = new(new StateValue("state"), NullLogger.Instance);
    Assert.Throws<ArgumentNullException>(() => store.Subscribe(null!));
    Assert.Throws<ArgumentOutOfRangeException>(() => store.Subscribe(_ => { }, queueCapacity: 0));
  }

  [Fact]
  public async Task ShellRegistersExecutesAndUnregistersCommandsCaseInsensitively()
  {
    StarBridgerShell shell = new(NullLogger<StarBridgerShell>.Instance);
    CommandDefinition definition = new() { Service = "Host", Name = "Status" };
    string? output = null;

    shell.RegisterCommand(definition, (_, write, _, _) =>
    {
      write?.Invoke("ready");
      return Task.CompletedTask;
    });

    Assert.Equal("host.status", Assert.Single(shell.GetCommandDefinitions()).CommandText);
    Assert.Throws<InvalidOperationException>(() => shell.RegisterCommand(
      new CommandDefinition { Service = "HOST", Name = "STATUS" },
      (_, _, _, _) => Task.CompletedTask));

    bool executed = await shell.ExecuteCommand(
      new Command { CommandText = "HOST.STATUS" },
      message => output = message,
      null,
      CancellationToken.None);

    Assert.True(executed);
    Assert.Equal("ready", output);

    shell.UnregisterServiceCommands("hOsT");
    Assert.Empty(shell.GetCommandDefinitions());
    Assert.False(await shell.ExecuteCommand(
      new Command { CommandText = "host.status" }, null, null, CancellationToken.None));
  }

  [Fact]
  public async Task ShellReturnsFalseForHandlerErrorsAndRethrowsRequestedCancellation()
  {
    StarBridgerShell shell = new(NullLogger<StarBridgerShell>.Instance);
    shell.RegisterCommand(
      new CommandDefinition { Service = "test", Name = "failure" },
      (_, _, _, _) => throw new InvalidOperationException("handler failed"));
    shell.RegisterCommand(
      new CommandDefinition { Service = "test", Name = "cancel" },
      (_, _, _, cancellationToken) => Task.FromCanceled(cancellationToken));

    Assert.False(await shell.ExecuteCommand(
      new Command { CommandText = "test.failure" }, null, null, CancellationToken.None));

    using CancellationTokenSource cancellation = new();
    cancellation.Cancel();
    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => shell.ExecuteCommand(
      new Command { CommandText = "test.cancel" }, null, null, cancellation.Token));
  }

  private sealed record StateValue(string Value);
}
