using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.Contracts.Core.Shell;

public class Argument
{
  [JsonProperty("name")]
  public string Name { get; init; } = string.Empty;

  [JsonProperty("value")]
  public string Value { get; init; } = string.Empty;
}