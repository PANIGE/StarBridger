
using Google.Protobuf;
using Game = Sc.Game.Telemetry.V1;
using System;

namespace StarBridger.Telemetry.Utils;

internal static class TelemetryComparer
{
  private const float SPEED_EPSILON = 0.01f;
  public static bool HasChanged<T>(T? previous, T? current) where T : class, IMessage<T>
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

  public static bool HasVehicleChanged(Game.VehicleFrame? previous, Game.VehicleFrame? current)
  {
    if (previous is null || current is null)
    {
      return previous != current;
    }

    Game.VehicleFrame a = previous;
    Game.VehicleFrame b = current;

    NormalizeVehicle(a);
    NormalizeVehicle(b);

    return !a.Equals(b);
  }

  private static void NormalizeVehicle(Game.VehicleFrame frame)
  {
    Game.VehicleFlightInstruments? instruments =
      frame.State?.Flight?.Instruments;

    if (instruments is not null && MathF.Abs(instruments.Speed) < SPEED_EPSILON)
    {
      instruments.Speed = 0f;
    }

    if (instruments is not null && MathF.Abs(instruments.VerticalSpeedIndicator) < SPEED_EPSILON)
    {
      instruments.VerticalSpeedIndicator = 0f;
    }
  }
}
