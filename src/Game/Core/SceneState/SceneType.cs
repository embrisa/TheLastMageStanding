namespace TheLastMageStanding.Game.Core.SceneState;

/// <summary>
/// Defines the types of scenes in the game.
/// </summary>
public enum SceneType
{
    /// <summary>
    /// Main menu shown on game startup.
    /// </summary>
    MainMenu,

    /// <summary>
    /// The hub scene where players configure their build (skills, talents, equipment).
    /// </summary>
    Hub,
    
    /// <summary>
    /// The stage/combat scene where players fight through waves with a locked loadout.
    /// </summary>
    Stage,
    
    /// <summary>
    /// Cutscene or narrative scene (future use).
    /// </summary>
    Cutscene
}
