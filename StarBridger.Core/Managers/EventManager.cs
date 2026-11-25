using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace StarBridger.Core.Managers;

public class EventManager<TEvent> where TEvent : class
{
  private const int DEFAULT_QUEUE_CAPACITY = 128;

  private readonly Lock _sync = new();
  private readonly Dictionary<Guid, Subscription<TEvent>> _subscribers = new();
  private readonly Dictionary<Guid, HashSet<Guid>> _owners = new();
  private readonly ILogger _logger;

  private Task? _stopTask;
  private bool _stopping;

  protected EventManager(ILogger logger)
  {
    ArgumentNullException.ThrowIfNull(logger);
    _logger = logger;
  }

  public int SubscriberCount
  {
    get
    {
      lock (_sync)
      {
        return _subscribers.Count;
      }
    }
  }

  #region Subscription Management

  public Guid Subscribe(Action<TEvent> callback, Guid? ownerId = null, int queueCapacity = DEFAULT_QUEUE_CAPACITY, BoundedChannelFullMode fullMode = BoundedChannelFullMode.Wait)
  {
    ArgumentNullException.ThrowIfNull(callback);
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(queueCapacity);

    lock (_sync)
    {
      if (_stopping)
        throw new InvalidOperationException("The event manager is stopping.");

      Guid subscriberId = Guid.NewGuid();
      Subscription<TEvent> subscription = new(
        subscriberId, ownerId, callback, queueCapacity, fullMode);

      _subscribers.Add(subscriberId, subscription);

      if (ownerId.HasValue)
      {
        if (!_owners.TryGetValue(ownerId.Value, out HashSet<Guid>? ids))
        {
          ids = new HashSet<Guid>();
          _owners.Add(ownerId.Value, ids);
        }

        ids.Add(subscriberId);
      }

      subscription.Worker = Task.Run(() => ProcessSubscriptionAsync(subscription));
      CaptureInitialEvent(subscription);
      return subscriberId;
    }
  }

  public void Unsubscribe(Guid subscriberId)
  {
    Subscription<TEvent>? subscription;

    lock (_sync)
    {
      if (!_subscribers.Remove(subscriberId, out subscription))
        return;

      if (subscription.OwnerId is Guid ownerId &&
          _owners.TryGetValue(ownerId, out HashSet<Guid>? ids))
      {
        ids.Remove(subscriberId);
        if (ids.Count == 0)
          _owners.Remove(ownerId);
      }
    }

    subscription.Stop();
  }

  public void UnsubscribeByOwner(Guid ownerId)
  {
    RemoveOwnerSubscriptions(ownerId);
  }

  public async Task UnsubscribeByOwnerAsync(Guid ownerId)
  {
    Task[] workers = RemoveOwnerSubscriptions(ownerId);
    await Task.WhenAll(workers);
  }

  private Task[] RemoveOwnerSubscriptions(Guid ownerId)
  {
    List<Subscription<TEvent>> subscriptions = new();

    lock (_sync)
    {
      if (!_owners.Remove(ownerId, out HashSet<Guid>? ids))
        return [];

      foreach (Guid subscriberId in ids)
      {
        if (_subscribers.Remove(subscriberId, out Subscription<TEvent>? subscription))
          subscriptions.Add(subscription);
      }
    }

    foreach (Subscription<TEvent> subscription in subscriptions)
      subscription.Stop();

    return [.. subscriptions.Select(subscription => subscription.Worker)];
  }

  #endregion

  #region Event Publishing

  public int Publish(TEvent eventData)
  {
    ArgumentNullException.ThrowIfNull(eventData);

    Subscription<TEvent>[] subscriptions;
    lock (_sync)
    {
      if (_stopping)
        return 0;

      subscriptions = [.. _subscribers.Values];
      eventData = PreparePublication(eventData);
    }

    int accepted = 0;
    foreach (Subscription<TEvent> subscription in subscriptions)
    {
      if (!subscription.IsActive)
        continue;

      if (subscription.Queue.Writer.TryWrite(eventData))
      {
        accepted++;
        continue;
      }

      if (!subscription.IsActive)
        continue;

      long rejected = subscription.RecordRejectedEvent();
      if (rejected == 1 || rejected % 100 == 0)
      {
        _logger.LogWarning(
          "Subscriber {SubscriberId} rejected {RejectedCount} events of {EventType}",
          subscription.Id,
          rejected,
          typeof(TEvent).Name);
      }
    }

    return accepted;
  }

  public Task StopAsync()
  {
    lock (_sync)
    {
      if (_stopTask is not null)
        return _stopTask;

      _stopping = true;
      Subscription<TEvent>[] subscriptions = [.. _subscribers.Values];
      _subscribers.Clear();
      _owners.Clear();

      foreach (Subscription<TEvent> subscription in subscriptions)
        subscription.Stop();

      _stopTask = Task.WhenAll(subscriptions.Select(subscription => subscription.Worker));
      return _stopTask;
    }
  }

  private async Task ProcessSubscriptionAsync(Subscription<TEvent> subscription)
  {
    try
    {
      await foreach (TEvent eventData in subscription.Queue.Reader.ReadAllAsync(
        subscription.Cancellation.Token))
      {
        if (!subscription.IsActive)
          break;

        try
        {
          subscription.Callback(eventData);
        }
        catch (Exception exception)
        {
          _logger.LogError(
            exception,
            "Subscriber {SubscriberId} failed handling {EventType}",
            subscription.Id,
            typeof(TEvent).Name);
        }
      }
    }
    catch (OperationCanceledException) when (!subscription.IsActive)
    {
      // Unsubscription or shutdown: pending events are discarded.
    }
    finally
    {
      subscription.Cancellation.Dispose();
    }
  }

  #endregion

  #region Virtuals for StateStore
  protected virtual void CaptureInitialEvent(Subscription<TEvent> subscription)
  {
  }

  protected virtual TEvent PreparePublication(TEvent eventData)
  {
    return eventData;
  }
  #endregion

}