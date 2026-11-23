using Newtonsoft.Json;
using StarBridger.Contracts.Enums;
using StarBridger.Contracts.JsonConverters;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.Contracts.IPC.Shell;

public class OutputShellResponse : ShellResponse
{
  public override ShellResponseType Type => ShellResponseType.Output;

  [JsonProperty("message")]
  public required string Message { get; init; }
}
