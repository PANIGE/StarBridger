using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StarBridger.Contracts.Core.Shell.Definitions;
using StarBridger.Contracts.Enums;
using StarBridger.Contracts.IPC.Shell;
using StarBridger.Contracts.Telemetry.Game.v1;

namespace StarBridger.Tests;

public sealed class ContractsTests
{
  [Fact]
  public void CommandDefinitionBuildsNormalizedCommandText()
  {
    CommandDefinition definition = new()
    {
      Service = "Host",
      Name = "Status"
    };

    Assert.Equal("host.status", definition.CommandText);
  }

  [Fact]
  public void ShellResponsesRoundTripThroughJsonConverter()
  {
    ShellResponse[] responses =
    [
      new OutputShellResponse { Message = "ready" },
      new DataShellResponse { Payload = JObject.FromObject(new { value = 42 }) },
      new CompletedShellResponse { Success = false, ErrorMessage = "failed" }
    ];

    foreach (ShellResponse response in responses)
    {
      string json = JsonConvert.SerializeObject(response);
      ShellResponse? result = JsonConvert.DeserializeObject<ShellResponse>(json);

      Assert.NotNull(result);
      Assert.Equal(response.Type, result.Type);

      if (response is OutputShellResponse expectedOutput)
        Assert.Equal(expectedOutput.Message, Assert.IsType<OutputShellResponse>(result).Message);

      if (response is DataShellResponse)
        Assert.Equal(42, Assert.IsType<JObject>(Assert.IsType<DataShellResponse>(result).Payload)["value"]!.Value<int>());

      if (response is CompletedShellResponse expectedCompletion)
      {
        CompletedShellResponse actual = Assert.IsType<CompletedShellResponse>(result);
        Assert.Equal(expectedCompletion.Success, actual.Success);
        Assert.Equal(expectedCompletion.ErrorMessage, actual.ErrorMessage);
      }
    }
  }

  [Fact]
  public void ContractVersionMetadataIsNotSerialized()
  {
    ActorTelemetry actor = new();

    string json = JsonConvert.SerializeObject(actor);

    Assert.DoesNotContain("CurrentVersion", json);
    Assert.DoesNotContain("PreviousVersion", json);
  }

  [Theory]
  [InlineData("{}")]
  [InlineData("{\"type\":\"unknown\"}")]
  [InlineData("{\"type\":\"output\"}")]
  [InlineData("{\"type\":\"completed\"}")]
  [InlineData("{\"type\":\"data\"}")]
  public void InvalidShellResponsesAreRejected(string json)
  {
    Assert.Throws<JsonSerializationException>(
      () => JsonConvert.DeserializeObject<ShellResponse>(json));
  }
}
