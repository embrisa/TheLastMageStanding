using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Events;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Audio;

public class AudioSettingsConfigTests
{
    [Fact]
    public void GetCategoryVolume_ClampsToOne()
    {
        var settings = AudioSettingsConfig.Default;
        settings.MasterVolume = 2f;
        settings.SfxVolume = 2f;

        var volume = settings.GetCategoryVolume(SfxCategory.Attack);

        Assert.Equal(1f, volume, 3);
    }

    [Fact]
    public void GetCategoryVolume_RespectsMuteAll()
    {
        var settings = AudioSettingsConfig.Default;
        settings.MuteAll = true;

        Assert.Equal(0f, settings.GetCategoryVolume(SfxCategory.UI));
        Assert.Equal(0f, settings.GetCategoryVolume(SfxCategory.Impact));
    }

    [Fact]
    public void Normalize_HandlesNanValues()
    {
        var settings = AudioSettingsConfig.Default;
        settings.MasterVolume = float.NaN;
        settings.SfxVolume = float.PositiveInfinity;

        settings.Normalize();

        Assert.Equal(0f, settings.MasterVolume);
        Assert.Equal(0f, settings.SfxVolume);
    }
}

