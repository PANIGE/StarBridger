using Newtonsoft.Json;
using StarBridger.Contracts.Core.Shell;
using StarBridger.Contracts.IPC;
using StarBridger.Contracts.IPC.MainBus;
using StarBridger.Contracts.IPC.Shell;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Text;

namespace StarBridger.SDK.Clients;

internal class LocalStarBridgerConnection : IStarBridgerConnection
{
  public bool IsConnected { get; private set; } = false;

  public Guid? SessionId { get;  set; } = null;

  public event Func<Packet, CancellationToken, Task>? MainBusReceived;

  private NamedPipeClientStream? _pipe;
  private CancellationTokenSource? _receiveCancellation;

  public async Task ConnectAsync(CancellationToken cancellationToken = default)
  {
    if (IsConnected)
    {
      throw new InvalidOperationException("Already connected.");
    }

    NamedPipeClientStream pipe = new(".", PipeNames.Main, PipeDirection.In, PipeOptions.Asynchronous);

    using CancellationTokenSource receiveCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    _receiveCancellation = receiveCancellation;

    await pipe.ConnectAsync(3000, _receiveCancellation.Token);

    IsConnected = true;

    _pipe = pipe;
    try
    {
      await ReceiveAsync(pipe, _receiveCancellation.Token);
    }
    catch (OperationCanceledException) when (_receiveCancellation.IsCancellationRequested)
    {
      // Normal disconnection requested by the client.
    }
    finally
    {
      _pipe = null;
      IsConnected = false;
      _receiveCancellation.Dispose();
      _receiveCancellation = null;
      IsConnected = false;
    }
  }

  private async Task ReceiveAsync(NamedPipeClientStream pipe, CancellationToken cancellationToken)
  {
    using StreamReader reader = new(pipe, Encoding.UTF8, false, 1024, leaveOpen: true);

    string? line;

    while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
    {
      Packet packet = JsonConvert.DeserializeObject<Packet>(line) 
        ?? throw new InvalidOperationException("Failed to deserialize packet.");

      if (MainBusReceived is { } handler)
      {
        await handler(packet, cancellationToken);
      }
    }
  }

  public Task DisconnectAsync()
  {
    if (!IsConnected)
    {
      throw new InvalidOperationException("Not connected.");
    }

    _receiveCancellation!.Cancel();

    return Task.CompletedTask;
  }

  public async ValueTask DisposeAsync()
  {
    if (IsConnected)
      await DisconnectAsync();
  }

  public async IAsyncEnumerable<ShellResponse> ExecuteShellAsync(Command command, [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    using NamedPipeClientStream pipe = new(".", PipeNames.Shell, PipeDirection.InOut, PipeOptions.Asynchronous);
    await pipe.ConnectAsync(3000, cancellationToken);

    using StreamReader reader = new(pipe, Encoding.UTF8, false, 1024, leaveOpen: true);

    using StreamWriter writer = new(pipe, new UTF8Encoding(false), 1024, leaveOpen: true)
    {
      AutoFlush = true
    };

    await writer.WriteLineAsync(JsonConvert.SerializeObject(command));

    string? line;

    while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
    {
      ShellResponse? response = JsonConvert.DeserializeObject<ShellResponse>(line);

      yield return response is null 
        ? throw new InvalidOperationException("Invalid Shell response.") 
        : response;

      if (response is CompletedShellResponse)
      {
        yield break;
      }
    }
  }
}
