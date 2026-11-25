using Newtonsoft.Json.Linq;
using StarBridger.Contracts.Core.Shell;
using StarBridger.Contracts.Enums;
using StarBridger.Contracts.IPC.MainBus;
using StarBridger.Contracts.IPC.Shell;
using StarBridger.Contracts.Telemetry.Game.v1;
using StarBridger.SDK.Clients;
using StarBridger.SDK.Controllers;
using StarBridger.SDK.Controllers.Shell;
using StarBridger.SDK.Exceptions;

namespace StarBridger.Tests;

public sealed class SdkTests
{
  [Fact]
  public async Task ShellCommandExecutionCollectsResponsesAndTypedData()
  {
    Command command = new() { CommandText = "test.command" };
    ShellCommandExecution execution = new(command, Responses(
      new OutputShellResponse { Message = "working" },
      new DataShellResponse { Payload = JObject.FromObject(new TestData { Value = 42 }) },
      new CompletedShellResponse { Success = true }));

    ShellCommandResult result = await execution.Completion;

    Assert.Same(command, execution.Request);
    Assert.True(result.Completed);
    Assert.True(result.Success);
    Assert.Equal(string.Empty, result.Error);
    Assert.Equal(42, Assert.IsType<TestData>(Assert.Single(result.GetData<TestData>())!).Value);
    Assert.Equal(1, result.FlushDataResult());
    Assert.Empty(result.GetData<TestData>());

    List<ShellResponse> responses = [];
    await foreach (ShellResponse response in result.Responses)
      responses.Add(response);
    Assert.Equal(3, responses.Count);
  }

  [Fact]
  public async Task ShellCommandExecutionRejectsStreamWithoutCompletion()
  {
    ShellCommandExecution execution = new(
      new Command { CommandText = "test.command" },
      Responses(new OutputShellResponse { Message = "partial" }));

    await Assert.ThrowsAsync<IOException>(async () => await execution.Completion);
  }

  [Fact]
  public void TelemetryContextEnforcesRequestedIntents()
  {
    TelemetryContext context = new(Intents.Actor, new FakeConnection());

    Assert.Null(context.Actor);
    Assert.Throws<IntentException>(() => context.Vehicle);
  }

  [Fact]
  public async Task TelemetryContextUpdatesStateAndRaisesActorEvent()
  {
    TelemetryContext context = new(Intents.Actor, new FakeConnection());
    ActorTelemetry? updated = null;
    context.ActorUpdated += actor => updated = actor;
    ActorTelemetry expected = new() { State = new ActorStateTelemetry { UecBalance = 1234 } };

    await context.PropagatePacketAsync(new Packet
    {
      Type = Intents.Actor,
      Payload = JObject.FromObject(expected)
    });

    Assert.NotNull(context.Actor);
    Assert.Equal(1234, context.Actor!.State!.UecBalance);
    Assert.Same(context.Actor, updated);
  }

  [Fact]
  public async Task TelemetryIdentityRegistersSessionVersionAndIntents()
  {
    FakeConnection connection = new();
    TelemetryContext context = new(Intents.Actor | Intents.Vehicle, connection);
    Guid identity = Guid.NewGuid();

    await context.PropagatePacketAsync(new Packet
    {
      Type = Intents.Identity,
      Payload = JValue.FromObject(identity)
    });

    Assert.Equal(identity, connection.SessionId);
    Assert.Collection(
      connection.Commands,
      version => Assert.Equal("ipc.version.set", version.CommandText),
      intents => Assert.Equal("ipc.intents.set", intents.CommandText));
    Assert.Equal(identity.ToString(), connection.Commands[0].Arguments[0].Value);
    Assert.Equal("1.0.0", connection.Commands[0].Arguments[1].Value);
    Assert.Equal(((ulong)(Intents.Actor | Intents.Vehicle)).ToString(), connection.Commands[1].Arguments[1].Value);
  }

  [Fact]
  public async Task TelemetryIdentityPropagatesCommandFailure()
  {
    FakeConnection connection = new() { FailCommandNumber = 1 };
    TelemetryContext context = new(Intents.Actor, connection);

    await Assert.ThrowsAsync<ShellCommandException>(() => context.PropagatePacketAsync(new Packet
    {
      Type = Intents.Identity,
      Payload = JValue.FromObject(Guid.NewGuid())
    }));

    Assert.Single(connection.Commands);
  }

  private static async IAsyncEnumerable<ShellResponse> Responses(params ShellResponse[] responses)
  {
    foreach (ShellResponse response in responses)
    {
      await Task.Yield();
      yield return response;
    }
  }

  private sealed class TestData
  {
    public int Value { get; init; }
  }

  private sealed class FakeConnection : IStarBridgerConnection
  {
    public bool IsConnected { get; set; }
    public Guid? SessionId { get; set; }
    public List<Command> Commands { get; } = [];
    public int? FailCommandNumber { get; init; }
    public event Func<Packet, CancellationToken, Task>? MainBusReceived;

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
      return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
      return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
      return ValueTask.CompletedTask;
    }

    public IAsyncEnumerable<ShellResponse> ExecuteShellAsync(Command command, CancellationToken cancellationToken = default)
    {
      Commands.Add(command);
      bool success = Commands.Count != FailCommandNumber;
      return Responses(new CompletedShellResponse { Success = success, ErrorMessage = success ? string.Empty : "rejected" });
    }

    public Task RaisePacketAsync(Packet packet, CancellationToken cancellationToken = default)
    {
      return MainBusReceived?.Invoke(packet, cancellationToken) ?? Task.CompletedTask;
    }
  }
}
