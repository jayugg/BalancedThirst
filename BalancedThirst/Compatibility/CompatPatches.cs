using System;
using System.Reflection;
using BalancedThirst.XSkillsCompat;
using HarmonyLib;
using Vintagestory.API.Common;
using XLib.XLeveling;
using XSkills;

namespace BalancedThirst.Compatibility;

public class CompatPatches : ModSystem
{
    private ICoreAPI _api;
    private Harmony HarmonyInstance;

    public override double ExecuteOrder() => 1.05;
    public override bool ShouldLoad(EnumAppSide forSide)
    {
        return BtCore.IsXskillsLoaded;
    }

    public override void Start(ICoreAPI api)
    {
        this._api = api;
        HarmonyInstance = new Harmony(Mod.Info.ModID);
        
        ConstructorInfo originalConstructor = typeof(Survival).GetConstructor(new Type[] { typeof(ICoreAPI) });
        
        MethodInfo postfix = typeof(Survival_Constructor_Patch).GetMethod(nameof(Survival_Constructor_Patch.Postfix));
        
        HarmonyInstance.Patch(originalConstructor, postfix: postfix);
        //HarmonyInstance.Patch(typeof(Skill).GetMethod(nameof(Skill.FindAbility)),
        //    postfix: typeof(Skill_FindAbility_Patch).GetMethod(nameof(Skill_FindAbility_Patch.Postfix)));
    }

    public override void Dispose() {
        HarmonyInstance?.UnpatchAll(Mod.Info.ModID);
    }
    
}