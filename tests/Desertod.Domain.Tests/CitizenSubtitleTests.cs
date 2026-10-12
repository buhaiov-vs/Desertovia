using Desertod.Domain.Entities;
using Desertod.Domain.Rules;

namespace Desertod.Domain.Tests;

public class CitizenSubtitleTests
{
    private static Citizen New() => CharacterCreation.Create(Guid.NewGuid(), Guid.NewGuid(), "Ivan", TestWorld.Now);

    [Fact]
    public void Subtitle_IsTrimmed()
    {
        var citizen = New();

        citizen.SetSubtitle("  Best hunter  ");

        Assert.Equal("Best hunter", citizen.Subtitle);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankSubtitle_RemovesIt(string? subtitle)
    {
        var citizen = New();
        citizen.SetSubtitle("Elder");

        citizen.SetSubtitle(subtitle);

        Assert.Null(citizen.Subtitle);
    }

    [Fact]
    public void Subtitle_HasALimit()
    {
        var citizen = New();

        citizen.SetSubtitle(new string('x', Citizen.SubtitleMaximumLength));
        var exception = Assert.Throws<GameRuleException>(() => citizen.SetSubtitle(new string('x', Citizen.SubtitleMaximumLength + 1)));

        Assert.Equal("citizen.subtitleLength", exception.Code);
        Assert.Equal(Citizen.SubtitleMaximumLength, citizen.Subtitle?.Length);
    }
}
