
using Microsoft.Extensions.Logging;
using System;

namespace StarBridger.Core.Managers;

public class StateStore<TState> : EventManager<TState> where TState : class
{
  public TState State { get; private set; }

  public StateStore(TState initialState, ILogger logger) : base(logger)
  {
    ArgumentNullException.ThrowIfNull(initialState);
    State = initialState;
  }

  protected override void CaptureInitialEvent(Subscription<TState> subscription)
  {
    subscription.Queue.Writer.TryWrite(State);
  }

  protected override TState PreparePublication(TState state)
  {
    State = state;
    return state;
  }
}