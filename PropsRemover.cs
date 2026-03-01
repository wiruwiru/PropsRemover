using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using System.Text.Json;

using PropsRemover.Utils;
using PropsRemover.Models;
using PropsRemover.Configs;
using PropsRemover.Commands;

namespace PropsRemover;

[MinimumApiVersion(363)]
public class PropsRemoverBase : BasePlugin, IPluginConfig<BaseConfigs>
{
    public override string ModuleName => "PropsRemover";
    public override string ModuleVersion => "1.0.2";
    public override string ModuleAuthor => "luca.uy";
    public override string ModuleDescription => "Automatically removes certain props on map start";

    internal List<PropData> PropDataList = [];
    private string ConfigPath => Path.Combine(ModuleDirectory, "prop_data.json");
    internal string _currentMap = "";

    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public required BaseConfigs Config { get; set; }
    public void OnConfigParsed(BaseConfigs config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        PropDataList = LoadPropData();

        RegisterAllAttributes(new RmpCommand(this));
        RegisterAllAttributes(new RmpropsCommand(this));

        RegisterListener<Listeners.OnMapStart>(mapName =>
        {
            _currentMap = mapName;
        });

        if (hotReload)
        {
            _currentMap = Server.MapName;
        }

        if (Config.RemoveBlood)
        {
            // GE_PlaceDecalEvent (ID 201 in EBaseGameEvents)
            HookUserMessage(201, um => HookResult.Stop, HookMode.Pre);
        }

        RegisterListener<Listeners.OnEntityTakeDamagePre>(OnTakeDamage);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
    }

    internal bool IsOnTakeDamageEnabled = false;

    private void OnMapEnd()
    {
        IsOnTakeDamageEnabled = false;
        PropDataList = LoadPropData();
        Utils.Logger.LogInfo("MapEnd", "Map ended. Reloaded prop data.");
    }

    private HookResult OnTakeDamage(CBaseEntity entity, CTakeDamageInfo info)
    {
        if (!IsOnTakeDamageEnabled) return HookResult.Continue;

        Utils.Logger.LogDebug("TakeDamage", $"Entity hit: {entity.DesignerName} (Valid: {entity.IsValid})");

        try
        {
            if (entity == null || !entity.IsValid) return HookResult.Continue;

            if (entity.DesignerName.Contains("prop_physics_override") ||
                entity.DesignerName.Contains("prop_physics") ||
                entity.DesignerName.Contains("prop_physics_multiplayer") ||
                entity.DesignerName.Contains("prop_dynamic") ||
                entity.DesignerName.Contains("prop_static"))
            {
                SavePropData(entity);

                entity.AcceptInput("Kill");
                Server.PrintToChatAll($"{Localizer["Prefix"]} {Localizer["PropRemoved", entity.DesignerName]}");
            }
        }
        catch (Exception ex)
        {
            Utils.Logger.LogError("TakeDamage", $"{Localizer["TakeDamageError", ex.Message]}");
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        try
        {
            var mapData = PropDataList.FirstOrDefault(data => data.Map.Equals(_currentMap, StringComparison.OrdinalIgnoreCase));
            if (mapData == null || mapData.Props.Count == 0)
            {
                Utils.Logger.LogWarning("RoundStart", $"No props registered for the map {_currentMap}.");
                return HookResult.Continue;
            }

            Utils.Logger.LogInfo("RoundStart", $"Removing registered props on the map {_currentMap}...");

            foreach (var propPath in mapData.Props)
            {
                RemoveEntitiesByPath(propPath);
            }
        }
        catch (Exception ex)
        {
            Utils.Logger.LogError("RoundStart", $"Error in OnRoundStart: {ex.Message}");
        }

        return HookResult.Continue;
    }

    internal static void RemoveEntitiesByPath(string entityPath)
    {
        try
        {
            Utils.Logger.LogDebug("RemoveEntities", $"Attempting to remove entities with path: {entityPath}");

            var entities = GetEntities();
            bool foundEntity = false;

            foreach (var entity in entities)
            {
                if (entity == null || !entity.IsValid)
                    continue;

                var modelEntity = entity.As<CBaseModelEntity>();
                string? currentPath = modelEntity?.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState?.ModelName;

                if (currentPath?.Equals(entityPath, StringComparison.OrdinalIgnoreCase) == true)
                {
                    foundEntity = true;
                    entity.AcceptInput("Kill");
                }
            }

            if (!foundEntity)
            {
                Utils.Logger.LogWarning("RemoveEntities", $"No entity found with path: {entityPath}");
            }
        }
        catch (Exception ex)
        {
            Utils.Logger.LogError("RemoveEntities", $"Error removing entities: {ex.Message}");
        }
    }

    private static IEnumerable<CEntityInstance> GetEntities()
    {
        try
        {
            var entities = Utilities.GetAllEntities().ToList();

            if (entities == null || entities.Count == 0)
            {
                Utils.Logger.LogWarning("GetEntities", "No active entities found.");
                return [];
            }

            Utils.Logger.LogDebug("GetEntities", $"{entities.Count} entities found.");

            var filteredEntities = entities.Where(entity =>
            {
                if (entity == null || !entity.IsValid || string.IsNullOrWhiteSpace(entity.DesignerName))
                    return false;

                if (entity.DesignerName.Equals("prop_static", StringComparison.OrdinalIgnoreCase) ||
                    entity.DesignerName.Equals("prop_dynamic", StringComparison.OrdinalIgnoreCase) ||
                    entity.DesignerName.Equals("prop_physics", StringComparison.OrdinalIgnoreCase) ||
                    entity.DesignerName.Equals("prop_physics_multiplayer", StringComparison.OrdinalIgnoreCase) ||
                    entity.DesignerName.Equals("prop_physics_override", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return false;
            });

            return filteredEntities;
        }
        catch (Exception ex)
        {
            Utils.Logger.LogError("GetEntities", $"Error getting entities: {ex.Message}");
            return [];
        }
    }

    private void SavePropData(CEntityInstance entity)
    {
        try
        {
            var getEntity = entity.As<CBaseModelEntity>();
            var entityPath = getEntity?.CBodyComponent?.SceneNode?.GetSkeletonInstance().ModelState.ModelName;

            if (string.IsNullOrEmpty(entityPath))
            {
                Utils.Logger.LogWarning("SavePropData", "Model not available for this prop.");
                return;
            }

            var mapData = PropDataList.FirstOrDefault(data => data.Map.Equals(_currentMap, StringComparison.OrdinalIgnoreCase));
            if (mapData == null)
            {
                mapData = new PropData
                {
                    Map = _currentMap,
                    Props = []
                };
                PropDataList.Add(mapData);
            }

            if (!mapData.Props.Contains(entityPath))
            {
                mapData.Props.Add(entityPath);
                Utils.Logger.LogInfo("SavePropData", $"Prop registered: {entityPath}");

                File.WriteAllText(ConfigPath, JsonSerializer.Serialize(PropDataList, _jsonOptions));
            }
        }
        catch (Exception ex)
        {
            Utils.Logger.LogError("SavePropData", $"Error saving prop data: {ex.Message}");
        }
    }

    private List<PropData> LoadPropData()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                Utils.Logger.LogInfo("LoadPropData", "prop_data.json loaded successfully.");
                return JsonSerializer.Deserialize<List<PropData>>(json) ?? [];
            }
            else
            {
                Utils.Logger.LogWarning("LoadPropData", "prop_data.json does not exist.");
                return [];
            }
        }
        catch (Exception ex)
        {
            Utils.Logger.LogError("LoadPropData", $"Error loading data: {ex.Message}");
            return [];
        }
    }

    public override void Unload(bool hotReload)
    {
        RemoveListener<Listeners.OnEntityTakeDamagePre>(OnTakeDamage);
        Utils.Logger.LogInfo("Unload", "Unloading PropsRemover...");
    }
}