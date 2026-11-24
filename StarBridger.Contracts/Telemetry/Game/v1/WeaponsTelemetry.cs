using Newtonsoft.Json;
using StarBridger.Contracts.Telemetry.Game.Versionning;

namespace StarBridger.Contracts.Telemetry.Game.v1;

public sealed record WeaponsTelemetry : ContractV1, IWeaponTelemetry
{
  [JsonProperty("settings")]
  public WeaponsSettingsTelemetry? Settings { get; init; }

  [JsonProperty("preset_index")]
  public uint PresetIndex { get; init; }

  [JsonProperty("preset_name")]
  public string PresetName { get; init; } = string.Empty;

  [JsonProperty("weapons")]
  public WeaponTelemetry[] Weapons { get; init; } = [];

  [JsonProperty("countermeasures")]
  public CountermeasureTelemetry[] Countermeasures { get; init; } = [];
}

public sealed record WeaponsSettingsTelemetry : ContractV1, IWeaponTelemetry
{
  [JsonProperty("fallback_convergence_distance")]
  public bool FallbackConvergenceDistance { get; init; }

  [JsonProperty("magnified_ui")]
  public bool MagnifiedUi { get; init; }

  [JsonProperty("precision_lines")]
  public bool PrecisionLines { get; init; }

  [JsonProperty("staggered_fire")]
  public bool StaggeredFire { get; init; }

  [JsonProperty("use_lag_pips")]
  public bool UseLagPips { get; init; }

  [JsonProperty("use_single_pips")]
  public bool UseSinglePips { get; init; }

  [JsonProperty("use_fading_pips")]
  public bool UseFadingPips { get; init; }

  [JsonProperty("gimbals_locked")]
  public bool GimbalsLocked { get; init; }
}

public sealed record WeaponTelemetry : ContractV1, IWeaponTelemetry
{
  [JsonProperty("name")]
  public string Name { get; init; } = string.Empty;

  [JsonProperty("is_powered")]
  public bool IsPowered { get; init; }

  [JsonProperty("ready_to_fire")]
  public bool ReadyToFire { get; init; }

  [JsonProperty("contained_in_preset")]
  public bool ContainedInPreset { get; init; }
}

public sealed record CountermeasureTelemetry : ContractV1, IWeaponTelemetry
{
  [JsonProperty("type")]
  public CountermeasureType Type { get; init; }

  [JsonProperty("amount")]
  public uint Amount { get; init; }
}