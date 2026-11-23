using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StarBridger.Contracts.Core.Shell;
using StarBridger.Contracts.Core.Shell.Definitions;
using StarBridger.Contracts.Enums;
using StarBridger.Core.Managers;
using StarBridger.Core.Services;
using StarBridger.IPC.Utils;
using System.Collections.Concurrent;
using System.IO.Pipes;

namespace StarBridger.IPC.Services;

public class MainIpcBusService : BackgroundService
{
  private readonly ILogger<MainIpcBusService> _logger;
  private readonly StarBridgerShell _shell;
  private readonly ConcurrentDictionary<Guid, IpcSession> _sessions = new();

  public Guid MainIpcId { get; } = Guid.NewGuid();
  public ConcurrentDictionary<Intents, object?> States { get; } = new();

  // actions are for unsubscribing
  private readonly Dictionary<Guid, Action> _eventSubscriptions = new();

  public MainIpcBusService(ILogger<MainIpcBusService> logger, StarBridgerShell shell)
  {
    _logger = logger;
    _shell = shell;
  }

  private readonly object _broadcastLock = new();

  #region Service Execution
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    List<Task> sessions = new();

    _logger.LogInformation("Main IPC listening on {PipeName}", Contracts.IPC.PipeNames.Main);

    try
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        NamedPipeServerStream pipe = new(
          Contracts.IPC.PipeNames.Main,
          PipeDirection.Out,
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

      _logger.LogInformation("Main IPC stopped");
    }
  }

  public override Task StopAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("Stopping Main IPC Bus");

    Action[] unsubscribeActions;

    lock (_broadcastLock)
    {
      unsubscribeActions = _eventSubscriptions.Values.ToArray();
      _eventSubscriptions.Clear();
    }

    foreach (Action unsubscribe in unsubscribeActions)
    {
      try
      {
        unsubscribe.Invoke();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error unsubscribing from event");
      }
    }

    return base.StopAsync(cancellationToken);
  }

  #endregion

  #region handling clients

  private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken stoppingToken)
  {
    IpcSession session = new(pipe, stoppingToken);
    lock (_broadcastLock)
    {
      session.Send(session.PipeID, Contracts.Enums.Intents.Identity, force: true);
      foreach ((Intents intent, object? value) in States)
      {
        session.Send(value, intent);
      }

      _sessions.TryAdd(session.PipeID, session);
    }

    try
    {
      await session.ProcessAsync();
    }
    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
    {
      // Normal host shutdown.
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error in IPC session {PipeID}", session.PipeID);
    }
    finally
    {
      _sessions.TryRemove(session.PipeID, out _);
      await pipe.DisposeAsync();
    }
  }

  public bool AddEventListener<TEvent>(EventManager<TEvent> eventManager, Intents intents) where TEvent : class
  {
    try
    {
      bool isStateStore = eventManager is StateStore<TEvent>;

      Guid id = eventManager.Subscribe(eventData =>
      {
        lock (_broadcastLock)
        {
          if (isStateStore)
          {
            States[intents] = eventData;
          }

          foreach (IpcSession session in _sessions.Values)
          {
            session.Send(eventData, intents);
          }
        }
      }, MainIpcId);

      _eventSubscriptions.Add(id, () => eventManager.Unsubscribe(id));

      return true;
    }
    catch (Exception ex)
    {
      _logger.LogError(
        ex,
        "Failed to add event listener for {EventType}",
        typeof(TEvent).Name);

      return false;
    }
  }

  #endregion

  #region register Shell Commands

  public void RegisterShellCommands()
  {
    CommandDefinition setIntent = new()
    {
      Name = "intents.set",
      Service = nameof(IPC),
      Description = "Sets the enabled intents for a Main IPC connection.",
      Arguments =
      [
        new()
        {
          Name = "intents",
          Description = "Intents to set in numbers",
          IsRequired = true
        },
        new()
        {
          Name = "id",
          Description = "The ID of the session Pipe for which to set intents",
          IsRequired = true
        },
      ]
    };

    _shell.RegisterCommand(setIntent, SetIntentCommand);
  }

  private Task SetIntentCommand(Command command, Action<string>? output, CancellationToken cancellationToken)
  {
    if (!command.Arguments.TryGetValue("intents", out string? intentsString) ||
        !command.Arguments.TryGetValue("id", out string? idString) ||
        !Guid.TryParse(idString, out Guid pipeId) ||
        !Enum.TryParse<Intents>(intentsString, out Intents intents))
    {
      output?.Invoke("Invalid arguments. check that all arguments are provided and valid.");
      throw new ArgumentException("Invalid arguments for intents.set command");
    }

    if (_sessions.TryGetValue(pipeId, out IpcSession? session))
    {
      lock (_broadcastLock)
      {
        Intents added = intents & ~session.Intents;

        session.Intents = intents;

        foreach ((Intents intent, object? value) in States)
        {
          if ((added & intent) == intent)
          {
            session.Send(value, intent);
          }
        }
      }

      output?.Invoke($"Intents for session {pipeId} set to {intents}");
    }
    else
    {
      output?.Invoke($"No session found with ID {pipeId}");
    }

    return Task.CompletedTask;
  }

  #endregion
}