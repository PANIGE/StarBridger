using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.Contracts.Core.IO;

public static class Folders
{
  private static string GetDataDirectory()
  {
    if (OperatingSystem.IsWindows())
      return
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StarBridger");

    if (OperatingSystem.IsLinux())
    {
      string? state = Environment.GetEnvironmentVariable("XDG_STATE_HOME");

      if (string.IsNullOrWhiteSpace(state) || !Path.IsPathFullyQualified(state))
        state = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "state");

      return Path.Combine(state, "starbridger");
    }

    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StarBridger");
  }

  public static string DataDirectory { get; } = GetDataDirectory();
}