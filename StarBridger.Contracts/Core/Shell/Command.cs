using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.Contracts.Core.Shell;

public class Command
{
  [JsonProperty("command")]
  public string CommandText { get; init; } = string.Empty;

  [JsonProperty("arguments")]
  public Argument[] Arguments { get; init; } = [];
}