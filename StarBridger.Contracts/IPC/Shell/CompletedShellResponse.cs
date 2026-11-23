using Newtonsoft.Json;
using StarBridger.Contracts.Enums;
using StarBridger.Contracts.JsonConverters;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.Contracts.IPC.Shell;

public class CompletedShellResponse : ShellResponse
{
  public override ShellResponseType Type => ShellResponseType.Completed;

  [JsonProperty("success")]
  public required bool Success { get; init; }

  [JsonProperty("error")]
  public string ErrorMessage { get; init; } = string.Empty;
}
