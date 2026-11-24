using Newtonsoft.Json;
using StarBridger.Contracts.Telemetry.Game.Versionning;

namespace StarBridger.Contracts.Telemetry.Game.v1;

public sealed record TelemetryGauge : ContractV1, ICommonTelemetry
{
  [JsonProperty("current")]
  public float Current { get; init; }

  [JsonProperty("max")]
  public float Max { get; init; }
}

public sealed record TelemetryAvailability : ContractV1, ICommonTelemetry
{
  [JsonProperty("enabled")]
  public bool Enabled { get; init; }

  [JsonProperty("active")]
  public bool Active { get; init; }
}