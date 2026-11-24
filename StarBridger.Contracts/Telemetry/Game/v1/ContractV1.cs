using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.Contracts.Telemetry.Game.v1;

public abstract record ContractV1 : VersionnableContract
{
  public override Version CurrentVersion { get; } = new Version(1, 0, 0);
}
