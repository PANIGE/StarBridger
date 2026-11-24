using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace StarBridger.Contracts.Telemetry.Game;

public abstract record VersionnableContract
{
  [JsonIgnore]
  public abstract Version CurrentVersion { get; }

  [JsonIgnore]
  public virtual Type? PreviousVersion => null;
}

public abstract record VersionnableContract<T> : VersionnableContract where T : VersionnableContract
{
  public override Type PreviousVersion => typeof(T);
}