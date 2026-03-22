namespace TheLastMageStanding.Game.Core.Ecs.Runtime;

internal static class EcsRuntimeComposer
{
    public static EcsRuntimeRegistration Compose(EcsRuntimeModuleContext context)
    {
        IEcsRuntimeModule[] modules =
        [
            new EcsDebugRuntimeModule(),
            new EcsCommonRuntimeModule(),
            new EcsHubRuntimeModule(),
            new EcsStageRuntimeModule(),
        ];

        return Compose(context, modules);
    }

    public static EcsRuntimeRegistration Compose(
        EcsRuntimeModuleContext context,
        IEnumerable<IEcsRuntimeModule> modules)
    {
        var registration = new EcsRuntimeRegistration();
        registration.ProvideCapability(EcsRuntimeCapability.SessionEntity, nameof(EcsWorldRunner));

        foreach (var module in OrderModules(modules))
        {
            registration.RegisterModule(module.Definition.Name);
            module.Register(registration, context);
        }

        return registration;
    }

    private static IReadOnlyList<IEcsRuntimeModule> OrderModules(IEnumerable<IEcsRuntimeModule> modules)
    {
        var moduleList = modules.ToList();
        var duplicateNames = moduleList
            .GroupBy(module => module.Definition.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        if (duplicateNames.Length > 0)
        {
            throw new InvalidOperationException(
                $"Runtime module names must be unique. Duplicate modules: {string.Join(", ", duplicateNames)}.");
        }

        var modulesByName = moduleList.ToDictionary(module => module.Definition.Name, StringComparer.Ordinal);

        foreach (var module in moduleList)
        {
            foreach (var dependency in module.Definition.Dependencies)
            {
                if (!modulesByName.ContainsKey(dependency))
                {
                    throw new InvalidOperationException(
                        $"Runtime module '{module.Definition.Name}' depends on missing module '{dependency}'.");
                }
            }
        }

        var dependencyCounts = moduleList.ToDictionary(
            module => module.Definition.Name,
            module => module.Definition.Dependencies.Distinct(StringComparer.Ordinal).Count(),
            StringComparer.Ordinal);
        var dependents = moduleList.ToDictionary(
            module => module.Definition.Name,
            _ => new List<string>(),
            StringComparer.Ordinal);

        foreach (var module in moduleList)
        {
            foreach (var dependency in module.Definition.Dependencies.Distinct(StringComparer.Ordinal))
            {
                dependents[dependency].Add(module.Definition.Name);
            }
        }

        var queue = new Queue<IEcsRuntimeModule>(
            moduleList.Where(module => dependencyCounts[module.Definition.Name] == 0));
        var ordered = new List<IEcsRuntimeModule>(moduleList.Count);

        while (queue.Count > 0)
        {
            var module = queue.Dequeue();
            ordered.Add(module);

            foreach (var dependentName in dependents[module.Definition.Name])
            {
                dependencyCounts[dependentName]--;
                if (dependencyCounts[dependentName] == 0)
                {
                    queue.Enqueue(modulesByName[dependentName]);
                }
            }
        }

        if (ordered.Count == moduleList.Count)
        {
            return ordered;
        }

        var cycleModules = dependencyCounts
            .Where(pair => pair.Value > 0)
            .Select(pair => pair.Key)
            .OrderBy(name => name, StringComparer.Ordinal);
        throw new InvalidOperationException(
            $"Runtime module dependency cycle detected: {string.Join(", ", cycleModules)}.");
    }
}
