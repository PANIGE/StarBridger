using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.Contracts.Core.Shell.Definitions;

public class ArgumentDefinition
{
  [JsonProperty("name")]
  public string Name { get; init; } = string.Empty;

  [JsonProperty("description")]
  public string Description { get; init; } = string.Empty;

  [JsonProperty("required")]
  public bool IsRequired { get; init; } = false;
}