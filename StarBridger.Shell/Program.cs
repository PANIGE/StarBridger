
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

    Command command = ParseCommand(commandText);

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
  private static Command ParseCommand(string input)
  {
    string[] tokens = Tokenize(input);

    if (tokens.Length == 0)
    {
      throw new FormatException("Empty command.");
    }

    List<Argument> arguments = [];

    for (int i = 1; i < tokens.Length; i++)
    {
      string token = tokens[i];

      if (!IsOption(token))
      {
        throw new FormatException(
          $"Unexpected argument: {token}");
      }

      string option = token.TrimStart('-');
      string name;
      string value;

      int separator = option.IndexOf('=');

      if (separator >= 0)
      {
        name = option[..separator];
        value = option[(separator + 1)..];
      }
      else
      {
        name = option;
        value = "true";

        if (i + 1 < tokens.Length &&
            !IsOption(tokens[i + 1]))
        {
          value = tokens[++i];
        }
      }

      if (string.IsNullOrWhiteSpace(name))
      {
        throw new FormatException("Invalid option name.");
      }

      arguments.Add(new Argument
      {
        Name = name,
        Value = value
      });
    }

    return new Command
    {
      CommandText = tokens[0],
      Arguments = arguments.ToArray()
    };
  }

  private static bool IsOption(string token)
  {
    string name = token.TrimStart('-');

    return name.Length > 0 &&
           name.Length < token.Length &&
           (char.IsLetter(name[0]) || name[0] == '_');
  }

  private static string[] Tokenize(string input)
  {
    List<string> tokens = [];
    StringBuilder current = new();

    char quote = '\0';
    bool started = false;

    foreach (char character in input)
    {
      if (quote != '\0')
      {
        if (character == quote)
        {
          quote = '\0';
        }
        else
        {
          current.Append(character);
        }

        continue;
      }

      if (character is '"' or '\'')
      {
        quote = character;
        started = true;
        continue;
      }

      if (char.IsWhiteSpace(character))
      {
        if (started)
        {
          tokens.Add(current.ToString());
          current.Clear();
          started = false;
        }

        continue;
      }

      current.Append(character);
      started = true;
    }

    if (quote != '\0')
    {
      throw new FormatException("Unclosed quotation mark.");
    }

    if (started)
    {
      tokens.Add(current.ToString());
    }

    return tokens.ToArray();
  }

}