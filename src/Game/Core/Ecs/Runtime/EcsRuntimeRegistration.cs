using System.Collections.Generic;
using System.Linq;
using TheLastMageStanding.Game.Core.Ecs.Systems;

namespace TheLastMageStanding.Game.Core.Ecs.Runtime;

internal enum EcsRuntimeCapability
{
    SessionEntity,
    SessionSettingsState,
    StageRunState,
}

internal enum EcsSceneScope
{
    Common,
    Hub,
    Stage,
}

internal sealed class EcsScopedPhase<TSystem>
    where TSystem : class, IEcsSystem
{
    private readonly List<TSystem> _common = [];
    private readonly List<TSystem> _hub = [];
    private readonly List<TSystem> _stage = [];

    public IReadOnlyList<TSystem> Common => _common;
    public IReadOnlyList<TSystem> Hub => _hub;
    public IReadOnlyList<TSystem> Stage => _stage;

    public void Add(EcsSceneScope scope, TSystem system)
    {
        GetList(scope).Add(system);
    }

    public IEnumerable<TSystem> EnumerateAll()
    {
        return _common.Concat(_hub).Concat(_stage);
    }

    private List<TSystem> GetList(EcsSceneScope scope) =>
        scope switch
        {
            EcsSceneScope.Common => _common,
            EcsSceneScope.Hub => _hub,
            EcsSceneScope.Stage => _stage,
            _ => _common,
        };
}

internal sealed class EcsLoadContentPhase
{
    private readonly List<ILoadContentSystem> _systems = [];
    private readonly HashSet<object> _registered = new(ReferenceEqualityComparer.Instance);

    public IReadOnlyList<ILoadContentSystem> Systems => _systems;

    public void Add(ILoadContentSystem system)
    {
        if (_registered.Add(system))
        {
            _systems.Add(system);
        }
    }
}

internal sealed class EcsRuntimeRegistration
{
    private readonly List<string> _moduleOrder = [];
    private readonly Dictionary<EcsRuntimeCapability, string> _capabilityProviders = [];

    public EcsScopedPhase<IUpdateSystem> Update { get; } = new();
    public EcsScopedPhase<IUpdateSystem> StageSessionUpdate { get; } = new();
    public EcsScopedPhase<IUpdateSystem> StagePreGameplayUpdate { get; } = new();
    public EcsScopedPhase<IUpdateSystem> StageGameplayUpdate { get; } = new();
    public EcsScopedPhase<IUpdateSystem> StageHitStopFeedbackUpdate { get; } = new();
    public EcsScopedPhase<IDrawSystem> Draw { get; } = new();
    public EcsScopedPhase<IUiDrawSystem> UiDraw { get; } = new();
    public EcsScopedPhase<IUiDrawSystem> ScreenSpaceUiDraw { get; } = new();
    public EcsLoadContentPhase LoadContent { get; } = new();
    public IReadOnlyList<string> ModuleOrder => _moduleOrder;
    public IReadOnlyDictionary<EcsRuntimeCapability, string> CapabilityProviders => _capabilityProviders;

    public void RegisterModule(string moduleName)
    {
        _moduleOrder.Add(moduleName);
    }

    public void ProvideCapability(EcsRuntimeCapability capability, string provider)
    {
        if (_capabilityProviders.TryGetValue(capability, out var existingProvider))
        {
            throw new InvalidOperationException(
                $"Runtime capability '{capability}' is already provided by '{existingProvider}' and cannot also be provided by '{provider}'.");
        }

        _capabilityProviders.Add(capability, provider);
    }

    public void RequireCapability(EcsRuntimeCapability capability, string consumer)
    {
        if (_capabilityProviders.ContainsKey(capability))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Runtime composition missing capability '{capability}' required by '{consumer}'.");
    }

    public IEnumerable<IEcsSystem> EnumerateSystemsForInitialization()
    {
        var systems = Update.EnumerateAll().Cast<IEcsSystem>()
            .Concat(StageSessionUpdate.EnumerateAll().Cast<IEcsSystem>())
            .Concat(StagePreGameplayUpdate.EnumerateAll().Cast<IEcsSystem>())
            .Concat(StageGameplayUpdate.EnumerateAll().Cast<IEcsSystem>())
            .Concat(StageHitStopFeedbackUpdate.EnumerateAll().Cast<IEcsSystem>())
            .Concat(Draw.EnumerateAll().Cast<IEcsSystem>())
            .Concat(UiDraw.EnumerateAll().Cast<IEcsSystem>())
            .Concat(ScreenSpaceUiDraw.EnumerateAll().Cast<IEcsSystem>())
            .Concat(LoadContent.Systems.Cast<IEcsSystem>());

        var initialized = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var system in systems)
        {
            if (initialized.Add(system))
            {
                yield return system;
            }
        }
    }
}
