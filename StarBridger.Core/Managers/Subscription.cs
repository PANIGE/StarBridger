using System.Threading.Channels;

namespace StarBridger.Core.Managers;

public sealed record Subscription<TEvent>(Guid Id, Guid? OwnerId, Action<TEvent> Callback, Channel<TEvent> Queue)
{
  internal int _stopped;
  internal long _rejectedEvents;

  public Subscription(Guid id, Guid? ownerId, Action<TEvent> callback, int queueCapacity, BoundedChannelFullMode fullMode) : this(id, ownerId, callback, Channel.CreateBounded<TEvent>(new BoundedChannelOptions(queueCapacity)
  {
    SingleReader = true,
    SingleWriter = false,
    FullMode = fullMode
  }))
  {
  }

  public CancellationTokenSource Cancellation { get; } = new();
  public Task Worker { get; set; } = Task.CompletedTask;
  public bool IsActive => Volatile.Read(ref _stopped) == 0;

  public long RecordRejectedEvent()
  {
    return Interlocked.Increment(ref _rejectedEvents);
  }

  public void Stop()
  {
    if (Interlocked.Exchange(ref _stopped, 1) != 0)
      return;

    Queue.Writer.TryComplete();
    Cancellation.Cancel();
  }
}