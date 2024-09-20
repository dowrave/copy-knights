public enum Faction
{
    Ally,
    Enemy,
    Neutral
}

public interface IFactionMember
{
    Faction Faction { get; }
}
