using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace StarBridger.Contracts.Telemetry.Game.v1;

[JsonConverter(typeof(StringEnumConverter))]
public enum VehicleType
{
  [EnumMember(Value = "VEHICLE_TYPE_UNSPECIFIED")]
  Unspecified = 0,

  [EnumMember(Value = "VEHICLE_TYPE_SPACESHIP")]
  Spaceship = 1,

  [EnumMember(Value = "VEHICLE_TYPE_GROUND")]
  Ground = 2,

  [EnumMember(Value = "VEHICLE_TYPE_GRAVLEV")]
  Gravlev = 3,

  [EnumMember(Value = "VEHICLE_TYPE_BOAT")]
  Boat = 4
}

[JsonConverter(typeof(StringEnumConverter))]
public enum VehicleComponentDeploymentState
{
  [EnumMember(Value = "VEHICLE_COMPONENT_DEPLOYMENT_STATE_UNSPECIFIED")]
  Unspecified = 0,

  [EnumMember(Value = "VEHICLE_COMPONENT_DEPLOYMENT_STATE_RETRACTED")]
  Retracted = 1,

  [EnumMember(Value = "VEHICLE_COMPONENT_DEPLOYMENT_STATE_DEPLOYING")]
  Deploying = 2,

  [EnumMember(Value = "VEHICLE_COMPONENT_DEPLOYMENT_STATE_DEPLOYED")]
  Deployed = 3,

  [EnumMember(Value = "VEHICLE_COMPONENT_DEPLOYMENT_STATE_RETRACTING")]
  Retracting = 4
}

[JsonConverter(typeof(StringEnumConverter))]
public enum VehicleMasterMode
{
  [EnumMember(Value = "VEHICLE_MASTER_MODE_UNSPECIFIED")]
  Unspecified = 0,

  [EnumMember(Value = "VEHICLE_MASTER_MODE_NAVIGATION")]
  Navigation = 1,

  [EnumMember(Value = "VEHICLE_MASTER_MODE_SCM")]
  Scm = 2,

  [EnumMember(Value = "VEHICLE_MASTER_MODE_STEALTH")]
  Stealth = 3
}

[JsonConverter(typeof(StringEnumConverter))]
public enum VehicleDoorOpenState
{
  [EnumMember(Value = "DOOR_OPEN_STATE_UNSPECIFIED")]
  Unspecified = 0,

  [EnumMember(Value = "DOOR_OPEN_STATE_OPENED")]
  Opened = 1,

  [EnumMember(Value = "DOOR_OPEN_STATE_CLOSED")]
  Closed = 2
}

[JsonConverter(typeof(StringEnumConverter))]
public enum VehicleDoorLockState
{
  [EnumMember(Value = "DOOR_LOCK_STATE_UNSPECIFIED")]
  Unspecified = 0,

  [EnumMember(Value = "DOOR_LOCK_STATE_UNLOCKED")]
  Unlocked = 1,

  [EnumMember(Value = "DOOR_LOCK_STATE_LOCKED")]
  Locked = 2
}

[JsonConverter(typeof(StringEnumConverter))]
public enum VehicleAnnunciatorState
{
  [EnumMember(Value = "ANNUNCIATOR_STATE_UNSPECIFIED")]
  Unspecified = 0,

  [EnumMember(Value = "ANNUNCIATOR_STATE_NOMINAL")]
  Nominal = 1,

  [EnumMember(Value = "ANNUNCIATOR_STATE_OFF")]
  Off = 2,

  [EnumMember(Value = "ANNUNCIATOR_STATE_MINOR_FLASHING")]
  MinorFlashing = 3,

  [EnumMember(Value = "ANNUNCIATOR_STATE_MINOR_ON")]
  MinorOn = 4,

  [EnumMember(Value = "ANNUNCIATOR_STATE_MAJOR_FLASHING")]
  MajorFlashing = 5,

  [EnumMember(Value = "ANNUNCIATOR_STATE_MAJOR_ON")]
  MajorOn = 6
}

[JsonConverter(typeof(StringEnumConverter))]
public enum ActorLocomotionState
{
  [EnumMember(Value = "ACTOR_LOCOMOTION_STATE_UNSPECIFIED")]
  Unspecified = 0,

  [EnumMember(Value = "ACTOR_LOCOMOTION_STATE_ON_FOOT")]
  OnFoot = 1,

  [EnumMember(Value = "ACTOR_LOCOMOTION_STATE_EVA")]
  Eva = 2,

  [EnumMember(Value = "ACTOR_LOCOMOTION_STATE_SEATED")]
  Seated = 3
}

[JsonConverter(typeof(StringEnumConverter))]
public enum ActorCrewRole
{
  [EnumMember(Value = "ACTOR_CREW_ROLE_UNSPECIFIED")]
  Unspecified = 0,

  [EnumMember(Value = "ACTOR_CREW_ROLE_PILOT")]
  Pilot = 1,

  [EnumMember(Value = "ACTOR_CREW_ROLE_GUNNER")]
  Gunner = 2,

  [EnumMember(Value = "ACTOR_CREW_ROLE_CREW")]
  Crew = 3
}

[JsonConverter(typeof(StringEnumConverter))]
public enum CountermeasureType
{
  [EnumMember(Value = "COUNTERMEASURE_TYPE_UNSPECIFIED")]
  Unspecified = 0,

  [EnumMember(Value = "COUNTERMEASURE_TYPE_DECOY")]
  Decoy = 1,

  [EnumMember(Value = "COUNTERMEASURE_TYPE_NOISE")]
  Noise = 2
}