using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace StarBridger.Contracts.Core.Shell.Definitions;

public record CommandDefinition
{
  [JsonProperty("service")]
  public string Service { get; init; } = string.Empty;

  [JsonProperty("name")]
  public string Name { get; init; } = string.Empty;

  [JsonProperty("description")]
  public string Description { get; init; } = string.Empty;

  [JsonProperty("arguments")]
  public ArgumentDefinition[] Arguments { get; init; } = Array.Empty<ArgumentDefinition>();

  [JsonIgnore]
  public string CommandText => $"{Service.ToLowerInvariant()}.{Name.ToLowerInvariant()}";
}