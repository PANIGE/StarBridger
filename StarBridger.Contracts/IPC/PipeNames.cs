using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.Contracts.IPC;

public static class PipeNames
{
  private static string GetPipeName(string serviceName)
  {
    return $"{nameof(StarBridger)}.{serviceName}".ToLowerInvariant();
  }

  public static string Shell { get; } = GetPipeName("shell");
  public static string Main { get; } = GetPipeName("main");
}