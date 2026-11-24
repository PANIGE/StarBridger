using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.Contracts.Enums;

[Flags]
public enum Intents : ulong
{
  None = 0,
  Identity = 1 << 0,
  Vehicle = 1 << 1,
  Actor = 1 << 2,
  Weapons = 1 << 3,
  GameConnection = 1 << 4,
  // TODO : add intents once telemetry is defined and implemented
  All = ulong.MaxValue
}