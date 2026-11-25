using StarBridger.Contracts.Core.Shell;
using StarBridger.Core.Utils;

namespace StarBridger.Tests;

public sealed class CoreExtensionsTests
{
  [Fact]
  public void TryGetValueFindsArgumentsWithoutCaseSensitivityAndConvertsValues()
  {
    Argument[] arguments = [new Argument("Limit", "17")];

    Assert.True(arguments.TryGetValue<int>("limit", out int limit));
    Assert.Equal(17, limit);
    Assert.False(arguments.TryGetValue<int>("missing", out int missing));
    Assert.Equal(0, missing);
  }

  [Fact]
  public void HasRawFlagChecksForArgumentPresence()
  {
    Assert.True(new[] { new Argument("RAW", string.Empty) }.HasRawFlag());
    Assert.False(Array.Empty<Argument>().HasRawFlag());
  }

  [Fact]
  public void TryGetValueSurfacesInvalidConversions()
  {
    Argument[] arguments = [new Argument("count", "not-a-number")];

    Assert.Throws<FormatException>(() => arguments.TryGetValue<int>("count", out _));
  }
}
