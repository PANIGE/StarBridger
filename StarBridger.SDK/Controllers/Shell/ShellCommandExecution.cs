using StarBridger.Contracts.Core.Shell;
using StarBridger.Contracts.IPC.Shell;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

namespace StarBridger.SDK.Controllers.Shell;

public sealed class ShellCommandExecution
{
  public Task<ShellCommandResult> Completion { get; }

  public Command Request { get; }
  public ShellCommandResult Result { get; }

  internal ShellCommandExecution(Command command, IAsyncEnumerable<ShellResponse> responses)
  {
    Request = command;

    Channel<ShellResponse> channel = Channel.CreateUnbounded<ShellResponse>();

    Result = new ShellCommandResult(channel.Reader.ReadAllAsync());
    Completion = ConsumeAsync(responses, channel.Writer);
  }

  private async Task<ShellCommandResult> ConsumeAsync(IAsyncEnumerable<ShellResponse> responses, ChannelWriter<ShellResponse> writer)
  {
    try
    {
      await foreach (ShellResponse response in responses)
      {
        Result.Update(response);
        writer.TryWrite(response);

        if (response is CompletedShellResponse)
          break;
      }

      if (!Result.Completed)
        throw new IOException("Shell closed before completion.");

      writer.TryComplete();
      return Result;
    }
    catch (Exception ex)
    {
      writer.TryComplete(ex);
      throw;
    }
  }

  public TaskAwaiter<ShellCommandResult> GetAwaiter()
  {
    return Completion.GetAwaiter();
  }
}
