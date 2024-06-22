using BalancedThirst.Items;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches;

public class CookingRecipe_Patch
{
    public static void Postfix()
    {
        CookingRecipe.NamingRegistry["boilingwater"] = new WaterCookingName();
    }
}