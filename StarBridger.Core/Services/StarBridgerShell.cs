using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StarBridger.Contracts.Core.Shell;
using StarBridger.Contracts.Core.Shell.Definitions;
using StarBridger.Core.Managers;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.Core.Services;

public sealed class StarBridgerShell : IHostedService
{
  private readonly ILogger _logger;
  private readonly Dictionary<string, CommandHandler> _commands = new(StringComparer.OrdinalIgnoreCase);
  private readonly List<CommandDefinition> _definitions = new();
  private readonly StateStore<List<CommandDefinition>> _definitionStore;
  private readonly object _sync = new();

  public StarBridgerShell(ILogger<StarBridgerShell> logger)
  {
    _logger = logger;
    _definitionStore = new StateStore<List<CommandDefinition>>(_definitions.ToList(), logger);
  }

  #region Command Registration

  public void RegisterCommand(CommandDefinition definition, CommandHandler handler)
  {
    ArgumentNullException.ThrowIfNull(definition);
    ArgumentNullException.ThrowIfNull(handler);

    if (_commands.ContainsKey(definition.CommandText))
    {
      throw new InvalidOperationException($"Command '{definition.CommandText}' is already registered.");
    }

    _commands.Add(definition.CommandText, handler);
    _definitions.Add(definition);

    _definitionStore.Publish(_definitions.ToList());

    _logger.LogInformation("Registered command {CommandText}", definition.CommandText);
  }

  public void UnregisterCommand(CommandDefinition definition)
  {
    ArgumentNullException.ThrowIfNull(definition);
    _logger.LogInformation("Unregistering command {CommandText}", definition.CommandText);
    _commands.Remove(definition.CommandText);
    _definitions.RemoveAll(d => d.CommandText == definition.CommandText);
    _definitionStore.Publish(_definitions.ToList());
  }

  public void UnregisterServiceCommands(string service)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(service);

    lock (_sync)
    {
      List<CommandDefinition> definitions = _definitions.FindAll(
        definition => definition.Service.Equals(
          service,
          StringComparison.OrdinalIgnoreCase));

      foreach (CommandDefinition definition in definitions)
      {
        _commands.Remove(definition.CommandText);
      }

      _definitions.RemoveAll(
        definition => definition.Service.Equals(
          service,
          StringComparison.OrdinalIgnoreCase));

      _definitionStore.Publish(_definitions.ToList());
    }

    _logger.LogInformation(
      "Unregistered commands for service {Service}",
      service);
  }

  #endregion

  public async Task<bool> ExecuteCommand(Command command, Action<string>? output, Action<object>? data, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(command);
    CommandHandler? handler;

    lock (_sync)
    {
      _commands.TryGetValue(command.CommandText, out handler);
    }

    if (handler is null)
    {
      _logger.LogWarning("Command {CommandText} not found", command.CommandText);

      return false;
    }

    try
    {
      await handler.Invoke(command, output, data, cancellationToken);
      return true;
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      _logger.LogInformation("Command {CommandText} cancelled", command.CommandText);

      throw;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error executing command {CommandText}", command.CommandText);
      return false;
    }
  }

  #region Hosted Service Implementation

  // Shell is a singleton, so no need to worry about multiple instances of the shell running at the same time.
  public Task StartAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("StarBridger Shell starting");
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("StarBridger Shell stopping");
    return _definitionStore.StopAsync();
  }

  public IReadOnlyList<CommandDefinition> GetCommandDefinitions()
  {
    lock (_sync)
    {
      return _definitions.ToList();
    }
  }
  #endregion

  public delegate Task CommandHandler(Command command, Action<string>? output, Action<object>? data, CancellationToken cancellationToken);
}