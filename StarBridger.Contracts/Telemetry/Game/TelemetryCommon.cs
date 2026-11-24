using Newtonsoft.Json;

namespace StarBridger.Contracts.Telemetry.Game;

public sealed record TelemetryGauge
{
  [JsonProperty("current")]
  public float Current { get; init; }

  [JsonProperty("max")]
  public float Max { get; init; }
}

public sealed record TelemetryAvailability
{
  [JsonProperty("enabled")]
  public bool Enabled { get; init; }

  [JsonProperty("active")]
  public bool Active { get; init; }
}