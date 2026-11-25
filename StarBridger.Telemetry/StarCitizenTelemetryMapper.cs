using StarBridger.Contracts.Telemetry.Game.v1;
using System;
using System.Linq;
using Game = Sc.Game.Telemetry.V1;

namespace StarBridger.Telemetry;

internal static class StarCitizenTelemetryMapper
{
  internal static VehicleTelemetry MapVehicle(Game.VehicleState source)
  {
    return new VehicleTelemetry
    {
      Name = source.Name,
      ClassName = source.ClassName,
      Manufacturer = source.Manufacturer,
      Type = (VehicleType) source.Type,
      LightsEnabled = source.LightsEnabled,
      Doors = MapDoors(source.Doors),
      Annunciators = MapAnnunciators(source.Annunciators),
      Flight = MapFlight(source.Flight),
      Power = MapPower(source.Power),
      Shields = MapShields(source.Shields),
      Health = source.Health,
      MasterMode = MapMasterMode(source.MasterMode)
    };
  }

  internal static ActorTelemetry MapActor(Game.ActorFrame source)
  {
    return new ActorTelemetry
    {
      Vitals = MapVitals(source.Vitals),
      State = MapActorState(source.State),
      Environment = MapEnvironment(source.Environment)
    };
  }

  internal static WeaponsTelemetry MapWeapons(Game.WeaponsFrame source)
  {
    return new WeaponsTelemetry
    {
      Settings = MapWeaponsSettings(source.Settings),
      PresetIndex = source.PresetIndex,
      PresetName = source.PresetName,
      Weapons = [.. source.Weapons.Select(weapon => new WeaponTelemetry
      {
        Name = weapon.Name,
        IsPowered = weapon.IsPowered,
        ReadyToFire = weapon.ReadyToFire,
        ContainedInPreset = weapon.ContainedInPreset
      })],
      Countermeasures = [.. source.Countermeasures.Select(item => new CountermeasureTelemetry
      {
        Type = (CountermeasureType) item.Type,
        Amount = item.Amount
      })]
    };
  }

  private static TelemetryGauge? MapGauge(Game.Gauge? source)
  {
    return source is null ? null : new TelemetryGauge
    {
      Current = source.Current,
      Max = source.Max
    };
  }

  private static TelemetryAvailability? MapAvailability(Game.Availability? source)
  {
    return source is null ? null : new TelemetryAvailability
    {
      Enabled = source.Enabled,
      Active = source.Active
    };
  }

  private static VehicleDoorsTelemetry? MapDoors(Game.VehicleDoorState? source)
  {
    return source is null ? null : new VehicleDoorsTelemetry
    {
      OpenState = (VehicleDoorOpenState) source.OpenState,
      LockState = (VehicleDoorLockState) source.LockState
    };
  }

  private static VehicleAnnunciatorsTelemetry? MapAnnunciators(Game.VehicleAnnunciator? source)
  {
    return source is null ? null : new VehicleAnnunciatorsTelemetry
    {
      Pwr = (VehicleAnnunciatorState) source.Pwr,
      Wpn = (VehicleAnnunciatorState) source.Wpn,
      Thr = (VehicleAnnunciatorState) source.Thr,
      Shld = (VehicleAnnunciatorState) source.Shld,
      Cool = (VehicleAnnunciatorState) source.Cool,
      Qdrv = (VehicleAnnunciatorState) source.Qdrv,
      Radr = (VehicleAnnunciatorState) source.Radr,
      Prox = (VehicleAnnunciatorState) source.Prox,
      Hit = (VehicleAnnunciatorState) source.Hit,
      Misl = (VehicleAnnunciatorState) source.Misl
    };
  }

  private static VehicleFlightTelemetry? MapFlight(Game.VehicleFlight? source)
  {
    return source is null ? null : new VehicleFlightTelemetry
    {
      State = MapFlightState(source.State),
      Settings = MapFlightSettings(source.Settings),
      Instruments = MapInstruments(source.Instruments)
    };
  }

  private static VehicleFlightStateTelemetry? MapFlightState(Game.VehicleFlightState? source)
  {
    return source is null ? null : new VehicleFlightStateTelemetry
    {
      IsLanded = source.IsLanded,
      Gear = source.HasGear ? (VehicleComponentDeploymentState) source.Gear : null,
      Vtol = source.HasVtol ? (VehicleComponentDeploymentState) source.Vtol : null,
      Transformation = source.HasTransformation
        ? (VehicleComponentDeploymentState) source.Transformation
        : null
    };
  }

  private static VehicleFlightSettingsTelemetry? MapFlightSettings(Game.VehicleFlightSettings? source)
  {
    return source is null ? null : new VehicleFlightSettingsTelemetry
    {
      IfcsCoupled = MapAvailability(source.IfcsCoupled),
      IfcsEsp = MapAvailability(source.IfcsEsp),
      IfcsGravityCompensation = MapAvailability(source.IfcsGravityCompensation),
      IfcsGSafe = MapAvailability(source.IfcsGSafe),
      IfcsCruiseMode = MapAvailability(source.IfcsCruiseMode),
      IfcsSpacebreakEnablesBoost = source.IfcsSpacebreakEnablesBoost,
      IfcsSpeedLimiter = MapAvailability(source.IfcsSpeedLimiter),
      IfcsProximityAssist = MapAvailability(source.IfcsProximityAssist),
      IfcsProximityAssistLanding = MapAvailability(source.IfcsProximityAssistLanding),
      IfcsStability = MapAvailability(source.IfcsStability),
      IfcsCore = MapAvailability(source.IfcsCore),
      AdvancedHudEnabledScm = source.AdvancedHudEnabledScm,
      AdvancedHudEnabledNav = source.AdvancedHudEnabledNav,
      AdvancedHudLabels = source.AdvancedHudLabels,
      CoursePrediction = source.CoursePrediction,
      IfcsWindCompensation = MapAvailability(source.IfcsWindCompensation),
      IfcsGSafeDisabledByBoost = source.IfcsGSafeDisabledByBoost,
      IfcsCommandBehaviour = MapAvailability(source.IfcsCommandBehaviour),
      FlightLightAmplification = source.FlightLightAmplification
    };
  }

  private static VehicleFlightInstrumentsTelemetry? MapInstruments(Game.VehicleFlightInstruments? source)
  {
    return source is null ? null : new VehicleFlightInstrumentsTelemetry
    {
      Speed = source.Speed,
      HydrogenFuel = source.HydrogenFuel,
      QuantumFuel = source.QuantumFuel,
      GMeter = MapGauge(source.GMeter),
      Boost = MapBoost(source.Boost),
      Altitude = NormalizeAltitude(source.Altitude),
      RadarAltitude = NormalizeAltitude(source.RadarAltitude),
      VerticalSpeedIndicator = source.VerticalSpeedIndicator,
      AtmosphericDensity = source.AtmosphericDensity,
      Heading = source.Heading
    };
  }

  private static float? NormalizeAltitude(float altitude)
  {
    // Star Citizen uses values close to float.MaxValue when altitude is unavailable.
    return !float.IsFinite(altitude) || altitude >= float.MaxValue * 0.99f
      ? null
      : altitude;
  }

  private static VehicleThrusterBoostTelemetry? MapBoost(Game.VehicleThrusterBoost? source)
  {
    return source is null ? null : new VehicleThrusterBoostTelemetry
    {
      Engaged = source.Engaged,
      Output = source.Output,
      Reserves = source.Reserves,
      Critical = source.Critical
    };
  }

  private static VehiclePowerTelemetry? MapPower(Game.VehiclePowerState? source)
  {
    return source is null ? null : new VehiclePowerTelemetry
    {
      Powerplants = source.Powerplants,
      Engines = source.Engines,
      Weapons = source.Weapons,
      Coolers = source.Coolers,
      LifeSupport = source.LifeSupport,
      GravityGenerators = source.GravityGenerators,
      Sensors = source.Sensors,
      ShieldGenerators = source.ShieldGenerators,
      ShieldEmitters = source.ShieldEmitters
    };
  }

  private static VehicleShieldsTelemetry? MapShields(Game.VehicleShields? source)
  {
    return source is null ? null : new VehicleShieldsTelemetry
    {
      Level = source.Level,
      Faces = [.. source.Faces]
    };
  }

  private static VehicleMasterModeTelemetry? MapMasterMode(Game.VehicleMasterModeState? source)
  {
    return source is null ? null : new VehicleMasterModeTelemetry
    {
      Mode = (VehicleMasterMode) source.Mode,
      Spooled = source.Spooled
    };
  }

  private static ActorVitalsTelemetry? MapVitals(Game.ActorVitals? source)
  {
    return source is null ? null : new ActorVitalsTelemetry
    {
      Health = MapGauge(source.Health),
      Oxygen = MapGauge(source.Oxygen),
      Hunger = MapGauge(source.Hunger),
      Thirst = MapGauge(source.Thirst),
      BodyTemperature = MapGauge(source.BodyTemperature),
      HeartRate = source.HeartRate,
      Gravity = source.Gravity,
      Pressure = source.Pressure,
      IntoxicationLevel = source.IntoxicationLevel
    };
  }

  private static ActorStateTelemetry? MapActorState(Game.ActorState? source)
  {
    return source is null ? null : new ActorStateTelemetry
    {
      Locomotion = (ActorLocomotionState) source.Locomotion,
      CrewRole = (ActorCrewRole) source.CrewRole,
      SeatName = source.SeatName,
      UecBalance = source.UecBalance
    };
  }

  private static ActorEnvironmentTelemetry? MapEnvironment(Game.ActorEnvironment? source)
  {
    return source is null ? null : new ActorEnvironmentTelemetry
    {
      ExternalTemperature = source.ExternalTemperature,
      AmbientRadiationRate = source.AmbientRadiationRate,
      BreathableAtmosphere = source.BreathableAtmosphere
    };
  }

  private static WeaponsSettingsTelemetry? MapWeaponsSettings(Game.WeaponsSettings? source)
  {
    return source is null ? null : new WeaponsSettingsTelemetry
    {
      FallbackConvergenceDistance = source.FallbackConvergenceDistance,
      MagnifiedUi = source.MagnifiedUi,
      PrecisionLines = source.PrecisionLines,
      StaggeredFire = source.StaggeredFire,
      UseLagPips = source.UseLagPips,
      UseSinglePips = source.UseSinglePips,
      UseFadingPips = source.UseFadingPips,
      GimbalsLocked = source.GimbalsLocked
    };
  }
}