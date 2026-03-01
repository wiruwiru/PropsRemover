using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes.Registration;

namespace PropsRemover.Commands;

public class RmpropsCommand(PropsRemoverBase plugin)
{
    [ConsoleCommand("css_rmprops", "Forcefully removes all registered props for the current map.")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnRmpropsCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || commandInfo == null) return;

        try
        {
            var currentMap = Server.MapName;
            var mapData = plugin.PropDataList.FirstOrDefault(data => data.Map.Equals(currentMap, StringComparison.OrdinalIgnoreCase));

            if (mapData == null || mapData.Props.Count == 0)
            {
                player.PrintToChat($"{plugin.Localizer["Prefix"]} {plugin.Localizer["NoRegisteredProps", currentMap]}");
                return;
            }

            foreach (var propPath in mapData.Props)
            {
                PropsRemoverBase.RemoveEntitiesByPath(propPath);
            }

            player.PrintToChat($"{plugin.Localizer["Prefix"]} {plugin.Localizer["PropsRemoved", currentMap]}");
        }
        catch (Exception ex)
        {
            player.PrintToChat($"{plugin.Localizer["Prefix"]} {plugin.Localizer["ErrorRemovingProps", ex.Message]}");
        }
    }
}