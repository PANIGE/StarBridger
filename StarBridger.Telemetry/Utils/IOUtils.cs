using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sc.Game.Telemetry.V1;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.Telemetry.Utils;

internal static class IOUtils
{
  private static string GetJsonString(VehicleFrame? vehicle, ActorFrame? actor, WeaponsFrame? weapons, DateTimeOffset? lastReceived)
  {
    JObject raw = new();
    JObject mapped = new();

    if (vehicle?.State is VehicleState vehicleState)
    {
      raw["vehicle"] = JToken.Parse(
        JsonFormatter.Default.Format(vehicle));

      mapped["vehicle"] = JToken.FromObject(
        StarCitizenTelemetryMapper.MapVehicle(vehicleState));
    }

    if (actor is not null)
    {
      raw["actor"] = JToken.Parse(
        JsonFormatter.Default.Format(actor));

      mapped["actor"] = JToken.FromObject(
        StarCitizenTelemetryMapper.MapActor(actor));
    }

    if (weapons is not null)
    {
      raw["weapons"] = JToken.Parse(
        JsonFormatter.Default.Format(weapons));

      mapped["weapons"] = JToken.FromObject(
        StarCitizenTelemetryMapper.MapWeapons(weapons));
    }

    JObject extract = new()
    {
      ["extractedAtUtc"] = DateTimeOffset.UtcNow.ToString("O"),
      ["lastReceivedAtUtc"] = lastReceived?.ToString("O"),
      ["raw"] = raw,
      ["mapped"] = mapped
    };

    return extract.ToString(Formatting.Indented);
  }

  public static string WriteExtract(VehicleFrame? vehicle, ActorFrame? actor, WeaponsFrame? weapons, DateTimeOffset? lastReceived)
  {
    string json = GetJsonString(vehicle, actor, weapons, lastReceived);
    string filename = $"Extract_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.json";
    Directory.CreateDirectory(Utils.Constants.ExtractPath);
    File.WriteAllText(Path.Join(Utils.Constants.ExtractPath, filename), json);
    return Path.Join(Utils.Constants.ExtractPath, filename);
  }
}