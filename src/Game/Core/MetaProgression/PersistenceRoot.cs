using TheLastMageStanding.Game.Core.Player;

namespace TheLastMageStanding.Game.Core.MetaProgression;

/// <summary>
/// Composition root for persistence. Owns the filesystem, root paths, and
/// construction of slot-scoped persistence services.
/// </summary>
public sealed class PersistenceRoot
{
    private const string SlotsFolderName = "Slots";
    private readonly IFileSystem _fileSystem;

    public PersistenceRoot(IFileSystem fileSystem, string? rootPath = null)
    {
        _fileSystem = fileSystem;
        RootPath = rootPath ?? PlayerProfileService.GetDefaultSaveDirectory();
        EnsureDirectory(RootPath);
        EnsureDirectory(SlotsRootPath);
    }

    public IFileSystem FileSystem => _fileSystem;
    public string RootPath { get; }
    public string SlotsRootPath => Path.Combine(RootPath, SlotsFolderName);

    public SaveSlotService CreateSaveSlotService()
    {
        return new SaveSlotService(this);
    }

    internal SlotPersistenceScope ForSlot(string slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId))
        {
            throw new ArgumentException("Slot id is required.", nameof(slotId));
        }

        var slotPath = GetSlotPath(slotId);
        return new SlotPersistenceScope(
            slotId,
            slotPath,
            CreatePlayerProfileService(slotPath),
            CreateRunHistoryService(slotPath),
            CreateEquipmentPersistenceService(slotPath),
            CreatePerkPersistenceService(slotPath));
    }

    internal string GetSlotPath(string slotId)
    {
        var slotPath = Path.Combine(SlotsRootPath, slotId);
        EnsureDirectory(slotPath);
        return slotPath;
    }

    internal PlayerProfileService CreatePlayerProfileService(string saveDirectory)
    {
        return new PlayerProfileService(_fileSystem, saveDirectory);
    }

    internal RunHistoryService CreateRunHistoryService(string saveDirectory)
    {
        return new RunHistoryService(_fileSystem, saveDirectory);
    }

    internal EquipmentPersistenceService CreateEquipmentPersistenceService(string saveDirectory)
    {
        return new EquipmentPersistenceService(_fileSystem, saveDirectory);
    }

    internal PerkPersistenceService CreatePerkPersistenceService(string saveDirectory)
    {
        return new PerkPersistenceService(_fileSystem, saveDirectory);
    }

    private void EnsureDirectory(string path)
    {
        if (!_fileSystem.DirectoryExists(path))
        {
            _fileSystem.CreateDirectory(path);
        }
    }
}

/// <summary>
/// Slot-scoped persistence services. Player-owned and current-run data must be
/// resolved from the active slot through this scope.
/// </summary>
internal sealed record SlotPersistenceScope(
    string SlotId,
    string SlotPath,
    PlayerProfileService PlayerProfile,
    RunHistoryService RunHistory,
    EquipmentPersistenceService EquipmentPersistence,
    PerkPersistenceService PerkPersistence);
