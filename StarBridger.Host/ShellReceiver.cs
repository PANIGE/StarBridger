using Microsoft.Extensions.Hosting;
using StarBridger.Contracts.Core.Shell;
using StarBridger.Contracts.Core.Shell.Definitions;
using StarBridger.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using StarBridger.Contracts.Host;
using StarBridger.Core.Utils;

namespace StarBridger.Host;

internal static class ShellReceiver
{

  internal static void SetupShell(IHostApplicationLifetime lifetime, StarBridgerShell shell)
  {
    List<(CommandDefinition, StarBridgerShell.CommandHandler)> commands = new()
    {
      (new CommandDefinition
        {
          Service = nameof(Host),
          Name = "Status",
          Description = "Get the current status of StarBridger",
          Arguments = Array.Empty<ArgumentDefinition>()
        },
        GetStatus),
      (new CommandDefinition
        {
          Service = nameof(Host),
          Name = "Shutdown",
          Description = "Shutdown StarBridger",
          Arguments = Array.Empty<ArgumentDefinition>()
        }, async (_, output, data, cancellationToken) => Shutdown(output, cancellationToken, lifetime)),
      (new CommandDefinition
        {
          Service = nameof(Host),
          Name = "Help",
          Description = "Lists all available commands and their arguments."
        },
        (command, output, data, cancellationToken) =>
          GetHelp(command, output, data, cancellationToken, shell)),
    };

    foreach ((CommandDefinition definition, StarBridgerShell.CommandHandler handler) in commands)
    {
      shell.RegisterCommand(definition, handler);
    }
  }

  private static Task GetHelp(Command command, Action<string>? output, Action<object>? data, CancellationToken cancellationToken, StarBridgerShell shell)
  {
    cancellationToken.ThrowIfCancellationRequested();

    IReadOnlyList<CommandDefinition> definitions =
      shell.GetCommandDefinitions();

    if (command.Arguments.HasRawFlag())
    {
      data?.Invoke(definitions);
      return Task.CompletedTask;
    }

    output?.Invoke("=== StarBridger Commands ===");

    foreach (CommandDefinition definition in definitions
      .OrderBy(d => d.CommandText))
    {
      cancellationToken.ThrowIfCancellationRequested();

      output?.Invoke(
        $"{definition.CommandText} - {definition.Description}");

      foreach (ArgumentDefinition argument in definition.Arguments)
      {
        string requirement = argument.IsRequired
          ? "required"
          : "optional";

        output?.Invoke(
          $"  {argument.Name} ({requirement}) - {argument.Description}");
      }
    }

    output?.Invoke($"Total: {definitions.Count} commands");

    return Task.CompletedTask;
  }

  private static Task Shutdown(Action<string>? output, CancellationToken cancellationToken, IHostApplicationLifetime lifetime)
  {
    cancellationToken.ThrowIfCancellationRequested();

    output?.Invoke("StarBridger shutdown requested.");

    lifetime.StopApplication();

    return Task.CompletedTask;
  }

  private static async Task GetStatus(Command command, Action<string>? output, Action<object>? data, CancellationToken cancellationToken)
  {
    using Process process = Process.GetCurrentProcess();

    process.Refresh();

    TimeSpan cpuBefore = process.TotalProcessorTime;
    Stopwatch stopwatch = Stopwatch.StartNew();

    await Task.Delay(300, cancellationToken);

    process.Refresh();
    stopwatch.Stop();

    TimeSpan cpuUsed = process.TotalProcessorTime - cpuBefore;

    double cpuPercent = cpuUsed.TotalSeconds
      / (stopwatch.Elapsed.TotalSeconds * Environment.ProcessorCount)
      * 100.0;

    TimeSpan uptime = DateTime.Now - process.StartTime;

    double workingSetMb = process.WorkingSet64 / 1024.0 / 1024.0;
    double managedMemoryMb = GC.GetTotalMemory(false) / 1024.0 / 1024.0;

    if (command.Arguments.HasRawFlag())
    {
      data?.Invoke(new HostStatus
      {
        HostPath = System.Reflection.Assembly.GetEntryAssembly()?.Location ?? "",
        CpuUsage = cpuPercent,
        MemoryManaged = managedMemoryMb,
        MemoryWorkingSet = workingSetMb,
        ProcessID = process.Id,
        ThreadsCount = process.Threads.Count,
        UpTime = uptime
      });
      return;
    }

    output?.Invoke("=== StarBridger Host Status ===");
    output?.Invoke("Status: Running");
    output?.Invoke($"PID: {process.Id}");
    output?.Invoke($"Uptime: {uptime:dd\\.hh\\:mm\\:ss}");
    output?.Invoke($"CPU: {cpuPercent:F2}%");
    output?.Invoke($"RAM (Working Set): {workingSetMb:F2} MB");
    output?.Invoke($"Managed Memory: {managedMemoryMb:F2} MB");
    output?.Invoke($"Threads: {process.Threads.Count}");
  }
}