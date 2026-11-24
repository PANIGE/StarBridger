using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.Host.Utils;

internal static class Extensions
{
  internal static void AddSingletonHostedService<TService>(this IServiceCollection services) where TService : class, IHostedService
  {
    services.AddSingleton<TService>();
    services.AddHostedService(provider => provider.GetRequiredService<TService>());
  }
}