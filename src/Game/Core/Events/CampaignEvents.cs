namespace TheLastMageStanding.Game.Core.Events;

internal readonly struct StageRunStartedEvent
{
    public string StageId { get; }

    public StageRunStartedEvent(string stageId)
    {
        StageId = stageId;
    }
}

internal readonly struct StageRunCompletedEvent
{
    public string StageId { get; }
    public bool IsVictory { get; }
    public bool BossKilled { get; }

    public StageRunCompletedEvent(string stageId, bool isVictory, bool bossKilled)
    {
        StageId = stageId;
        IsVictory = isVictory;
        BossKilled = bossKilled;
    }
}

internal readonly struct RunMetaXpBonusEvent
{
    public int Amount { get; }

    public RunMetaXpBonusEvent(int amount)
    {
        Amount = amount;
    }
}

