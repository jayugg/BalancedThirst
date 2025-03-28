using System.Drawing;
using BalancedThirst.Hud;
using BalancedThirst.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BalancedThirst.Config;

public class ConfigClient : IModConfig
{
    public float ThirstBarX { get; set; }
    public float ThirstBarY { get; set; }
    public string ThirstBarColor { get; set; } = ModGuiStyle.ThirstBarColor.ToHex();
    
    public ConfigClient(ICoreAPI api, ConfigClient previousConfig = null)
    {
        if (previousConfig == null)
        {
            return;
        }
        ThirstBarX = previousConfig.ThirstBarX;
        ThirstBarY = previousConfig.ThirstBarY;
        ThirstBarColor = previousConfig.ThirstBarColor;
    }
}