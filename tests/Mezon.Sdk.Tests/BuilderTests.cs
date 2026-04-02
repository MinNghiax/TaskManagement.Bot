namespace Mezon.Sdk.Tests;

using Mezon.Sdk.Builders;
using Xunit;

public class BuilderTests
{
    [Fact]
    public void ButtonBuilder_BuildsComponents()
    {
        var builder = new ButtonBuilder()
            .AddButton("btn1", "Click me", ButtonStyle.Primary)
            .AddButton("btn2", "Cancel", ButtonStyle.Danger);

        var result = builder.Build();
        Assert.Equal(2, result.Count);
        Assert.Equal("btn1", result[0].Id);
        Assert.Equal("Click me", result[0].Component.Label);
        Assert.Equal((int)ButtonStyle.Primary, result[0].Component.Style);
        Assert.Equal("btn2", result[1].Id);
    }

    [Fact]
    public void InteractiveBuilder_BuildsMessage()
    {
        var msg = new InteractiveBuilder("Hello World")
            .SetDescription("A test message")
            .SetColor("#FF5733")
            .AddField("Field 1", "Value 1", inline: true)
            .AddField("Field 2", "Value 2")
            .Build();

        Assert.Equal("Hello World", msg.Title);
        Assert.Equal("A test message", msg.Description);
        Assert.Equal("#FF5733", msg.Color);
        Assert.Equal(2, msg.Fields.Count);
        Assert.True(msg.Fields[0].Inline);
        Assert.False(msg.Fields[1].Inline);
    }

    [Fact]
    public void InteractiveBuilder_SerializesToJson()
    {
        var json = new InteractiveBuilder("Test")
            .AddField("f", "v")
            .ToJson();

        Assert.Contains("\"title\":\"Test\"", json);
        Assert.Contains("\"fields\"", json);
    }
}
