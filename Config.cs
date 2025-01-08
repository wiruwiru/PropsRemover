using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace PropsRemover;

public class BaseConfigs : BasePluginConfig
{
    [JsonPropertyName("RemoveBlood")]
    public bool removeblood { get; set; } = false;
}