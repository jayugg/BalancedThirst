using System;
using BalancedThirst.ModBehavior;
using BalancedThirst.Systems;
using BalancedThirst.Thirst;
using Vintagestory.API.Common;
using XLib.XLeveling;

namespace BalancedThirst.Compatibility;

public class XSkillsCompat : ModSystem
{
    private ICoreAPI _api;

    public override double ExecuteOrder() => 1.05;
    public override bool ShouldLoad(EnumAppSide forSide)
    {
        return BtCore.IsXSkillsLoaded && !BtCore.IsHoDLoaded;
    }

    public override void Start(ICoreAPI api)
    {
        this._api = api;
        var xLeveling = api.ModLoader.GetModSystem("XLib.XLeveling.XLeveling") as XLeveling;
        var survival = xLeveling?.GetSkill("survival");
        var camelHump = new Ability("camelhump", BtCore.Modid+":ability-camelhump", BtCore.Modid+":abilitydesc-camelhump", 1, 3, new int[] { 500, 1000, 1500 });
        camelHump.OnPlayerAbilityTierChanged += OnCamelHump;
        survival?.AddAbility(camelHump);
        var elephantBladder = new Ability("elephantbladder", BtCore.Modid+":ability-elephantbladder", BtCore.Modid+":abilitydesc-elephantbladder", 1, 2, new int[] { 750, 1500 });
        elephantBladder.OnPlayerAbilityTierChanged += OnElephantBladder;
        survival?.AddAbility(elephantBladder);
    }
    
    public static void OnCamelHump(PlayerAbility playerAbility, int oldTier)
    {
        var player = playerAbility.PlayerSkill.PlayerSkillSet.Player;
        if (player == null)
            return;
        var side = player.Entity?.Api.Side;
        var enumAppSide = EnumAppSide.Server;
        if (!(side.GetValueOrDefault() == enumAppSide & side.HasValue))
            return;
        var behavior = player.Entity.GetBehavior<EntityBehaviorThirst>();
        if (behavior == null)
            return;
        var factor = 1f + (float) ((double) playerAbility.Value(0)/500)*ConfigSystem.ConfigServer.CamelHumpMaxHydrationMultiplier;
        behavior.MaxHydrationModifier *= factor;
        behavior.Hydration *= factor;
        behavior.UpdateThirstBoosts();
    }
    
    public static void OnElephantBladder(PlayerAbility playerAbility, int oldTier)
    {
        var player = playerAbility.PlayerSkill.PlayerSkillSet.Player;
        if (player == null)
            return;
        var side = player.Entity?.Api.Side;
        var enumAppSide = EnumAppSide.Server;
        if (!(side.GetValueOrDefault() == enumAppSide & side.HasValue))
            return;
        var behavior = player.Entity.GetBehavior<EntityBehaviorBladder>();
        if (behavior == null)
            return;
        var factor = 1f + (float) ((double) playerAbility.Value(0)/750)*ConfigSystem.ConfigServer.ElephantBladderCapacityMultiplier;
        behavior.CapacityModifier *= factor;
    }
    
}