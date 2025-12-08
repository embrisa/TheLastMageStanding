using Microsoft.Xna.Framework;

namespace TheLastMageStanding.Game.Core.Ecs.Components;

/// <summary>
/// Defines visual telegraph data for attack windup.
/// </summary>
internal readonly struct TelegraphData
{
    public float Duration { get; }
    public Color Color { get; }
    public float Radius { get; }
    public Vector2 Offset { get; }
    public TelegraphShape Shape { get; }

    public TelegraphData(float duration, Color color, float radius, Vector2 offset, TelegraphShape shape = TelegraphShape.Circle)
    {
        Duration = duration;
        Color = color;
        Radius = radius;
        Offset = offset;
        Shape = shape;
    }

    public static TelegraphData Default() => new(0.2f, new Color(255, 0, 0, 128), 40f, Vector2.Zero, TelegraphShape.Circle);
}

/// <summary>
/// Shape of telegraph indicator.
/// </summary>
internal enum TelegraphShape
{
    Circle,
    Cone,
    Rectangle
}

/// <summary>
/// Component marking an entity as displaying a telegraph warning.
/// </summary>
internal struct ActiveTelegraph
{
    public float RemainingTime { get; set; }
    public TelegraphData Data { get; }

    public ActiveTelegraph(float remainingTime, TelegraphData data)
    {
        RemainingTime = remainingTime;
        Data = data;
    }
}
