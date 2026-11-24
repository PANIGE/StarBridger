using Microsoft.Extensions.Hosting;
using Serilog;
using StarBridger.Contracts.Core.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.Host.Utils;

internal static class LogUtils
{
  internal static string GetLogsPath()
  {
    return Path.Combine(Folders.DataDirectory, "Logs");
  }

  internal static void ConfigureLogging()
  {
    string logsPath = GetLogsPath();
    Directory.CreateDirectory(logsPath);

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .WriteTo.File(
            Path.Combine(logsPath, "starbridger-.log"),
            rollingInterval: RollingInterval.Day,
            rollOnFileSizeLimit: true,
            fileSizeLimitBytes: 10 * 1024 * 1024,
            retainedFileCountLimit: 14,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
        .CreateLogger();
  }
}