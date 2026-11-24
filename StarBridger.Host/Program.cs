
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using StarBridger.Contracts.Enums;
using StarBridger.Core.Services;
using StarBridger.Host;
using StarBridger.Host.Utils;
using StarBridger.IPC.Services;
using StarBridger.Telemetry;

internal class Program
{
  private static async Task Main(string[] args)
  {
    LogUtils.ConfigureLogging();

    try
    {
      HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

      builder.Services.AddSerilog();

      ConfigureServices(builder.Services);

      using IHost host = builder.Build();

      RegisterShellCommands(host);

      Microsoft.Extensions.Logging.ILogger logger = host.Services
          .GetRequiredService<ILoggerFactory>()
          .CreateLogger("StarBridger.Host");

      logger.LogInformation("StarBridger starting");

      await host.RunAsync();
    }
    catch (Exception ex)
    {
      Log.Fatal(ex, "StarBridger terminated unexpectedly");
    }
    finally
    {
      Log.CloseAndFlush();
    }
  }

  private static void RegisterShellCommands(IHost host)
  {
    IHostApplicationLifetime lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
    StarBridgerShell shell = host.Services.GetRequiredService<StarBridgerShell>();
    ShellReceiver.SetupShell(lifetime, shell);
  }

  private static void ConfigureServices(IServiceCollection services)
  {
    services.AddSingletonHostedService<StarBridgerShell>();
    services.AddSingletonHostedService<MainIpcBusService>();
    services.AddSingletonHostedService<ShellIpcService>();
    services.AddSingletonHostedService<StarCitizenTelemetryService>();
  }

  private static void ConfigureIpcConnections(IHost host)
  {
    MainIpcBusService ipc = host.Services.GetRequiredService<MainIpcBusService>();
    StarCitizenTelemetryService telemetry = host.Services.GetRequiredService<StarCitizenTelemetryService>();
    ipc.AddEventListener(telemetry.Connection, Intents.GameConnection);
    ipc.AddEventListener(telemetry.Vehicle, Intents.Vehicle);
    ipc.AddEventListener(telemetry.Actor, Intents.Actor);
    ipc.AddEventListener(telemetry.Weapons, Intents.Weapons);
  }
}