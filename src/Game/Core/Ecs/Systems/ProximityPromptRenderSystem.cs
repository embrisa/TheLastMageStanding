using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Renders "E to interact" prompts above NPCs when player is nearby.
/// </summary>
internal sealed class ProximityPromptRenderSystem : IDrawSystem, ILoadContentSystem
{
    private SpriteFont? _font;

    public void Initialize(EcsWorld world)
    {
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        _font = content.Load<SpriteFont>("Fonts/FontRegularText");
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        if (_font == null)
            return;

        var spriteBatch = context.SpriteBatch;

        // Draw prompts for all entities with ProximityPrompt component
        world.ForEach<ProximityPrompt>(
            (Entity _, ref ProximityPrompt prompt) =>
            {
                var promptText = GetPromptText(prompt.InteractionType);
                var textSize = _font.MeasureString(promptText);
                
                // Draw above the NPC position
                var screenPos = prompt.PromptPosition + new Vector2(-textSize.X / 2, -40);

                // Background
                var padding = 4;
                var bgRect = new Rectangle(
                    (int)(screenPos.X - padding),
                    (int)(screenPos.Y - padding),
                    (int)(textSize.X + padding * 2),
                    (int)(textSize.Y + padding * 2)
                );
                
                // Draw semi-transparent background (would need white pixel texture)
                // For now, just draw the text
                spriteBatch.DrawString(_font, promptText, screenPos, Color.Yellow);
            });
    }

    private static string GetPromptText(InteractionType type)
    {
        return type switch
        {
            InteractionType.OpenTalentTree => "E - Talent Tree",
            InteractionType.OpenStageSelection => "E - Stage Select",
            InteractionType.OpenSkillSelection => "E - Skills",
            InteractionType.OpenShop => "E - Shop",
            InteractionType.OpenStats => "E - Stats",
            _ => "E - Interact"
        };
    }
}
