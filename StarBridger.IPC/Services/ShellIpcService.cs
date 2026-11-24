using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StarBridger.Contracts.Core.Shell;
using StarBridger.Contracts.Enums;
using StarBridger.Contracts.IPC.Shell;
using StarBridger.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Channels;

namespace StarBridger.IPC.Services;

public sealed class ShellIpcService : BackgroundService
{
  private const int QUEUE_CAPACITY = 128;

  private readonly StarBridgerShell _shell;
  private readonly ILogger<ShellIpcService> _logger;

  public ShellIpcService(StarBridgerShell shell, ILogger<ShellIpcService> logger)
  {
    _shell = shell;
    _logger = logger;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    List<Task> sessions = new();

    _logger.LogInformation("Shell IPC listening on {PipeName}", Contracts.IPC.PipeNames.Shell);

    try
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        NamedPipeServerStream pipe = new(
          Contracts.IPC.PipeNames.Shell,
          PipeDirection.InOut,
          NamedPipeServerStream.MaxAllowedServerInstances,
          PipeTransmissionMode.Byte,
          PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

        try
        {
          await pipe.WaitForConnectionAsync(stoppingToken);
        }
        catch
        {
          await pipe.DisposeAsync();
          throw;
        }

        sessions.RemoveAll(task => task.IsCompleted);
        sessions.Add(HandleClientAsync(pipe, stoppingToken));
      }
    }
    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
    {
      // Normal host shutdown.
    }
    finally
    {
      await Task.WhenAll(sessions);

      _logger.LogInformation("Shell IPC stopped");
    }
  }

  private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
  {
    await using (pipe)
    {
      using StreamReader reader = new(
        pipe,
        Encoding.UTF8,
        false,
        1024,
        leaveOpen: true);

      using StreamWriter writer = new(
        pipe,
        new UTF8Encoding(false),
        1024,
        leaveOpen: true)
      {
        AutoFlush = true
      };

      try
      {
        string? request = await reader.ReadLineAsync(cancellationToken);

        if (request is null)
        {
          return;
        }

        Command? command;

        try
        {
          command = JsonConvert.DeserializeObject<Command>(request);
        }
        catch (JsonException)
        {
          await writer.WriteLineAsync(
            JsonConvert.SerializeObject(new CompletedShellResponse
            {
              Success = false,
              ErrorMessage = "invalid_json"
            }).AsMemory(),
            cancellationToken);

          return;
        }

        if (command is null || string.IsNullOrWhiteSpace(command.CommandText))
        {
          await writer.WriteLineAsync(
            JsonConvert.SerializeObject(new CompletedShellResponse
            {
              Success = false,
              ErrorMessage = "invalid_command"
            }).AsMemory(),
            cancellationToken);

          return;
        }

        Channel<string> responses = Channel.CreateBounded<string>(
            new BoundedChannelOptions(QUEUE_CAPACITY)
            {
              SingleReader = true,
              SingleWriter = false,
              FullMode = BoundedChannelFullMode.Wait
            });

        Task sender = SendResponsesAsync(
          writer,
          responses.Reader,
          cancellationToken);

        try
        {
          void output(string message)
          {
            string response = JsonConvert.SerializeObject(new OutputShellResponse
            {
              Message = message
            });

            if (!responses.Writer.TryWrite(response))
            {
              _logger.LogWarning("Shell output queue full for {Command}", command.CommandText);
            }
          }

          bool success = await _shell.ExecuteCommand(command, output, cancellationToken);

          await responses.Writer.WriteAsync(
            JsonConvert.SerializeObject(new CompletedShellResponse
            {
              Success = success
            }),
            cancellationToken);
        }
        finally
        {
          responses.Writer.TryComplete();
          await sender;
        }
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        // Host shutting down.
      }
      catch (IOException exception)
      {
        _logger.LogDebug(exception, "Shell IPC client disconnected");
      }
      catch (Exception exception)
      {
        _logger.LogError(exception, "Shell IPC session failed");
      }
    }
  }

  private static async Task SendResponsesAsync(StreamWriter writer, ChannelReader<string> responses, CancellationToken cancellationToken)
  {
    await foreach (string response in responses.ReadAllAsync(cancellationToken))
    {
      await writer.WriteLineAsync(response.AsMemory(), cancellationToken);
    }
  }
}