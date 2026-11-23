using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.Contracts.Enums;

[Flags]
public enum Intents : ulong
{
  None = 0,
  Identity = 1 << 0,
  // TODO : add intents once telemetry is defined and implemented
  All = ulong.MaxValue
}
