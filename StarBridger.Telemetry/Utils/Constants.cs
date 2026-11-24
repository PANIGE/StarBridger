using StarBridger.Contracts.Core.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.Telemetry.Utils;

internal static class Constants
{
  internal static string ExtractPath { get; } = Path.Combine(Folders.DataDirectory, "Extracts");
}