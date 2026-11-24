using Newtonsoft.Json;
using StarBridger.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.Contracts.IPC.Shell;

public class DataShellResponse : ShellResponse
{
  public override ShellResponseType Type => ShellResponseType.Data;

  [JsonProperty("payload")]
  public object? Payload { get; init; }
}
