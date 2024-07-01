using BalancedThirst.ModBehavior;
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
        XLeveling xLeveling = api.ModLoader.GetModSystem("XLib.XLeveling.XLeveling") as XLeveling;
        Skill survival = xLeveling?.GetSkill("survival");
        Ability camelHump = new Ability("camelhump", BtCore.Modid+":ability-camelhump", BtCore.Modid+":abilitydesc-camelhump", 1, 3, new int[] { 500, 1000, 1500 });
        camelHump.OnPlayerAbilityTierChanged += OnCamelHump;
        survival?.AddAbility(camelHump);
        Ability elephantBladder = new Ability("elephantbladder", BtCore.Modid+":ability-elephantbladder", BtCore.Modid+":abilitydesc-elephantbladder", 1, 2, new int[] { 750, 1500 });
        elephantBladder.OnPlayerAbilityTierChanged += OnElephantBladder;
        survival?.AddAbility(elephantBladder);
    }
    
    public static void OnCamelHump(PlayerAbility playerAbility, int oldTier)
    {
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
        float num = (BtCore.ConfigServer.MaxHydration + playerAbility.Value(0)) / behavior.MaxHydration;
        behavior.MaxHydration = (BtCore.ConfigServer.MaxHydration + playerAbility.Value(0));
        behavior.Euhydration *= num;
        behavior.Hydration *= num;
        behavior.UpdateThirstBoosts();
    }
    
    public static void OnElephantBladder(PlayerAbility playerAbility, int oldTier)
    {
        IPlayer player = playerAbility.PlayerSkill.PlayerSkillSet.Player;
        if (player == null)
            return;
        EnumAppSide? side = player.Entity?.Api.Side;
        EnumAppSide enumAppSide = EnumAppSide.Server;
        if (!(side.GetValueOrDefault() == enumAppSide & side.HasValue))
            return;
        EntityBehaviorBladder behavior = player.Entity.GetBehavior<EntityBehaviorBladder>();
        if (behavior == null)
            return;
        behavior.Capacity = (BtCore.ConfigServer.MaxHydration + playerAbility.Value(0));
        behavior.CapacityOverload = BtCore.ConfigServer.BladderCapacityOverload*(BtCore.ConfigServer.MaxHydration + playerAbility.Value(0));
    }
    
}