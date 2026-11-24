using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.Contracts.Host;

public class HostStatus
{
  [JsonProperty("hostPath")]
  public string HostPath { get; init; } = "";

  [JsonProperty("pid")]
  public int ProcessID { get; init; }

  [JsonProperty("upTime")]
  public TimeSpan UpTime { get; init; }

  [JsonProperty("cpu")]
  public double CpuUsage { get; init; }

  [JsonProperty("memoryWork")]
  public double MemoryWorkingSet { get; init; }

  [JsonProperty("memoryManaged")]
  public double MemoryManaged { get; init; }

  [JsonProperty("threads")]
  public int ThreadsCount { get; init; }
}
