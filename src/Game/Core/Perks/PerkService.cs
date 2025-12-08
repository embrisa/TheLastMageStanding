using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Perks;

namespace TheLastMageStanding.Game.Core.Perks;

/// <summary>
/// Service for managing perk allocation, validation, and effects calculation.
/// </summary>
internal sealed class PerkService
{
    private readonly PerkTreeConfig _config;

    public PerkService(PerkTreeConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Check if a perk can be allocated (has points, prerequisites met, not at max rank).
    /// </summary>
    public PerkAllocationResult CanAllocate(string perkId, PlayerPerks playerPerks, PerkPoints perkPoints)
    {
        var perkDef = _config.GetPerk(perkId);
        if (perkDef == null)
        {
            return new PerkAllocationResult(false, "Perk not found");
        }

        var currentRank = playerPerks.GetRank(perkId);

        // Check if at max rank
        if (currentRank >= perkDef.MaxRank)
        {
            return new PerkAllocationResult(false, $"Already at max rank ({perkDef.MaxRank})");
        }

        // Check if have enough points
        if (perkPoints.AvailablePoints < perkDef.PointsPerRank)
        {
            return new PerkAllocationResult(false, $"Need {perkDef.PointsPerRank} points (have {perkPoints.AvailablePoints})");
        }

        // Check prerequisites
        foreach (var prereq in perkDef.Prerequisites)
        {
            var prereqRank = playerPerks.GetRank(prereq.PerkId);
            if (prereqRank < prereq.MinimumRank)
            {
                var prereqDef = _config.GetPerk(prereq.PerkId);
                var prereqName = prereqDef?.Name ?? prereq.PerkId;
                return new PerkAllocationResult(false, $"Requires {prereqName} rank {prereq.MinimumRank}");
            }
        }

        return new PerkAllocationResult(true, "Can allocate");
    }

    /// <summary>
    /// Allocate a rank in a perk. Returns true if successful.
    /// </summary>
    public bool Allocate(string perkId, ref PlayerPerks playerPerks, ref PerkPoints perkPoints)
    {
        var result = CanAllocate(perkId, playerPerks, perkPoints);
        if (!result.CanAllocate)
        {
            return false;
        }

        var perkDef = _config.GetPerk(perkId);
        if (perkDef == null)
        {
            return false;
        }

        var currentRank = playerPerks.GetRank(perkId);
        playerPerks.SetRank(perkId, currentRank + 1);
        perkPoints.AvailablePoints -= perkDef.PointsPerRank;

        return true;
    }

    /// <summary>
    /// Deallocate a rank in a perk (for granular respec). Returns true if successful.
    /// Cannot deallocate if another perk depends on this one.
    /// </summary>
    public bool Deallocate(string perkId, ref PlayerPerks playerPerks, ref PerkPoints perkPoints)
    {
        var perkDef = _config.GetPerk(perkId);
        if (perkDef == null)
        {
            return false;
        }

        var currentRank = playerPerks.GetRank(perkId);
        if (currentRank == 0)
        {
            return false; // Nothing to deallocate
        }

        // Check if any allocated perks depend on this one
        foreach (var otherPerk in _config.Perks)
        {
            var otherRank = playerPerks.GetRank(otherPerk.Id);
            if (otherRank > 0)
            {
                foreach (var prereq in otherPerk.Prerequisites)
                {
                    if (prereq.PerkId == perkId && currentRank - 1 < prereq.MinimumRank)
                    {
                        return false; // Can't deallocate, another perk depends on it
                    }
                }
            }
        }

        playerPerks.SetRank(perkId, currentRank - 1);
        perkPoints.AvailablePoints += perkDef.PointsPerRank;

        return true;
    }

    /// <summary>
    /// Full respec: clear all perks and refund all points.
    /// </summary>
    public void RespecAll(ref PlayerPerks playerPerks, ref PerkPoints perkPoints)
    {
        // Calculate total spent points
        int refundPoints = 0;
        foreach (var (perkId, rank) in playerPerks.AllocatedRanks)
        {
            var perkDef = _config.GetPerk(perkId);
            if (perkDef != null)
            {
                refundPoints += rank * perkDef.PointsPerRank;
            }
        }

        // Clear all allocations
        playerPerks.AllocatedRanks.Clear();

        // Refund points
        perkPoints.AvailablePoints += refundPoints;
    }

    /// <summary>
    /// Calculate the total effects from all allocated perks.
    /// </summary>
    public PerkEffects CalculateTotalEffects(PlayerPerks playerPerks)
    {
        var effectsList = new List<PerkEffects>();

        foreach (var (perkId, rank) in playerPerks.AllocatedRanks)
        {
            var perkDef = _config.GetPerk(perkId);
            if (perkDef == null || rank <= 0)
            {
                continue;
            }

            // Get effects for this rank
            if (perkDef.EffectsByRank.TryGetValue(rank, out var effects))
            {
                effectsList.Add(effects);
            }
        }

        return effectsList.Count > 0 
            ? PerkEffects.Combine(effectsList.ToArray()) 
            : PerkEffects.None;
    }
}

/// <summary>
/// Result of attempting to allocate a perk.
/// </summary>
internal readonly struct PerkAllocationResult
{
    public bool CanAllocate { get; }
    public string Message { get; }

    public PerkAllocationResult(bool canAllocate, string message)
    {
        CanAllocate = canAllocate;
        Message = message;
    }
}
