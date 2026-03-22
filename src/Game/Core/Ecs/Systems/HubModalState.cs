using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal static class HubModalState
{
    public static bool HasBlockingModalOpen(EcsWorld world)
    {
        return IsHubMenuOpen(world)
            || IsSettingsOpen(world)
            || IsPerkTreeOpen(world)
            || IsInventoryOpen(world)
            || IsStageSelectionOpen(world)
            || IsSkillSelectionOpen(world)
            || IsRunHistoryOpen(world);
    }

    public static bool IsHubMenuOpen(EcsWorld world) => AnyOpen<HubMenuState>(world, static state => state.IsOpen);

    public static bool IsSettingsOpen(EcsWorld world) => AnyOpen<SettingsMenuState>(world, static state => state.IsOpen);

    private static bool IsPerkTreeOpen(EcsWorld world) => AnyOpen<PerkTreeUI>(world, static state => state.IsOpen);

    private static bool IsInventoryOpen(EcsWorld world) => AnyOpen<InventoryUiState>(world, static state => state.IsOpen);

    private static bool IsStageSelectionOpen(EcsWorld world) => AnyOpen<StageSelectionUIState>(world, static state => state.IsOpen);

    private static bool IsSkillSelectionOpen(EcsWorld world) => AnyOpen<SkillSelectionUIState>(world, static state => state.IsOpen);

    private static bool IsRunHistoryOpen(EcsWorld world) => AnyOpen<RunHistoryUIState>(world, static state => state.IsOpen);

    private static bool AnyOpen<T>(EcsWorld world, Func<T, bool> isOpen)
        where T : struct
    {
        var anyOpen = false;
        world.ForEach<T>((Entity _, ref T state) =>
        {
            anyOpen |= isOpen(state);
        });

        return anyOpen;
    }
}
