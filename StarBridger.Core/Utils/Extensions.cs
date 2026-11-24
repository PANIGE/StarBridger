using StarBridger.Contracts.Core.Shell;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace StarBridger.Core.Utils;

public static class Extensions
{
  public static Version Normalize(this Version version)
  {
    ArgumentNullException.ThrowIfNull(version);
    return new Version(version.Major, version.Minor, Math.Max(version.Build, 0));
  }

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

  public static bool HasRawFlag(this Argument[] arguments)
  {
    return arguments.TryGetValue<object>("raw", out _);
  }
}
