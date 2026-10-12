namespace Desertod.Domain.Stats;

public sealed class StatBlock
{
    public int Strength { get; set; }
    public int Dexterity { get; set; }
    public int Intelligence { get; set; }
    public int Charisma { get; set; }
    public int Endurance { get; set; }

    public double StrengthExperience { get; set; }
    public double DexterityExperience { get; set; }
    public double IntelligenceExperience { get; set; }
    public double CharismaExperience { get; set; }
    public double EnduranceExperience { get; set; }

    public int Total => Strength + Dexterity + Intelligence + Charisma + Endurance;

    public int Get(Stat stat) => stat switch
    {
        Stat.Strength => Strength,
        Stat.Dexterity => Dexterity,
        Stat.Intelligence => Intelligence,
        Stat.Charisma => Charisma,
        Stat.Endurance => Endurance,
        _ => throw new ArgumentOutOfRangeException(nameof(stat)),
    };

    public void Set(Stat stat, int value)
    {
        switch (stat)
        {
            case Stat.Strength: Strength = value; break;
            case Stat.Dexterity: Dexterity = value; break;
            case Stat.Intelligence: Intelligence = value; break;
            case Stat.Charisma: Charisma = value; break;
            case Stat.Endurance: Endurance = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(stat));
        }
    }

    public double GetExperience(Stat stat) => stat switch
    {
        Stat.Strength => StrengthExperience,
        Stat.Dexterity => DexterityExperience,
        Stat.Intelligence => IntelligenceExperience,
        Stat.Charisma => CharismaExperience,
        Stat.Endurance => EnduranceExperience,
        _ => throw new ArgumentOutOfRangeException(nameof(stat)),
    };

    public void SetExperience(Stat stat, double value)
    {
        switch (stat)
        {
            case Stat.Strength: StrengthExperience = value; break;
            case Stat.Dexterity: DexterityExperience = value; break;
            case Stat.Intelligence: IntelligenceExperience = value; break;
            case Stat.Charisma: CharismaExperience = value; break;
            case Stat.Endurance: EnduranceExperience = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(stat));
        }
    }
}
