using Microsoft.Xna.Framework;

namespace TheLastMageStanding.Game.Core.Ecs.Components;

/// <summary>
/// Marks an entity as interactable and specifies the action to trigger on interaction.
/// </summary>
internal struct InteractionTrigger
{
    public InteractionType Type;
    public float InteractionRadius;  // Distance within which player can interact
}

internal enum InteractionType
{
    None = 0,
    OpenTalentTree,
    OpenStageSelection,
    OpenSkillSelection,
    OpenShop,
    OpenStats,
}

/// <summary>
/// Temporary component added when player is near an interactable entity.
/// Used to display "E to interact" prompt.
/// </summary>
internal struct ProximityPrompt
{
    public Entity InteractableEntity;
    public InteractionType InteractionType;
    public Vector2 PromptPosition;  // World position to draw the prompt
}
