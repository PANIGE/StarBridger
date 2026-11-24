using Newtonsoft.Json;

namespace StarBridger.Contracts.Telemetry.Game;

public sealed record ActorTelemetry
{
  [JsonProperty("vitals")]
  public ActorVitalsTelemetry? Vitals { get; init; }

  [JsonProperty("state")]
  public ActorStateTelemetry? State { get; init; }

  [JsonProperty("environment")]
  public ActorEnvironmentTelemetry? Environment { get; init; }
}

public sealed record ActorVitalsTelemetry
{
  [JsonProperty("health")]
  public TelemetryGauge? Health { get; init; }

  [JsonProperty("oxygen")]
  public TelemetryGauge? Oxygen { get; init; }

  [JsonProperty("hunger")]
  public TelemetryGauge? Hunger { get; init; }

  [JsonProperty("thirst")]
  public TelemetryGauge? Thirst { get; init; }

  [JsonProperty("body_temperature")]
  public TelemetryGauge? BodyTemperature { get; init; }

  [JsonProperty("heart_rate")]
  public float HeartRate { get; init; }

  [JsonProperty("gravity")]
  public float Gravity { get; init; }

  [JsonProperty("pressure")]
  public float Pressure { get; init; }

  [JsonProperty("intoxication_level")]
  public int IntoxicationLevel { get; init; }
}

public sealed record ActorStateTelemetry
{
  [JsonProperty("locomotion")]
  public ActorLocomotionState Locomotion { get; init; }

  [JsonProperty("crew_role")]
  public ActorCrewRole CrewRole { get; init; }

  [JsonProperty("seat_name")]
  public string SeatName { get; init; } = string.Empty;

  [JsonProperty("uec_balance")]
  public long UecBalance { get; init; }
}

public sealed record ActorEnvironmentTelemetry
{
  [JsonProperty("external_temperature")]
  public float ExternalTemperature { get; init; }

  [JsonProperty("ambient_radiation_rate")]
  public float AmbientRadiationRate { get; init; }

  [JsonProperty("breathable_atmosphere")]
  public bool BreathableAtmosphere { get; init; }
}