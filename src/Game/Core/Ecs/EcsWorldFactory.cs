using TheLastMageStanding.Game.Core.Audio;
using TheLastMageStanding.Game.Core.Camera;
using TheLastMageStanding.Game.Core.Campaign;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.MetaProgression;
using TheLastMageStanding.Game.Core.SceneState;

namespace TheLastMageStanding.Game.Core.Ecs;

internal interface IEcsWorldFactory
{
    EcsWorldRunner Create(string slotId);
}

internal sealed class EcsWorldFactory : IEcsWorldFactory
{
    private readonly Camera2D _camera;
    private readonly AudioSettingsConfig _audioSettings;
    private readonly AudioSettingsStore _audioSettingsStore;
    private readonly VideoSettingsConfig _videoSettings;
    private readonly VideoSettingsStore _videoSettingsStore;
    private readonly InputBindingsConfig _inputBindings;
    private readonly InputBindingsStore _inputBindingsStore;
    private readonly MusicService _musicService;
    private readonly EventBus _eventBus;
    private readonly StageRegistry _stageRegistry;
    private readonly SceneStateService _sceneStateService;
    private readonly SceneManager _sceneManager;
    private readonly PersistenceRoot _persistenceRoot;

    public EcsWorldFactory(
        Camera2D camera,
        AudioSettingsConfig audioSettings,
        AudioSettingsStore audioSettingsStore,
        VideoSettingsConfig videoSettings,
        VideoSettingsStore videoSettingsStore,
        InputBindingsConfig inputBindings,
        InputBindingsStore inputBindingsStore,
        MusicService musicService,
        EventBus eventBus,
        StageRegistry stageRegistry,
        SceneStateService sceneStateService,
        SceneManager sceneManager,
        PersistenceRoot persistenceRoot)
    {
        _camera = camera;
        _audioSettings = audioSettings;
        _audioSettingsStore = audioSettingsStore;
        _videoSettings = videoSettings;
        _videoSettingsStore = videoSettingsStore;
        _inputBindings = inputBindings;
        _inputBindingsStore = inputBindingsStore;
        _musicService = musicService;
        _eventBus = eventBus;
        _stageRegistry = stageRegistry;
        _sceneStateService = sceneStateService;
        _sceneManager = sceneManager;
        _persistenceRoot = persistenceRoot;
    }

    public EcsWorldRunner Create(string slotId)
    {
        return new EcsWorldRunner(new EcsWorldRunnerDependencies(
            _camera,
            _audioSettings,
            _audioSettingsStore,
            _videoSettings,
            _videoSettingsStore,
            _inputBindings,
            _inputBindingsStore,
            _musicService,
            _eventBus,
            _stageRegistry,
            _sceneStateService,
            _sceneManager,
            _persistenceRoot.ForSlot(slotId)));
    }
}
