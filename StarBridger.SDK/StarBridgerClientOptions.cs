
using StarBridger.Contracts.Enums;

namespace StarBridger.SDK;

public sealed record StarBridgerClientOptions(Uri? Endpoint = null)
{
  public ConnectionMode Mode { get; init; } = ConnectionMode.Local;
  public Intents Intents { get; init; } = Intents.All;
}
