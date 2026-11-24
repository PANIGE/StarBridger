using Newtonsoft.Json;
using StarBridger.Contracts.Enums;
using StarBridger.Contracts.JsonConverters;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.Contracts.IPC.Shell;

[JsonConverter(typeof(ShellResponseTypeConverter))]
public abstract class ShellResponse
{
  [JsonProperty("type")]
  public abstract ShellResponseType Type { get; }
}