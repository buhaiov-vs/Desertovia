using Desertod.Domain.Entities;

namespace Desertod.Domain.Tests;

public class VillageAccessTests
{
    private static readonly DateTimeOffset Now = TestWorld.Now;

    private static Village New(bool isPrivate, string name = "Sand Town", string? description = null) =>
        Village.Create("v", name, description, isPrivate, Guid.NewGuid(), Now);

    [Fact]
    public void PublicVillage_AdmitsEveryone_AndHasNoCode()
    {
        var village = New(isPrivate: false);

        Assert.Null(village.InviteCode);
        Assert.True(village.Admits(null));
        Assert.True(village.Admits("anything"));
    }

    [Fact]
    public void PrivateVillage_NeedsItsInviteCode()
    {
        var village = New(isPrivate: true);

        Assert.NotNull(village.InviteCode);
        Assert.Equal(Village.InviteCodeLength, village.InviteCode!.Length);
        Assert.False(village.Admits(null));
        Assert.False(village.Admits(""));
        Assert.False(village.Admits("WRONG123"));
        Assert.True(village.Admits(village.InviteCode));
    }

    [Fact]
    public void InviteCode_IgnoresCaseAndSpaces()
    {
        var village = New(isPrivate: true);

        Assert.True(village.Admits($"  {village.InviteCode!.ToLowerInvariant()} "));
    }

    [Fact]
    public void NewInviteCode_RevokesTheOldOne()
    {
        var village = New(isPrivate: true);
        var old = village.InviteCode!;

        var fresh = village.NewInviteCode();

        Assert.NotEqual(old, fresh);
        Assert.False(village.Admits(old));
        Assert.True(village.Admits(fresh));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("   ab   ")]
    public void Name_MustBeLongEnough(string name)
    {
        var exception = Assert.Throws<GameRuleException>(() => New(isPrivate: false, name: name));
        Assert.Equal("village.nameLength", exception.Code);
    }

    [Fact]
    public void Description_IsTrimmed_AndBlankBecomesNull()
    {
        Assert.Null(New(isPrivate: false, description: "   ").Description);
        Assert.Equal("Come build with us", New(isPrivate: false, description: "  Come build with us ").Description);
    }

    [Fact]
    public void Description_HasALimit()
    {
        var exception = Assert.Throws<GameRuleException>(
            () => New(isPrivate: false, description: new string('x', Village.DescriptionMaximumLength + 1)));
        Assert.Equal("village.descriptionLength", exception.Code);
    }
}
