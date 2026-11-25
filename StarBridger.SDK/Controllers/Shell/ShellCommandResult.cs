using Newtonsoft.Json.Linq;
using StarBridger.Contracts.IPC.Shell;
using StarBridger.SDK.Exceptions;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace StarBridger.SDK.Controllers.Shell;

public sealed class ShellCommandResult
{
  public IAsyncEnumerable<ShellResponse> Responses { get; }

  private CompletedShellResponse? _completion;

  private readonly Lock _dataLock = new();
  private readonly List<DataShellResponse> _data = new();

  public bool Completed => _completion is not null;
  public bool Success => _completion?.Success ?? false;
  public string? Error => _completion?.ErrorMessage;

  internal ShellCommandResult(IAsyncEnumerable<ShellResponse> responses)
  {
    Responses = responses;
  }

  internal void Update(ShellResponse response)
  {
    switch (response)
    {
      case CompletedShellResponse completed:
        _completion = completed;
        break;

      case DataShellResponse data:
        lock (_dataLock)
        {
          _data.Add(data);
        }

        break;
    }
  }

  public T?[] GetData<T>()
  {
    lock (_dataLock)
    {
      return [.. _data.Select(d => d.Payload is JToken token ? token.ToObject<T>() : throw new ShellCommandException("Data inside payload was malformed"))];
    }
  }

  public int FlushDataResult()
  {
    lock (_dataLock)
    {
      int count = _data.Count;
      _data.Clear();
      return count;
    }
  }
}