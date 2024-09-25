using System;
using BalancedThirst.Systems;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace BalancedThirst.ModBehavior;

public class EntityBehaviorBladder : EntityBehavior
{
    private ITreeAttribute _bladderTree;
    private ICoreAPI _api;
    public override string PropertyName() => AttributeKey;
    private long _listenerId;
    private string AttributeKey => BtCore.Modid + ":bladder";
    
    public StatMultiplier WalkSpeedMultiplier = new StatMultiplier()
    {
        Multiplier = ConfigSystem.ConfigServer.BladderWalkSpeedDebuff,
        Centering = EnumUpOrDown.Down,
        Curve = EnumBuffCurve.Linear,
        LowerHalfCurve = EnumBuffCurve.Linear,
        Inverted = false
    }; 

    public float CapacityOverload
    {
        get => this._bladderTree?.GetFloat("capacityoverload") ?? ConfigSystem.ConfigServer.BladderCapacityOverload*ConfigSystem.ConfigServer.MaxHydration;
        set
        {
            this._bladderTree?.SetFloat("capacityoverload", value);
            this.entity.WatchedAttributes.MarkPathDirty(AttributeKey);
        }
    }
    
    public float Capacity
    {
        get => this._bladderTree?.GetFloat("capacity") ?? ConfigSystem.ConfigServer.MaxHydration;
        set
        {
            this._bladderTree?.SetFloat("capacity", value);
            this.entity.WatchedAttributes.MarkPathDirty(AttributeKey);
        }
    }

    public float CurrentLevel
    {
        get => this._bladderTree?.GetFloat("currentlevel") ?? 0;
        set
        {
            this._bladderTree?.SetFloat("currentlevel", value);
            this.entity.WatchedAttributes.MarkPathDirty(AttributeKey);
        }
    }
    
    public float EffectiveCapacity => Capacity + CapacityOverload;
    
    public EntityBehaviorBladder(Entity entity) : base(entity)
    {
    }

    public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
    {
        this._bladderTree = this.entity.WatchedAttributes.GetTreeAttribute(AttributeKey);
        this._api = this.entity.World.Api;

        if (this._bladderTree == null || this._bladderTree.GetFloat("capacity") == 0 || this._bladderTree.GetFloat("capacityoverload") == 0)
        {
            this.entity.WatchedAttributes.SetAttribute(AttributeKey, _bladderTree = new TreeAttribute());
            this.CurrentLevel = typeAttributes["currentlevel"].AsFloat(0);
            this.Capacity = typeAttributes["capacity"].AsFloat(ConfigSystem.ConfigServer.MaxHydration);
            this.CapacityOverload = typeAttributes["capacityoverload"].AsFloat(ConfigSystem.ConfigServer.BladderCapacityOverload*ConfigSystem.ConfigServer.MaxHydration);
        }
        this._listenerId = this.entity.World.RegisterGameTickListener(this.SlowTick, 500);
    }
    
    public void ReceiveFluid(float fluidAmount)
    {
        this._api.Event.PushEvent(EventIds.BladderReceiveFluid, new FloatAttribute(fluidAmount));
        this.CurrentLevel = Math.Clamp(this.CurrentLevel + fluidAmount, 0.0f, EffectiveCapacity);
    }
    
    public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
    {
        if (damageSource.Type != EnumDamageType.Heal || damageSource.Source != EnumDamageSource.Revive)
            return;
        this.Capacity = 0f;
        this.CapacityOverload = 0f;
    }
    
    public bool Drain(float multiplier = 1)
    {
        float currentLevel = this.CurrentLevel;
        if (currentLevel < 0.0) return false;
        float newLevel = Math.Clamp(currentLevel - multiplier * 10f, 0.0f, EffectiveCapacity);
        this.CurrentLevel = newLevel;
        return newLevel < currentLevel;
    }
    
    public static void PlayPeeSound(EntityAgent byEntity, int soundRepeats = 1)
    {
        IPlayer dualCallByPlayer = null;
        if (byEntity is EntityPlayer)
            dualCallByPlayer = byEntity.World.PlayerByUid(((EntityPlayer) byEntity).PlayerUID);
        byEntity.World.PlaySoundAt(new AssetLocation("sounds/effect/watering"), byEntity, dualCallByPlayer, true, 16, 0.2f);
        soundRepeats--;
        if (soundRepeats <= 0)
            return;
        byEntity.World.RegisterCallback(_ => PlayPeeSound(byEntity, soundRepeats), 300);
    }
    
    private void SlowTick(float dt)
    { 
        if (this.entity is EntityPlayer player &&
            player.World.PlayerByUid(player.PlayerUID).WorldData.CurrentGameMode ==
            EnumGameMode.Creative)
            return;
        if (!IsOverloaded() || !ConfigSystem.ConfigServer.EnableBladder)
        {
            this.entity.Stats.Remove("walkspeed", "bladderfull");
        }
        else
        {
            WalkSpeedMultiplier.Multiplier = ConfigSystem.ConfigServer.BladderWalkSpeedDebuff;
            this.entity.Stats.Set("walkspeed", "bladderfull", WalkSpeedMultiplier.CalcModifier((CurrentLevel - Capacity)/CapacityOverload));
        }
    }
    
    private bool IsOverloaded()
    {
        return CurrentLevel > Capacity;
    }
    
}