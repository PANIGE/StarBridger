using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.Contracts.Telemetry.Game;

public static class Constants
{
  public static Version LatestVersion { get; } = new Version(1, 0, 0);
  public static Version LowestSupportedVersion { get; } = new Version(1, 0, 0);
}
