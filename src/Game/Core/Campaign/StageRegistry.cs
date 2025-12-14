using Microsoft.Xna.Framework;

namespace TheLastMageStanding.Game.Core.Campaign;

/// <summary>
/// Registry of all campaign acts and stages.
/// </summary>
public sealed class StageRegistry
{
    private readonly Dictionary<string, StageDefinition> _stages = new();
    private readonly List<ActDefinition> _acts = new();
    
    public StageRegistry()
    {
        RegisterDefaultCampaign();
    }
    
    public IReadOnlyList<ActDefinition> GetAllActs() => _acts;

    public ActDefinition? GetAct(int actNumber) => _acts.FirstOrDefault(a => a.ActNumber == actNumber);

    public BossDefinition? GetBoss(string bossId) =>
        _acts.Select(a => a.Boss).FirstOrDefault(b => b != null && b.BossId == bossId);

    private void RegisterDefaultCampaign()
    {
        const string firstMap = "Tiles/Maps/FirstMap";

        var act1Biome = new BiomeDefinition(
            BiomeId: "act1_fallen_academy",
            DisplayName: "The Fallen Academy",
            PrimaryColor: new Color(160, 160, 175),
            SecondaryColor: new Color(120, 70, 160),
            EnemyArchetypeIds: new[] { "bone_hexer", "bone_scout", "bone_mage", "elite_hexer" });

        var act2Biome = new BiomeDefinition(
            BiomeId: "act2_ashen_mire",
            DisplayName: "The Ashen Mire",
            PrimaryColor: new Color(85, 110, 90),
            SecondaryColor: new Color(200, 120, 60),
            EnemyArchetypeIds: new[] { "bone_hexer", "charger_hexer", "protector_hexer", "bone_mage", "elite_hexer" });

        var act3Biome = new BiomeDefinition(
            BiomeId: "act3_frostbound_ruins",
            DisplayName: "The Frostbound Ruins",
            PrimaryColor: new Color(160, 210, 235),
            SecondaryColor: new Color(60, 120, 210),
            EnemyArchetypeIds: new[] { "bone_scout", "bone_mage", "buffer_hexer", "elite_hexer" });

        var act4Biome = new BiomeDefinition(
            BiomeId: "act4_void_sanctum",
            DisplayName: "The Void Sanctum",
            PrimaryColor: new Color(35, 25, 50),
            SecondaryColor: new Color(200, 60, 220),
            EnemyArchetypeIds: new[] { "bone_hexer", "charger_hexer", "buffer_hexer", "protector_hexer", "elite_hexer" });

        var act1Boss = new BossDefinition(
            BossId: "act1_boss_corrupted_headmaster",
            DisplayName: "Corrupted Headmaster",
            Description: "A fallen archmage bound to the academy's lingering curse.",
            BossArchetypeId: "boss_act1_headmaster",
            Phases: new[]
            {
                new BossPhaseDefinition(
                    Name: "Phase I - Caster",
                    StartsBelowHealthFraction: 1.0f,
                    MoveSpeedMultiplier: 1.0f,
                    AttackCooldownMultiplier: 1.0f,
                    ProjectileDamageMultiplier: 1.0f,
                    SummonOnEnterCount: 0,
                    SummonArchetypeId: null),
                new BossPhaseDefinition(
                    Name: "Phase II - Wrath",
                    StartsBelowHealthFraction: 0.5f,
                    MoveSpeedMultiplier: 1.15f,
                    AttackCooldownMultiplier: 0.85f,
                    ProjectileDamageMultiplier: 1.15f,
                    SummonOnEnterCount: 3,
                    SummonArchetypeId: "bone_hexer"),
                new BossPhaseDefinition(
                    Name: "Phase III - Enrage",
                    StartsBelowHealthFraction: 0.25f,
                    MoveSpeedMultiplier: 1.25f,
                    AttackCooldownMultiplier: 0.7f,
                    ProjectileDamageMultiplier: 1.35f,
                    SummonOnEnterCount: 5,
                    SummonArchetypeId: "bone_scout"),
            });

        var act2Boss = new BossDefinition(
            BossId: "act2_boss_bog_lich",
            DisplayName: "Bog Lich",
            Description: "A lich that festers in ash and brine. Placeholder boss mechanics.",
            BossArchetypeId: "boss_act2_bog_lich",
            Phases: new[]
            {
                new BossPhaseDefinition("Phase I", 1.0f, 1.0f, 1.0f, 1.0f, 0, null),
                new BossPhaseDefinition("Phase II", 0.5f, 1.15f, 0.85f, 1.1f, 2, "bone_hexer"),
                new BossPhaseDefinition("Phase III", 0.25f, 1.25f, 0.75f, 1.25f, 4, "charger_hexer"),
            });

        var act3Boss = new BossDefinition(
            BossId: "act3_boss_frost_sentinel",
            DisplayName: "Frost Sentinel",
            Description: "An ancient construct guarding the ruins. Placeholder boss mechanics.",
            BossArchetypeId: "boss_act3_frost_sentinel",
            Phases: new[]
            {
                new BossPhaseDefinition("Phase I", 1.0f, 1.0f, 1.0f, 1.0f, 0, null),
                new BossPhaseDefinition("Phase II", 0.5f, 1.1f, 0.9f, 1.15f, 3, "bone_mage"),
                new BossPhaseDefinition("Phase III", 0.25f, 1.2f, 0.75f, 1.3f, 5, "buffer_hexer"),
            });

        var act4Boss = new BossDefinition(
            BossId: "act4_boss_void_archon",
            DisplayName: "Void Archon",
            Description: "The sanctum's final warden. Placeholder boss mechanics.",
            BossArchetypeId: "boss_act4_void_archon",
            Phases: new[]
            {
                new BossPhaseDefinition("Phase I", 1.0f, 1.0f, 1.0f, 1.0f, 0, null),
                new BossPhaseDefinition("Phase II", 0.5f, 1.15f, 0.85f, 1.25f, 4, "protector_hexer"),
                new BossPhaseDefinition("Phase III", 0.25f, 1.3f, 0.7f, 1.45f, 6, "elite_hexer"),
            });

        var act1Stages = new[]
        {
            new StageDefinition(
                stageId: "act1_stage1",
                displayName: "Courtyard",
                actNumber: 1,
                stageNumber: 1,
                description: "Reclaim the academy's shattered courtyard. (Tutorial)",
                requiredMetaLevel: 1,
                requiredPreviousStageId: null,
                mapAssetPath: firstMap,
                biomeId: act1Biome.BiomeId,
                maxWaves: 5,
                isBossStage: false,
                bossId: null,
                rewards: new StageRewardDefinition(CompletionGold: 75, CompletionMetaXpBonus: 150, GuaranteedLoot: false)),
            new StageDefinition(
                stageId: "act1_stage2",
                displayName: "Library",
                actNumber: 1,
                stageNumber: 2,
                description: "Purge the cursed stacks and survive the growing tide.",
                requiredMetaLevel: 3,
                requiredPreviousStageId: "act1_stage1",
                mapAssetPath: firstMap,
                biomeId: act1Biome.BiomeId,
                maxWaves: 8,
                isBossStage: false,
                bossId: null,
                rewards: new StageRewardDefinition(CompletionGold: 120, CompletionMetaXpBonus: 250, GuaranteedLoot: false)),
            new StageDefinition(
                stageId: "act1_stage3",
                displayName: "Headmaster's Hall",
                actNumber: 1,
                stageNumber: 3,
                description: "Confront the Corrupted Headmaster in the heart of the ruin.",
                requiredMetaLevel: 5,
                requiredPreviousStageId: "act1_stage2",
                mapAssetPath: firstMap,
                biomeId: act1Biome.BiomeId,
                maxWaves: 10,
                isBossStage: true,
                bossId: act1Boss.BossId,
                rewards: new StageRewardDefinition(CompletionGold: 350, CompletionMetaXpBonus: 800, GuaranteedLoot: true)),
        };

        var act2Stages = new[]
        {
            new StageDefinition(
                "act2_stage1", "Sootfen Approach", 2, 1,
                "Enter the mire. Placeholder stage.", 10, "act1_stage3", firstMap,
                act2Biome.BiomeId, 6, false, null,
                new StageRewardDefinition(120, 250, false)),
            new StageDefinition(
                "act2_stage2", "Ashwater Crossing", 2, 2,
                "Press deeper into the bog. Placeholder stage.", 12, "act2_stage1", firstMap,
                act2Biome.BiomeId, 8, false, null,
                new StageRewardDefinition(160, 300, false)),
            new StageDefinition(
                "act2_stage3", "Lich's Sinkhole", 2, 3,
                "Boss encounter: Bog Lich (placeholder).", 14, "act2_stage2", firstMap,
                act2Biome.BiomeId, 10, true, act2Boss.BossId,
                new StageRewardDefinition(450, 900, true)),
        };

        var act3Stages = new[]
        {
            new StageDefinition(
                "act3_stage1", "Glacier Courtyards", 3, 1,
                "Frozen ruins and brittle magic. Placeholder stage.", 25, "act2_stage3", firstMap,
                act3Biome.BiomeId, 7, false, null,
                new StageRewardDefinition(200, 400, false)),
            new StageDefinition(
                "act3_stage2", "Shattered Nave", 3, 2,
                "Elites emerge from the ice. Placeholder stage.", 27, "act3_stage1", firstMap,
                act3Biome.BiomeId, 9, false, null,
                new StageRewardDefinition(240, 500, false)),
            new StageDefinition(
                "act3_stage3", "Sentinel's Vault", 3, 3,
                "Boss encounter: Frost Sentinel (placeholder).", 30, "act3_stage2", firstMap,
                act3Biome.BiomeId, 10, true, act3Boss.BossId,
                new StageRewardDefinition(550, 1100, true)),
        };

        var act4Stages = new[]
        {
            new StageDefinition(
                "act4_stage1", "Umbral Antechamber", 4, 1,
                "The sanctum consumes light. Placeholder stage.", 40, "act3_stage3", firstMap,
                act4Biome.BiomeId, 8, false, null,
                new StageRewardDefinition(260, 550, false)),
            new StageDefinition(
                "act4_stage2", "Void Reliquary", 4, 2,
                "Reality frays. Placeholder stage.", 45, "act4_stage1", firstMap,
                act4Biome.BiomeId, 10, false, null,
                new StageRewardDefinition(320, 650, false)),
            new StageDefinition(
                "act4_stage3", "Archon's Throne", 4, 3,
                "Final boss: Void Archon (placeholder).", 50, "act4_stage2", firstMap,
                act4Biome.BiomeId, 10, true, act4Boss.BossId,
                new StageRewardDefinition(700, 1400, true)),
        };

        RegisterAct(new ActDefinition(1, act1Biome.DisplayName, act1Biome, act1Stages, act1Boss));
        RegisterAct(new ActDefinition(2, act2Biome.DisplayName, act2Biome, act2Stages, act2Boss));
        RegisterAct(new ActDefinition(3, act3Biome.DisplayName, act3Biome, act3Stages, act3Boss));
        RegisterAct(new ActDefinition(4, act4Biome.DisplayName, act4Biome, act4Stages, act4Boss));
    }

    private void RegisterAct(ActDefinition act)
    {
        _acts.Add(act);
        foreach (var stage in act.Stages)
        {
            Register(stage);
        }
    }
    
    public void Register(StageDefinition stage)
    {
        _stages[stage.StageId] = stage;
    }
    
    public StageDefinition? GetStage(string stageId)
    {
        return _stages.TryGetValue(stageId, out var stage) ? stage : null;
    }
    
    public IReadOnlyList<StageDefinition> GetAllStages()
    {
        return _stages.Values.OrderBy(s => s.ActNumber).ThenBy(s => s.StageNumber).ToList();
    }
    
    public IReadOnlyList<StageDefinition> GetStagesForAct(int actNumber)
    {
        return _stages.Values
            .Where(s => s.ActNumber == actNumber)
            .OrderBy(s => s.StageNumber)
            .ToList();
    }
}
