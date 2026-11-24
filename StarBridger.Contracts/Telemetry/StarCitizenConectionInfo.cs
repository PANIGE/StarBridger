using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using StarBridger.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.Contracts.Telemetry;

public sealed record StarCitizenConnectionInfo
{
  [JsonProperty("status")]
  [JsonConverter(typeof(StringEnumConverter))]
  public StarCitizenConnectionStatus Status { get; init; }

  [JsonProperty("endpoint")]
  public string Endpoint { get; init; } = string.Empty;

  [JsonProperty("lastConnectedAtUtc")]
  public DateTimeOffset? LastConnectedAtUtc { get; init; }

  [JsonProperty("lastFrameAtUtc")]
  public DateTimeOffset? LastFrameAtUtc { get; init; }

  [JsonProperty("lastError")]
  public string? LastError { get; init; }
}