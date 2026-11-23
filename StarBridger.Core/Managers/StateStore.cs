
using System;
using Microsoft.Extensions.Logging;

namespace StarBridger.Core.Managers;

public class StateStore<TState> : EventManager<TState> where TState : class
{
  private TState _state;

  public StateStore(TState initialState, ILogger logger) : base(logger)
  {
    ArgumentNullException.ThrowIfNull(initialState);
    _state = initialState;
  }

  protected override void CaptureInitialEvent(Subscription<TState> subscription)
  {
    subscription.Queue.Writer.TryWrite(_state);
  }

  protected override TState PreparePublication(TState state)
  {
    _state = state;
    return state;
  }
}
