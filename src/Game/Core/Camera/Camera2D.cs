using Microsoft.Xna.Framework;

namespace TheLastMageStanding.Game.Core.Camera;

internal sealed class Camera2D
{
    private readonly float _viewportWidth;
    private readonly float _viewportHeight;

    public Matrix Transform { get; private set; } = Matrix.Identity;
    public Vector2 Position { get; private set; } = Vector2.Zero;
    public Vector2 ShakeOffset { get; set; } = Vector2.Zero;
    public float Zoom { get; set; } = 1f;
    public float Rotation { get; set; }

    public Camera2D(float viewportWidth, float viewportHeight)
    {
        _viewportWidth = viewportWidth;
        _viewportHeight = viewportHeight;
        UpdateTransform();
    }

    public void LookAt(Vector2 position)
    {
        Position = position;
        UpdateTransform();
    }

    private void UpdateTransform()
    {
        // Apply shake offset to camera position
        var effectivePosition = Position + ShakeOffset;
        
        Transform =
            Matrix.CreateTranslation(new Vector3(-effectivePosition, 0f)) *
            Matrix.CreateRotationZ(Rotation) *
            Matrix.CreateScale(Zoom, Zoom, 1f) *
            Matrix.CreateTranslation(_viewportWidth * 0.5f, _viewportHeight * 0.5f, 0f);
    }
}

