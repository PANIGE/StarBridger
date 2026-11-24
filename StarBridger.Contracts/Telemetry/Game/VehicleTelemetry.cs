using Newtonsoft.Json;

namespace StarBridger.Contracts.Telemetry.Game;

public sealed record VehicleTelemetry
{
  [JsonProperty("name")]
  public string Name { get; init; } = string.Empty;

  [JsonProperty("class_name")]
  public string ClassName { get; init; } = string.Empty;

  [JsonProperty("manufacturer")]
  public string Manufacturer { get; init; } = string.Empty;

  [JsonProperty("type")]
  public VehicleType Type { get; init; }

  [JsonProperty("lights_enabled")]
  public bool LightsEnabled { get; init; }

  [JsonProperty("doors")]
  public VehicleDoorsTelemetry? Doors { get; init; }

  [JsonProperty("annunciators")]
  public VehicleAnnunciatorsTelemetry? Annunciators { get; init; }

  [JsonProperty("flight")]
  public VehicleFlightTelemetry? Flight { get; init; }

  [JsonProperty("power")]
  public VehiclePowerTelemetry? Power { get; init; }

  [JsonProperty("shields")]
  public VehicleShieldsTelemetry? Shields { get; init; }

  [JsonProperty("health")]
  public float Health { get; init; }

  [JsonProperty("master_mode")]
  public VehicleMasterModeTelemetry? MasterMode { get; init; }
}

public sealed record VehicleDoorsTelemetry
{
  [JsonProperty("open_state")]
  public VehicleDoorOpenState OpenState { get; init; }

  [JsonProperty("lock_state")]
  public VehicleDoorLockState LockState { get; init; }
}

public sealed record VehicleFlightTelemetry
{
  [JsonProperty("state")]
  public VehicleFlightStateTelemetry? State { get; init; }

  [JsonProperty("settings")]
  public VehicleFlightSettingsTelemetry? Settings { get; init; }

  [JsonProperty("instruments")]
  public VehicleFlightInstrumentsTelemetry? Instruments { get; init; }
}

public sealed record VehicleFlightStateTelemetry
{
  [JsonProperty("is_landed")]
  public bool IsLanded { get; init; }

  [JsonProperty("gear")]
  public VehicleComponentDeploymentState? Gear { get; init; }

  [JsonProperty("vtol")]
  public VehicleComponentDeploymentState? Vtol { get; init; }

  [JsonProperty("transformation")]
  public VehicleComponentDeploymentState? Transformation { get; init; }
}

public sealed record VehicleFlightSettingsTelemetry
{
  [JsonProperty("ifcs_coupled")]
  public TelemetryAvailability? IfcsCoupled { get; init; }

  [JsonProperty("ifcs_esp")]
  public TelemetryAvailability? IfcsEsp { get; init; }

  [JsonProperty("ifcs_gravity_compensation")]
  public TelemetryAvailability? IfcsGravityCompensation { get; init; }

  [JsonProperty("ifcs_g_safe")]
  public TelemetryAvailability? IfcsGSafe { get; init; }

  [JsonProperty("ifcs_cruise_mode")]
  public TelemetryAvailability? IfcsCruiseMode { get; init; }

  [JsonProperty("ifcs_spacebreak_enables_boost")]
  public bool IfcsSpacebreakEnablesBoost { get; init; }

  [JsonProperty("ifcs_speed_limiter")]
  public TelemetryAvailability? IfcsSpeedLimiter { get; init; }

  [JsonProperty("ifcs_proximity_assist")]
  public TelemetryAvailability? IfcsProximityAssist { get; init; }

  [JsonProperty("ifcs_proximity_assist_landing")]
  public TelemetryAvailability? IfcsProximityAssistLanding { get; init; }

  [JsonProperty("ifcs_stability")]
  public TelemetryAvailability? IfcsStability { get; init; }

  [JsonProperty("ifcs_core")]
  public TelemetryAvailability? IfcsCore { get; init; }

  [JsonProperty("advanced_hud_enabled_scm")]
  public bool AdvancedHudEnabledScm { get; init; }

  [JsonProperty("advanced_hud_enabled_nav")]
  public bool AdvancedHudEnabledNav { get; init; }

  [JsonProperty("advanced_hud_labels")]
  public bool AdvancedHudLabels { get; init; }

  [JsonProperty("course_prediction")]
  public bool CoursePrediction { get; init; }

  [JsonProperty("ifcs_wind_compensation")]
  public TelemetryAvailability? IfcsWindCompensation { get; init; }

  [JsonProperty("ifcs_g_safe_disabled_by_boost")]
  public bool IfcsGSafeDisabledByBoost { get; init; }

  [JsonProperty("ifcs_command_behaviour")]
  public TelemetryAvailability? IfcsCommandBehaviour { get; init; }

  [JsonProperty("flight_light_amplification")]
  public bool FlightLightAmplification { get; init; }
}

public sealed record VehicleFlightInstrumentsTelemetry
{
  [JsonProperty("speed")]
  public float Speed { get; init; }

  [JsonProperty("hydrogen_fuel")]
  public float HydrogenFuel { get; init; }

  [JsonProperty("quantum_fuel")]
  public float QuantumFuel { get; init; }

  [JsonProperty("g_meter")]
  public TelemetryGauge? GMeter { get; init; }

  [JsonProperty("boost")]
  public VehicleThrusterBoostTelemetry? Boost { get; init; }

  [JsonProperty("altitude")]
  public float? Altitude { get; init; }

  [JsonProperty("radar_altitude")]
  public float? RadarAltitude { get; init; }

  [JsonProperty("vertical_speed_indicator")]
  public float VerticalSpeedIndicator { get; init; }

  [JsonProperty("atmospheric_density")]
  public float AtmosphericDensity { get; init; }

  [JsonProperty("heading")]
  public float Heading { get; init; }
}

public sealed record VehicleThrusterBoostTelemetry
{
  [JsonProperty("engaged")]
  public bool Engaged { get; init; }

  [JsonProperty("output")]
  public float Output { get; init; }

  [JsonProperty("reserves")]
  public float Reserves { get; init; }

  [JsonProperty("critical")]
  public bool Critical { get; init; }
}

public sealed record VehiclePowerTelemetry
{
  [JsonProperty("powerplants")]
  public bool Powerplants { get; init; }

  [JsonProperty("engines")]
  public bool Engines { get; init; }

  [JsonProperty("weapons")]
  public bool Weapons { get; init; }

  [JsonProperty("coolers")]
  public bool Coolers { get; init; }

  [JsonProperty("life_support")]
  public bool LifeSupport { get; init; }

  [JsonProperty("gravity_generators")]
  public bool GravityGenerators { get; init; }

  [JsonProperty("sensors")]
  public bool Sensors { get; init; }

  [JsonProperty("shield_generators")]
  public bool ShieldGenerators { get; init; }

  [JsonProperty("shield_emitters")]
  public bool ShieldEmitters { get; init; }
}

public sealed record VehicleAnnunciatorsTelemetry
{
  [JsonProperty("pwr")]
  public VehicleAnnunciatorState Pwr { get; init; }

  [JsonProperty("wpn")]
  public VehicleAnnunciatorState Wpn { get; init; }

  [JsonProperty("thr")]
  public VehicleAnnunciatorState Thr { get; init; }

  [JsonProperty("shld")]
  public VehicleAnnunciatorState Shld { get; init; }

  [JsonProperty("cool")]
  public VehicleAnnunciatorState Cool { get; init; }

  [JsonProperty("qdrv")]
  public VehicleAnnunciatorState Qdrv { get; init; }

  [JsonProperty("radr")]
  public VehicleAnnunciatorState Radr { get; init; }

  [JsonProperty("prox")]
  public VehicleAnnunciatorState Prox { get; init; }

  [JsonProperty("hit")]
  public VehicleAnnunciatorState Hit { get; init; }

  [JsonProperty("misl")]
  public VehicleAnnunciatorState Misl { get; init; }
}

public sealed record VehicleShieldsTelemetry
{
  [JsonProperty("level")]
  public float Level { get; init; }

  [JsonProperty("faces")]
  public float[] Faces { get; init; } = [];
}

public sealed record VehicleMasterModeTelemetry
{
  [JsonProperty("mode")]
  public VehicleMasterMode Mode { get; init; }

  [JsonProperty("spooled")]
  public bool Spooled { get; init; }
}