using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Core.Attributes.Registration;

namespace PropsRemover;

[MinimumApiVersion(296)]
public class PropsRemoverBase : BasePlugin, IPluginConfig<BaseConfigs>
{
    public override string ModuleName => "PropsRemover";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "luca.uy";
    public override string ModuleDescription => "Automatically removes certain props on map start";

    private List<PropData> _propDataList = new();
    private string _configPath => Path.Combine(ModuleDirectory, "prop_data.json");
    private string _currentMap = "";

    public required BaseConfigs Config { get; set; }
    public void OnConfigParsed(BaseConfigs config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        AddCommand("rmprops", "Forcefully removes all registered props for the current map.", (player, commandInfo) =>
        {
            if (player == null || commandInfo == null) return;

            var permissionValidator = new RequiresPermissions("@css/root");
            if (!permissionValidator.CanExecuteCommand(player))
            {
                player.PrintToChat($"{Localizer["Prefix"]} {Localizer["NoPermissions"]}");
                return;
            }

            try
            {
                var currentMap = Server.MapName;
                var mapData = _propDataList.FirstOrDefault(data => data.Map.Equals(currentMap, StringComparison.OrdinalIgnoreCase));

                if (mapData == null || mapData.Props.Count == 0)
                {
                    player.PrintToChat($"{Localizer["Prefix"]} {Localizer["NoRegisteredProps", currentMap]}");
                    return;
                }

                foreach (var propPath in mapData.Props)
                {
                    RemoveEntitiesByPath(propPath);
                }

                player.PrintToChat($"{Localizer["Prefix"]} {Localizer["PropsRemoved", currentMap]}");
            }
            catch (Exception ex)
            {
                player.PrintToChat($"{Localizer["Prefix"]} {Localizer["ErrorRemovingProps", ex.Message]}");
            }
        });

        AddCommand("RMP", "Activates or deactivates props elimination.", (player, commandInfo) =>
        {
            if (player == null || commandInfo == null) return;

            var permissionValidator = new RequiresPermissions("@css/root");
            if (!permissionValidator.CanExecuteCommand(player))
            {
                player.PrintToChat($"{Localizer["Prefix"]} {Localizer["NoPermissions"]}");
                return;
            }

            _isOnTakeDamageEnabled = !_isOnTakeDamageEnabled;

            var status = _isOnTakeDamageEnabled ? Localizer["Enabled"] : Localizer["Disabled"];
            var playerName = player?.PlayerName ?? Localizer["Unknown"];
            player?.PrintToChat($"{Localizer["Prefix"]} {Localizer["TogglePropRemoval", status, playerName]}");
        });

        _propDataList = LoadPropData();

        RegisterListener<Listeners.OnMapStart>(mapName =>
        {
            _currentMap = mapName;
        });

        if (hotReload)
        {
            _currentMap = Server.MapName;
        }

        if (Config.removeblood)
        {
            HookUserMessage(411, um => { um.Recipients.Clear(); return HookResult.Continue; }, HookMode.Pre);
        }

        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Post);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterListener<Listeners.OnMapEnd>(() => Unload(true));
    }

    public override void Unload(bool hotReload)
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Post);
    }

    private bool _isOnTakeDamageEnabled = false;
    private HookResult OnTakeDamage(DynamicHook hook)
    {
        if (!_isOnTakeDamageEnabled) return HookResult.Continue;

        try
        {
            var entity = hook.GetParam<CEntityInstance>(0);
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
            Console.WriteLine($"{Localizer["Prefix"]} {Localizer["TakeDamageError", ex.Message]}");
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        try
        {
            var mapData = _propDataList.FirstOrDefault(data => data.Map.Equals(_currentMap, StringComparison.OrdinalIgnoreCase));
            if (mapData == null || mapData.Props.Count == 0)
            {
                Console.WriteLine($"[PropsRemover] No props registered for the map {_currentMap}.");
                return HookResult.Continue;
            }

            Console.WriteLine($"[PropsRemover] Removing registered props on the map {_currentMap}...");

            foreach (var propPath in mapData.Props)
            {
                RemoveEntitiesByPath(propPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PropsRemover] Error in OnRoundStart: {ex.Message}");
        }

        return HookResult.Continue;
    }

    private void RemoveEntitiesByPath(string entityPath)
    {
        try
        {
            Console.WriteLine($"[PropsRemover] Attempting to remove entities with path: {entityPath}");

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
                Console.WriteLine($"[PropsRemover] No entity found with path: {entityPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PropsRemover] Error removing entities: {ex.Message}");
        }
    }

    private IEnumerable<CEntityInstance> GetEntities()
    {
        try
        {
            var entities = Utilities.GetAllEntities().ToList();

            if (entities == null || !entities.Any())
            {
                Console.WriteLine("[PropsRemover] No active entities found.");
                return Enumerable.Empty<CEntityInstance>();
            }

            Console.WriteLine($"[PropsRemover] {entities.Count} entities found.");

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
            Console.WriteLine($"[PropsRemover] Error getting entities: {ex.Message}");
            return Enumerable.Empty<CEntityInstance>();
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
                Console.WriteLine("[PropsRemover] Model not available for this prop.");
                return;
            }

            var mapData = _propDataList.FirstOrDefault(data => data.Map.Equals(_currentMap, StringComparison.OrdinalIgnoreCase));
            if (mapData == null)
            {
                mapData = new PropData
                {
                    Map = _currentMap,
                    Props = new List<string>()
                };
                _propDataList.Add(mapData);
            }

            if (!mapData.Props.Contains(entityPath))
            {
                mapData.Props.Add(entityPath);
                Console.WriteLine($"[PropsRemover] Prop registered: {entityPath}");

                File.WriteAllText(_configPath, JsonSerializer.Serialize(_propDataList, new JsonSerializerOptions { WriteIndented = true }));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PropsRemover] Error saving prop data: {ex.Message}");
        }
    }

    private List<PropData> LoadPropData()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                Console.WriteLine("[PropsRemover] prop_data.json loaded successfully.");
                return JsonSerializer.Deserialize<List<PropData>>(json) ?? new List<PropData>();
            }
            else
            {
                Console.WriteLine("[PropsRemover] prop_data.json does not exist.");
                return new List<PropData>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PropsRemover] Error loading data: {ex.Message}");
            return new List<PropData>();
        }
    }

    private class PropData
    {
        public string Map { get; set; } = string.Empty;
        public List<string> Props { get; set; } = new();
    }
}