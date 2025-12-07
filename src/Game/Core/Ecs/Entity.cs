namespace TheLastMageStanding.Game.Core.Ecs;

internal readonly record struct Entity(int Id)
{
    public static readonly Entity None = new(-1);

    public bool IsValid => Id >= 0;
}

