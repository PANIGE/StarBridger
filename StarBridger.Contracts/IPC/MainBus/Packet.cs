using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using StarBridger.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.Contracts.IPC.MainBus;

public class Packet
{
  [JsonProperty("type")]
  [JsonConverter(typeof(StringEnumConverter))]
  public Intents Type { get; init; }

  [JsonProperty("payload")]
  public object? Payload { get; init; }
}