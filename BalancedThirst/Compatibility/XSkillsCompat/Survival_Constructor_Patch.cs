using System.Collections.Generic;
using BalancedThirst.Thirst;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using XLib.XLeveling;
using XSkills;

namespace BalancedThirst.XSkillsCompat;

public class Survival_Constructor_Patch
{
    public static Dictionary<Survival, int> CamelHumpIDs { get; private set; } = new Dictionary<Survival, int>();
    
    public static void Postfix(Survival __instance)
    {
        BtCore.Logger.Warning("Loading Survival Constructor Postfix");
        CamelHumpIDs.Add(__instance,
            __instance.AddAbility(new Ability("camelhump", "xskills:ability-camelhump",
                "xskills:abilitydesc-camelhump", 1, 3, new[]
                {
                    500,
                    1000,
                    1500
                }))
        );
        __instance[CamelHumpIDs[__instance]].OnPlayerAbilityTierChanged += new OnPlayerAbilityTierChangedDelegate(OnCamelHump);
    }
    
    public static void OnCamelHump(PlayerAbility playerAbility, int oldTier)
    {
        BtCore.Logger.Warning("oncamelhump");
        IPlayer player = playerAbility.PlayerSkill.PlayerSkillSet.Player;
        if (player == null)
            return;
        EnumAppSide? side = player.Entity?.Api.Side;
        EnumAppSide enumAppSide = EnumAppSide.Server;
        if (!(side.GetValueOrDefault() == enumAppSide & side.HasValue))
            return;
        EntityBehaviorThirst behavior = player.Entity.GetBehavior<EntityBehaviorThirst>();
        if (behavior == null)
            return;
        float num = (1500 + playerAbility.Value(0)) / behavior.MaxHydration;
        behavior.MaxHydration = (BtCore.ConfigServer.MaxHydration + playerAbility.Value(0));
        behavior.Euhydration *= num;
        behavior.Hydration *= num;
        behavior.UpdateThirstBoosts();
    }
}