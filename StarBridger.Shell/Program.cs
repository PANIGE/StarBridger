
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using StarBridger.Contracts.Core.Shell;
using StarBridger.Contracts.IPC;
using StarBridger.Contracts.IPC.Shell;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json.Serialization;

namespace StarBridger.ConsoleClient;

internal static class Program
{
  private static async Task Main()
  {
    Console.WriteLine("StarBridger Shell");
    Console.WriteLine("Type 'exit' to quit.");
    Console.WriteLine();

    while (true)
    {
      Console.Write("starbridger> ");

      string? input = Console.ReadLine()?.Trim();

      if (input is null ||
          input.Equals("exit", StringComparison.OrdinalIgnoreCase))
      {
        break;
      }

      if (string.IsNullOrWhiteSpace(input))
      {
        continue;
      }

      try
      {
        await ExecuteCommand(input);
      }
      catch (TimeoutException)
      {
        Console.WriteLine("Unable to connect to StarBridger (timeout).");
      }
      catch (IOException ex)
      {
        Console.WriteLine($"IPC error: {ex.Message}");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error: {ex.Message}");
      }

      Console.WriteLine();
    }
  }

  private static async Task ExecuteCommand(string commandText)
  {
    using NamedPipeClientStream pipe = new(
      ".",
      PipeNames.Shell,
      PipeDirection.InOut,
      PipeOptions.Asynchronous);

    await pipe.ConnectAsync(3000);

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

    Command command = new()
    {
      CommandText = commandText,
      Arguments = []
    };

    await writer.WriteLineAsync(
      JsonConvert.SerializeObject(command));

    string? line;

    while ((line = await reader.ReadLineAsync()) is not null)
    {
      ShellResponse? response =
        JsonConvert.DeserializeObject<ShellResponse>(line);

      switch (response)
      {
        case OutputShellResponse output:
          Console.WriteLine(output.Message);
          break;

        case CompletedShellResponse completed:
          if (completed.Success)
          {
            Console.WriteLine("[Completed]");
          }
          else
          {
            Console.WriteLine(
              $"[Failed] {completed.ErrorMessage}");
          }

          return;
      }
    }

    Console.WriteLine("[Disconnected before completion]");
  }
}
