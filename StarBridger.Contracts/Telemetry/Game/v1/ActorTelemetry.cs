using Newtonsoft.Json;
using StarBridger.Contracts.Telemetry.Game.Versionning;

namespace StarBridger.Contracts.Telemetry.Game.v1;

public sealed record ActorTelemetry : ContractV1, IActorTelemetry
{
  [JsonProperty("vitals")]
  public ActorVitalsTelemetry? Vitals { get; init; }

  [JsonProperty("state")]
  public ActorStateTelemetry? State { get; init; }

  [JsonProperty("environment")]
  public ActorEnvironmentTelemetry? Environment { get; init; }
}

public sealed record ActorVitalsTelemetry : ContractV1, IActorTelemetry
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

public sealed record ActorStateTelemetry : ContractV1, IActorTelemetry
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

public sealed record ActorEnvironmentTelemetry : ContractV1, IActorTelemetry
{
  [JsonProperty("external_temperature")]
  public float ExternalTemperature { get; init; }

  [JsonProperty("ambient_radiation_rate")]
  public float AmbientRadiationRate { get; init; }

  [JsonProperty("breathable_atmosphere")]
  public bool BreathableAtmosphere { get; init; }
}