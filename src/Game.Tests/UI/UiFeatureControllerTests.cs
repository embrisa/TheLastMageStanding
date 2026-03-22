using TheLastMageStanding.Game.Core.Skills;
using TheLastMageStanding.Game.Core.UI;
using Xunit;

namespace TheLastMageStanding.Game.Tests.UI;

public sealed class UiFeatureControllerTests
{
    [Fact]
    public void SkillSelectionController_Update_EquipsSelectedSkillAndConfirmsPendingLoadout()
    {
        var controller = new SkillSelectionController();
        controller.Open(new SkillLoadout(
            Primary: SkillId.Firebolt,
            Hotkey1: SkillId.None,
            Hotkey2: SkillId.None,
            Hotkey3: SkillId.None,
            Hotkey4: SkillId.None));

        controller.SelectSkill(SkillId.Blizzard);
        controller.SelectSlot(2);
        controller.RequestConfirm();

        var result = controller.Update(default);

        Assert.True(result.ConfirmRequested);
        Assert.Equal(SkillId.Blizzard, controller.PendingLoadout.Hotkey2);
        Assert.True(result.ViewState.HasChanges);
    }

    [Fact]
    public void SkillSelectionController_Update_SwapsExistingSlotWhenMovingSelectedSkill()
    {
        var controller = new SkillSelectionController();
        controller.Open(new SkillLoadout(
            Primary: SkillId.Firebolt,
            Hotkey1: SkillId.Fireball,
            Hotkey2: SkillId.FrostBolt,
            Hotkey3: SkillId.None,
            Hotkey4: SkillId.None));

        controller.SelectSkill(SkillId.Fireball);
        controller.SelectSlot(2);

        var result = controller.Update(default);

        Assert.Equal(SkillId.FrostBolt, controller.PendingLoadout.Hotkey1);
        Assert.Equal(SkillId.Fireball, controller.PendingLoadout.Hotkey2);
        Assert.True(result.ViewState.HasChanges);
    }

    [Fact]
    public void StageSelectionController_Update_StartsUnlockedStageAndClosesOnBack()
    {
        var controller = new StageSelectionController();
        var catalog = CreateStageCatalog();
        controller.Open(actIndex: 0, stageIndex: 0, catalog);

        var startResult = controller.Update(catalog, new StageSelectionControllerInput(
            MoveLeft: false,
            MoveRight: false,
            MoveUp: false,
            MoveDown: false,
            Confirm: true,
            Back: false));

        Assert.Equal("act1_stage1", startResult.StartStageId);
        Assert.False(startResult.CloseRequested);

        controller.RequestBack();
        var closeResult = controller.Update(catalog, default);

        Assert.True(closeResult.CloseRequested);
    }

    [Fact]
    public void StageSelectionController_Update_ResetsSelectionOnActChangeAndBlocksLockedStageStart()
    {
        var controller = new StageSelectionController();
        var catalog = CreateStageCatalog();
        controller.Open(actIndex: 0, stageIndex: 1, catalog);

        controller.ChangeAct(1, catalog);
        var result = controller.Update(catalog, new StageSelectionControllerInput(
            MoveLeft: false,
            MoveRight: false,
            MoveUp: false,
            MoveDown: false,
            Confirm: true,
            Back: false));

        Assert.Equal(1, controller.SelectedActIndex);
        Assert.Equal(0, controller.SelectedStageIndex);
        Assert.Null(result.StartStageId);
        Assert.Equal("LOCKED - Requires Meta Level 10", result.ViewState.Details.StatusText);
    }

    private static StageSelectionCatalog CreateStageCatalog()
    {
        return new StageSelectionCatalog(
            MetaText: "Meta Level: 5",
            Acts:
            [
                new StageSelectionActOption(
                    ActNumber: 1,
                    Title: "The Fallen Academy",
                    Stages:
                    [
                        new StageSelectionStageOption(
                            StageId: "act1_stage1",
                            DisplayName: "Courtyard",
                            Description: "Tutorial stage",
                            IsUnlocked: true,
                            IsCompleted: false,
                            LockedReason: string.Empty),
                        new StageSelectionStageOption(
                            StageId: "act1_stage2",
                            DisplayName: "Library",
                            Description: "Second stage",
                            IsUnlocked: true,
                            IsCompleted: true,
                            LockedReason: string.Empty)
                    ]),
                new StageSelectionActOption(
                    ActNumber: 2,
                    Title: "The Ashen Mire",
                    Stages:
                    [
                        new StageSelectionStageOption(
                            StageId: "act2_stage1",
                            DisplayName: "Sootfen Approach",
                            Description: "Locked stage",
                            IsUnlocked: false,
                            IsCompleted: false,
                            LockedReason: "Requires Meta Level 10")
                    ])
            ]);
    }
}
