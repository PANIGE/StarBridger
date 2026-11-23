
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using StarBridger.Core.Services;
using StarBridger.Host;
using StarBridger.Host.Utils;
using StarBridger.IPC.Services;

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
  }
}