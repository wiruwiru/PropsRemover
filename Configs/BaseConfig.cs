using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace PropsRemover.Configs;

public class BaseConfigs : BasePluginConfig
{
    [JsonPropertyName("RemoveBlood")]
    public bool RemoveBlood { get; set; } = false;

    [JsonPropertyName("EnableDebug")]
    public bool EnableDebug { get; set; } = false;

    [JsonPropertyName("ConfigVersion")]
    public override int Version { get; set; } = 1;
}