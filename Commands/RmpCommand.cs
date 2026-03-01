using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes.Registration;

namespace PropsRemover.Commands;

public class RmpCommand(PropsRemoverBase plugin)
{
    [ConsoleCommand("css_rmp", "Activates or deactivates props elimination.")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnRmpCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || commandInfo == null) return;

        plugin.IsOnTakeDamageEnabled = !plugin.IsOnTakeDamageEnabled;

        var status = plugin.IsOnTakeDamageEnabled ? plugin.Localizer["Enabled"] : plugin.Localizer["Disabled"];
        var playerName = player.PlayerName ?? plugin.Localizer["Unknown"];
        player.PrintToChat($"{plugin.Localizer["Prefix"]} {plugin.Localizer["TogglePropRemoval", status, playerName]}");
    }
}