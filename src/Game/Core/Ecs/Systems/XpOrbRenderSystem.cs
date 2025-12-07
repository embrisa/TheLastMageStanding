using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Renders XP orbs in the game world.
/// </summary>
internal sealed class XpOrbRenderSystem : IDrawSystem, ILoadContentSystem
{
    private Texture2D? _orbTexture;
    private bool _contentLoaded;

    public void Initialize(EcsWorld world)
    {
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        _orbTexture = content.Load<Texture2D>("Sprites/objects/ExperienceShard");
        _contentLoaded = true;
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        if (!_contentLoaded || _orbTexture == null)
            return;

        var spriteBatch = context.SpriteBatch;

        world.ForEach<XpOrb, Position>(
            (Entity entity, ref XpOrb orb, ref Position position) =>
            {
                var origin = new Vector2(_orbTexture.Width / 2f, _orbTexture.Height / 2f);
                
                spriteBatch.Draw(
                    _orbTexture,
                    position.Value,
                    null,
                    Color.White,
                    0f,
                    origin,
                    1f,
                    SpriteEffects.None,
                    0f);
            });
    }
}
