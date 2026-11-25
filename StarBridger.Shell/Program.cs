using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StarBridger.Contracts.Core.Shell;
using StarBridger.Contracts.IPC.Shell;
using StarBridger.SDK;
using StarBridger.SDK.Controllers;
using StarBridger.SDK.Controllers.Shell;
using System.Text;

namespace StarBridger.Shell;

internal static class Program
{
  private enum DashboardView
  {
    All,
    Actor,
    Vehicle,
    Weapons,
    GameConnection
  }

  private readonly record struct DashboardRow(string Section, string Path, string Value);

  private static async Task Main()
  {
    Console.OutputEncoding = Encoding.UTF8;

    await using StarBridgerClient client = new();
    using CancellationTokenSource lifetime = new();

    Task runTask = client.RunAsync(lifetime.Token);
    bool reportedRunFailure = false;

    Console.WriteLine("StarBridger Shell (SDK)");
    Console.WriteLine("Commands: monitor (live telemetry), exit (quit), or any Host Shell command.");
    Console.WriteLine();

    try
    {
      while (true)
      {
        if (runTask.IsCompleted && !reportedRunFailure)
        {
          ReportMainBusCompletion(runTask);
          reportedRunFailure = true;
        }

        Console.Write("starbridger> ");
        string? input = Console.ReadLine()?.Trim();

        if (input is null || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
          break;

        if (string.IsNullOrWhiteSpace(input))
          continue;

        try
        {
          if (input.Equals("monitor", StringComparison.OrdinalIgnoreCase))
          {
            await RunMonitorAsync(client, runTask, lifetime.Token);
          }
          else
          {
            await ExecuteCommandAsync(client, input, lifetime.Token);
          }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
        {
          break;
        }
        catch (TimeoutException)
        {
          Console.WriteLine("Unable to connect to StarBridger (timeout).");
        }
        catch (IOException ex)
        {
          Console.WriteLine($"I/O error: {ex.Message}");
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine();
      }
    }
    finally
    {
      lifetime.Cancel();

      try
      {
        await runTask;
      }
      catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
      {
        // Expected shutdown.
      }
      catch (Exception ex)
      {
        if (!reportedRunFailure)
          Console.WriteLine($"MainBus stopped with an error: {ex.Message}");
      }
    }
  }

  private static void ReportMainBusCompletion(Task runTask)
  {
    if (runTask.IsFaulted)
    {
      Console.WriteLine($"[MainBus error] {runTask.Exception?.GetBaseException().Message}");
    }
    else if (runTask.IsCanceled)
    {
      Console.WriteLine("[MainBus canceled]");
    }
    else
    {
      Console.WriteLine("[MainBus disconnected]");
    }
  }

  private static async Task ExecuteCommandAsync(
    StarBridgerClient client,
    string commandText,
    CancellationToken cancellationToken)
  {
    Command command = ParseCommand(commandText);
    ShellCommandExecution execution = client.Shell.ExecuteAsync(command, cancellationToken);

    await foreach (ShellResponse response in execution.Result.Responses.WithCancellation(cancellationToken))
    {
      switch (response)
      {
        case OutputShellResponse output:
          Console.WriteLine(output.Message);
          break;

        case DataShellResponse data:
          Console.WriteLine(data.Payload is JToken token
            ? token.ToString(Formatting.Indented)
            : JsonConvert.SerializeObject(data.Payload, Formatting.Indented));
          break;

        case CompletedShellResponse completed:
          Console.WriteLine(completed.Success
            ? "[Completed]"
            : $"[Failed] {completed.ErrorMessage}");
          break;
      }
    }

    await execution.Completion;
  }

  private static async Task RunMonitorAsync(
    StarBridgerClient client,
    Task runTask,
    CancellationToken cancellationToken)
  {
    if (Console.IsInputRedirected || Console.IsOutputRedirected)
    {
      Console.WriteLine("The live monitor requires an interactive terminal.");
      return;
    }

    DashboardView view = DashboardView.All;
    int offset = 0;
    List<string> previousFrame = [];
    int previousWidth = 0;
    int previousHeight = 0;

    try
    {
      Console.CursorVisible = false;
      Console.Clear();

      while (!cancellationToken.IsCancellationRequested)
      {
        int width = Console.WindowWidth;
        int height = Console.WindowHeight;
        int visibleRows = Math.Max(0, height - 6);

        while (Console.KeyAvailable)
        {
          ConsoleKeyInfo key = Console.ReadKey(intercept: true);

          if (key.Key is ConsoleKey.Q or ConsoleKey.Escape)
            return;

          switch (key.Key)
          {
            case ConsoleKey.D0:
            case ConsoleKey.NumPad0:
              view = DashboardView.All;
              offset = 0;
              break;
            case ConsoleKey.D1:
            case ConsoleKey.NumPad1:
              view = DashboardView.Actor;
              offset = 0;
              break;
            case ConsoleKey.D2:
            case ConsoleKey.NumPad2:
              view = DashboardView.Vehicle;
              offset = 0;
              break;
            case ConsoleKey.D3:
            case ConsoleKey.NumPad3:
              view = DashboardView.Weapons;
              offset = 0;
              break;
            case ConsoleKey.D4:
            case ConsoleKey.NumPad4:
              view = DashboardView.GameConnection;
              offset = 0;
              break;
            case ConsoleKey.UpArrow:
              offset = Math.Max(0, offset - 1);
              break;
            case ConsoleKey.DownArrow:
              offset++;
              break;
            case ConsoleKey.PageUp:
              offset = Math.Max(0, offset - Math.Max(1, visibleRows));
              break;
            case ConsoleKey.PageDown:
              offset += Math.Max(1, visibleRows);
              break;
            case ConsoleKey.Home:
              offset = 0;
              break;
            case ConsoleKey.End:
              offset = int.MaxValue;
              break;
          }
        }

        List<DashboardRow> rows = BuildRows(client.Context, view);
        int maxOffset = Math.Max(0, rows.Count - visibleRows);
        offset = Math.Clamp(offset, 0, maxOffset);

        if (width != previousWidth || height != previousHeight)
        {
          Console.Clear();
          previousFrame.Clear();
          previousWidth = width;
          previousHeight = height;
        }

        List<string> frame = BuildFrame(rows, view, offset, width, height, runTask);
        RenderFrame(frame, previousFrame, width);

        await Task.Delay(250, cancellationToken);
      }
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      // Expected shutdown.
    }
    finally
    {
      Console.CursorVisible = true;
      Console.Clear();
    }
  }

  private static List<DashboardRow> BuildRows(TelemetryContext context, DashboardView view)
  {
    List<DashboardRow> rows = [];

    if (view is DashboardView.All or DashboardView.Actor)
      AddState(rows, "ACTOR", context.Actor);

    if (view is DashboardView.All or DashboardView.Vehicle)
      AddState(rows, "VEHICLE", context.Vehicle);

    if (view is DashboardView.All or DashboardView.Weapons)
      AddState(rows, "WEAPONS", context.Weapons);

    if (view is DashboardView.All or DashboardView.GameConnection)
      AddState(rows, "GAME CONNECTION", context.GameConnection);

    return rows;
  }

  private static void AddState(List<DashboardRow> rows, string section, object? state)
  {
    if (state is null)
    {
      rows.Add(new DashboardRow(section, "(not received yet)", "—"));
      return;
    }

    AddToken(rows, section, string.Empty, JToken.FromObject(state));
  }

  private static void AddToken(
    List<DashboardRow> rows,
    string section,
    string path,
    JToken token)
  {
    if (token is JObject obj)
    {
      if (!obj.HasValues)
        rows.Add(new DashboardRow(section, path, "{}"));

      foreach (JProperty property in obj.Properties())
      {
        string childPath = string.IsNullOrEmpty(path)
          ? property.Name
          : $"{path}.{property.Name}";

        AddToken(rows, section, childPath, property.Value);
      }
    }
    else if (token is JArray array)
    {
      if (array.Count == 0)
        rows.Add(new DashboardRow(section, path, "[]"));

      for (int i = 0; i < array.Count; i++)
        AddToken(rows, section, $"{path}[{i}]", array[i]);
    }
    else
    {
      rows.Add(new DashboardRow(
        section,
        path,
        token.Type == JTokenType.Null ? "null" : token.ToString(Formatting.None)));
    }
  }

  private static List<string> BuildFrame(
    List<DashboardRow> rows,
    DashboardView view,
    int offset,
    int width,
    int height,
    Task runTask)
  {
    List<string> frame = [];

    string busStatus = runTask.IsFaulted ? "FAULTED"
      : runTask.IsCanceled ? "CANCELED"
      : runTask.IsCompleted ? "STOPPED"
      : "RUNNING";

    frame.Add($"STARBRIDGER | LIVE GAME CONTEXT | MainBus: {busStatus}");
    frame.Add("[0] All  [1] Actor  [2] Vehicle  [3] Weapons  [4] Connection");
    frame.Add("[Up/Down/PgUp/PgDn/Home/End] Scroll  [Q/Esc] Exit monitor");
    frame.Add($"View: {view} | Fields: {rows.Count} | Scroll: {offset + 1}/{Math.Max(1, rows.Count)}");

    int usableWidth = Math.Max(1, width - 1);
    int fieldWidth = Math.Clamp(usableWidth * 2 / 3, 1, Math.Max(1, usableWidth - 3));
    int valueWidth = Math.Max(0, usableWidth - fieldWidth - 3);

    frame.Add($"{Fit("SECTION / FIELD", fieldWidth)} | {Fit("VALUE", valueWidth)}");
    frame.Add(new string('-', usableWidth));

    int visibleRows = Math.Max(0, height - frame.Count);

    foreach (DashboardRow row in rows.Skip(offset).Take(visibleRows))
    {
      string label = $"{row.Section} / {row.Path}";
      frame.Add($"{Fit(label, fieldWidth)} | {Fit(row.Value, valueWidth)}");
    }

    return frame;
  }

  private static string Fit(string text, int width)
  {
    if (width <= 0)
      return string.Empty;

    if (text.Length > width)
      return width == 1 ? "…" : text[..(width - 1)] + "…";

    return text.PadRight(width);
  }

  private static void RenderFrame(List<string> frame, List<string> previousFrame, int width)
  {
    int usableWidth = Math.Max(1, width - 1);
    int height = Console.WindowHeight;

    for (int y = 0; y < height; y++)
    {
      string line = y < frame.Count ? Fit(frame[y], usableWidth) : new string(' ', usableWidth);

      if (y < previousFrame.Count && previousFrame[y] == line)
        continue;

      Console.SetCursorPosition(0, y);
      Console.Write(line);

      if (y < previousFrame.Count)
        previousFrame[y] = line;
      else
        previousFrame.Add(line);
    }
  }

  private static Command ParseCommand(string input)
  {
    string[] tokens = Tokenize(input);

    if (tokens.Length == 0)
      throw new FormatException("Empty command.");

    List<Argument> arguments = [];

    for (int i = 1; i < tokens.Length; i++)
    {
      string token = tokens[i];

      if (!IsOption(token))
        throw new FormatException($"Unexpected argument: {token}");

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

        if (i + 1 < tokens.Length && !IsOption(tokens[i + 1]))
          value = tokens[++i];
      }

      if (string.IsNullOrWhiteSpace(name))
        throw new FormatException("Invalid option name.");

      arguments.Add(new Argument
      {
        Name = name,
        Value = value
      });
    }

    return new Command
    {
      CommandText = tokens[0],
      Arguments = [.. arguments]
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
          quote = '\0';
        else
          current.Append(character);

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
      throw new FormatException("Unclosed quotation mark.");

    if (started)
      tokens.Add(current.ToString());

    return [.. tokens];
  }
}
