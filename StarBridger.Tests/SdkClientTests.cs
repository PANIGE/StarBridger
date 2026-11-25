using StarBridger.Contracts.Enums;
using StarBridger.SDK;
using StarBridger.SDK.Exceptions;

namespace StarBridger.Tests;

public sealed class SdkClientTests
{
  [Fact]
  public async Task ClientCanChangeIntentsBeforeConnecting()
  {
    StarBridgerClient client = new(new StarBridgerClientOptions
    {
      Intents = Intents.Actor
    });

    Assert.False(client.IsConnected);
    Assert.Equal(Intents.Actor, client.Intents);

    await client.SetIntentsAsync(Intents.Vehicle);

    Assert.Equal(Intents.Vehicle, client.Intents);
    Assert.Throws<IntentException>(() => client.Context.Actor);
    Assert.Null(client.Context.Vehicle);
  }
}
