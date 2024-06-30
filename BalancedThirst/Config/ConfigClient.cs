using BalancedThirst.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BalancedThirst.Config;

public class ConfigClient : IModConfig
{
    public float ThirstBarX { get; set; }
    public float ThirstBarY { get; set; }
    public EnumPeeMode PeeMode { get; set; }
    public bool BladderBarVisible { get; set; } = false;
    public float HideBladderBarAt { get; set; } = 0.0f;
    public ConfigClient(ICoreAPI api, ConfigClient previousConfig = null)
    {
        if (previousConfig == null)
        {
            return;
        }
        ThirstBarX = previousConfig.ThirstBarX;
        ThirstBarY = previousConfig.ThirstBarY;
        PeeMode = previousConfig.PeeMode;
        BladderBarVisible = previousConfig.BladderBarVisible;
        HideBladderBarAt = previousConfig.HideBladderBarAt;
    }
}