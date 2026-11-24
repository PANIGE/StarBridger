using StarBridger.Contracts.Core.Shell;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.IPC.Utils;

public static class Extensions
{
  public static bool TryGetValue<TOut>(this Argument[] arguments, string name, out TOut value)
  {
    Argument? argument = Array.Find(arguments, a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    if (argument is not null)
    {
      value = (TOut) Convert.ChangeType(argument.Value, typeof(TOut));
      return true;
    }

    value = default!;
    return false;
  }
}