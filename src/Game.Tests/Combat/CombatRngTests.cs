using Xunit;
using TheLastMageStanding.Game.Core.Combat;

namespace TheLastMageStanding.Game.Tests.Combat;

public class CombatRngTests
{
    [Fact]
    public void CombatRng_Deterministic_SameSeedSameResults()
    {
        var rng1 = new CombatRng(12345);
        var rng2 = new CombatRng(12345);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(rng1.NextFloat(), rng2.NextFloat());
        }
    }

    [Fact]
    public void CombatRng_DifferentSeeds_DifferentResults()
    {
        var rng1 = new CombatRng(12345);
        var rng2 = new CombatRng(54321);

        var v1 = rng1.NextFloat();
        var v2 = rng2.NextFloat();

        Assert.NotEqual(v1, v2);
    }

    [Fact]
    public void CombatRng_NextFloat_InRange()
    {
        var rng = new CombatRng(12345);

        for (int i = 0; i < 1000; i++)
        {
            var value = rng.NextFloat();
            Assert.InRange(value, 0f, 1f);
        }
    }

    [Fact]
    public void CombatRng_RollCrit_100Percent_AlwaysTrue()
    {
        var rng = new CombatRng(12345);

        for (int i = 0; i < 100; i++)
        {
            Assert.True(rng.RollCrit(1.0f));
        }
    }

    [Fact]
    public void CombatRng_RollCrit_0Percent_AlwaysFalse()
    {
        var rng = new CombatRng(12345);

        for (int i = 0; i < 100; i++)
        {
            Assert.False(rng.RollCrit(0.0f));
        }
    }

    [Fact]
    public void CombatRng_RollCrit_50Percent_ApproximatelyHalf()
    {
        var rng = new CombatRng(12345);
        int successes = 0;
        int trials = 10000;

        for (int i = 0; i < trials; i++)
        {
            if (rng.RollCrit(0.5f))
                successes++;
        }

        // With 10k trials, 50% should be close to 5000
        // Allow 2% margin (4900-5100)
        Assert.InRange(successes, trials * 0.48, trials * 0.52);
    }

    [Fact]
    public void CombatRng_Reseed_ChangesSequence()
    {
        var rng = new CombatRng(12345);
        var v1 = rng.NextFloat();
        
        rng.Seed(12345); // Reset to same seed
        var v2 = rng.NextFloat();

        Assert.Equal(v1, v2); // Should get same first value
    }
}
