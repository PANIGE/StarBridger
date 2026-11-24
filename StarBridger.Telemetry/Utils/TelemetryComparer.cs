
using Google.Protobuf;
using System;

namespace StarBridger.Telemetry.Utils;

internal static class TelemetryComparer
{
  public static bool HasChanged<T>(T? previous, T? current)
    where T : class, IMessage<T>
  {
    if (ReferenceEquals(previous, current))
    {
      return false;
    }

    if (previous is null || current is null)
    {
      return true;
    }

    return !previous.Equals(current);
  }
}
