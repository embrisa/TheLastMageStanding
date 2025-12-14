namespace TheLastMageStanding.Game.Core.Ecs.Components;

internal struct StageRunState
{
    public string StageId { get; set; }
    public int MaxWaves { get; set; }
    public bool IsBossStage { get; set; }
    public int BossWaveIndex { get; set; }
    public string? BossId { get; set; }
    public string? BossArchetypeId { get; set; }
    public bool BossSpawned { get; set; }
    public Entity BossEntity { get; set; }
    public string BiomeId { get; set; }
    public int CompletionGold { get; set; }
    public int CompletionMetaXpBonus { get; set; }

    public StageRunState()
    {
        StageId = string.Empty;
        MaxWaves = 0;
        IsBossStage = false;
        BossWaveIndex = 0;
        BossId = null;
        BossArchetypeId = null;
        BossSpawned = false;
        BossEntity = Entity.None;
        BiomeId = string.Empty;
        CompletionGold = 0;
        CompletionMetaXpBonus = 0;
    }
}

internal struct BossEncounter
{
    public string BossId { get; set; }

    public BossEncounter(string bossId)
    {
        BossId = bossId;
    }
}

internal struct BossPhaseState
{
    public int PhaseIndex { get; set; }

    public BossPhaseState(int phaseIndex)
    {
        PhaseIndex = phaseIndex;
    }
}

internal struct BossBaseStats
{
    public float BaseMoveSpeed { get; set; }
    public float BaseAttackCooldownSeconds { get; set; }
    public float BaseProjectileDamage { get; set; }
    public float BaseProjectileWindupSeconds { get; set; }
}

