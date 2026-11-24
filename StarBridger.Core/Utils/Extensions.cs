using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.Core.Utils;

public static class Extensions
{
  public static Version Normalize(this Version version)
  {
    ArgumentNullException.ThrowIfNull(version);
    return new Version(version.Major, version.Minor, Math.Max(version.Build, 0));
  }
}
