
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using StarBridger.Host.Utils;

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

  private static void ConfigureServices(IServiceCollection services)
  {
    // main executions services go there
    services.GetHashCode(); // to avoid IDE0060 TODO: remove this line when adding services
  }
}