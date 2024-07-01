using System;
using BalancedThirst.Config;
using BalancedThirst.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BalancedThirst.Thirst
{
  public class EntityBehaviorThirst : EntityBehavior
  {
    private ITreeAttribute _thirstTree;
    private EntityAgent _entityAgent;
    private float _thirstCounter;
    private int _sprintCounter;
    private long _listenerId;
    private long _lastMoveMs;
    private ICoreAPI _api;
    private float _detoxCounter;
    private readonly AssetLocation _vomitSound = new("sounds/player/hurt1");
    
    public override string PropertyName() => AttributeKey;
    private string AttributeKey => BtCore.Modid + ":thirst";

    public float HydrationLossDelay
    {
      get => this._thirstTree?.GetFloat("hydrationlossdelay") ?? 180;
      set
      {
        this._thirstTree?.SetFloat("hydrationlossdelay", value);
        this.entity.WatchedAttributes.MarkPathDirty(AttributeKey);
      }
    }

    public float Hydration
    {
      get => this._thirstTree?.GetFloat("currenthydration") ?? BtCore.ConfigServer.MaxHydration;
      set
      {
        this._thirstTree?.SetFloat("currenthydration", value);
        this.entity.WatchedAttributes.MarkPathDirty(AttributeKey);
      }
    }

    public float MaxHydration
    {
      get => this._thirstTree?.GetFloat("maxhydration") ?? BtCore.ConfigServer.MaxHydration;
      set
      {
        this._thirstTree?.SetFloat("maxhydration", value);
        this.entity.WatchedAttributes.MarkPathDirty(AttributeKey);
      }
    }
    
    public float Euhydration
    {
      get => this._thirstTree?.GetFloat("euhydration") ?? 0f;
      set
      {
        this._thirstTree?.SetFloat("euhydration", value);
        this.entity.WatchedAttributes.MarkPathDirty(AttributeKey);
      }
    }

    public EntityBehaviorThirst(Entity entity)
      : base(entity)
    {
      this._entityAgent = entity as EntityAgent;
    }

    public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
    {
      this._thirstTree = this.entity.WatchedAttributes.GetTreeAttribute(AttributeKey);
      this._api = this.entity.World.Api;
      if (this._thirstTree == null || this._thirstTree.GetFloat("maxhydration") == 0)
      {
        this.entity.WatchedAttributes.SetAttribute(AttributeKey, _thirstTree = new TreeAttribute());
        this.Hydration = typeAttributes["currenthydration"].AsFloat(BtCore.ConfigServer.MaxHydration);
        this.MaxHydration = typeAttributes["maxhydration"].AsFloat(BtCore.ConfigServer.MaxHydration);
        this.HydrationLossDelay = 180.0f;
        this.Euhydration = 0f;
      }
      this._listenerId = this.entity.World.RegisterGameTickListener(this.SlowTick, 6000);
      entity.Stats.Register(BtCore.Modid+":thirstrate");
      this.UpdateThirstBoosts();
    }

    public override void OnEntityDespawn(EntityDespawnData despawn)
    {
      base.OnEntityDespawn(despawn);
      this.entity.World.UnregisterGameTickListener(this._listenerId);
    }

    public override void DidAttack(
      DamageSource source,
      EntityAgent targetEntity,
      ref EnumHandling handled)
    {
      this.ConsumeHydration(3f);
    }

    public virtual void ConsumeHydration(float amount) => this.ReduceHydration(amount / 10f);

    public double VomitChance(double purity) => Math.Exp(-8.445*purity);
    
    public double VomitChance(EnumPurityLevel purityLevel)
    {
      double purity;

      switch (purityLevel)
      {
        case EnumPurityLevel.Pure:
          purity = BtCore.ConfigServer.PurePurityLevel;
          break;
        case EnumPurityLevel.Filtered:
          purity = BtCore.ConfigServer.FilteredPurityLevel;
          break;
        case EnumPurityLevel.Potable:
          purity = BtCore.ConfigServer.PotablePurityLevel;
          break;
        case EnumPurityLevel.Okay:
          purity = BtCore.ConfigServer.OkayPurityLevel;
          break;
        case EnumPurityLevel.Stagnant:
          purity = BtCore.ConfigServer.StagnantPurityLevel;
          break;
        case EnumPurityLevel.Yuck:
          purity = BtCore.ConfigServer.RotPurityLevel;
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(purityLevel), purityLevel, null);
      }

      return VomitChance(purity);
    }
    
    public void ReceiveHydration(HydrationProperties hydrationProperties)
    {
      if (_api.Side == EnumAppSide.Client) return;
      float maxHydration = this.MaxHydration;
      bool isHydrationMaxed = this.Hydration >= maxHydration;
      this.Hydration = Math.Clamp(this.Hydration + hydrationProperties.Hydration, 0, maxHydration);
      if (!isHydrationMaxed) this.HydrationLossDelay = Math.Max(this.HydrationLossDelay, hydrationProperties.HydrationLossDelay);
      if (entity.World.Rand.NextDouble() < VomitChance(hydrationProperties.Purity))
      {
        entity.WatchedAttributes.SetFloat("intoxication", 1.0f);
        entity.World.RegisterCallback(dt => Vomit(), 2000);
      }
      if (hydrationProperties.Scalding) entity.ReceiveDamage(new DamageSource() {Type = EnumDamageType.Heat, Source = EnumDamageSource.Internal}, 3);
      this.UpdateThirstStatBoosts();
      if (!isHydrationMaxed) this.UpdateThirstHealthBoost(hydrationProperties);
    }

    public override void OnGameTick(float deltaTime)
    {
      if (this.entity is EntityPlayer player)
      {
        if (entity.World.Side == EnumAppSide.Server)
        {
        }
        var tree = player.WatchedAttributes.GetTreeAttribute(AttributeKey);
        
        EnumGameMode currentGameMode = player.World.PlayerByUid(player.PlayerUID).WorldData.CurrentGameMode;
        this.Detox(deltaTime);
        if (currentGameMode == EnumGameMode.Creative || currentGameMode == EnumGameMode.Spectator)
          return;
        if (player.Controls.TriesToMove || player.Controls.Jump || player.Controls.LeftMouseDown || player.Controls.RightMouseDown)
          this._lastMoveMs = this.entity.World.ElapsedMilliseconds;
      }
      if (this._entityAgent != null && this._entityAgent.Controls.Sprint)
        ++this._sprintCounter;
      this._thirstCounter += deltaTime;
      if ( this._thirstCounter <= 10.0)
        return;
      int num1 = this.entity.World.ElapsedMilliseconds - this._lastMoveMs > 3000L ? 1 : 0;
      float num2 = this.entity.Api.World.Calendar.SpeedOfTime * this.entity.Api.World.Calendar.CalendarSpeedMul;
      float num3 = GlobalConstants.HungerSpeedModifier / 30f;
      if (num1 != 0)
        num3 /= 4f;
      this.ReduceHydration(num3 * (float) (1.2000000476837158 * (8.0 + this._sprintCounter / 15.0) / 10.0) * this.entity.Stats.GetBlended("thirstrate") * num2);
      this._thirstCounter = 0.0f;
      this._sprintCounter = 0;
      this.Detox(deltaTime);
    }

    private void Detox(float dt)
    {
      this._detoxCounter += dt;
      if (this._detoxCounter <= 1.0)
        return;
      float num = this.entity.WatchedAttributes.GetFloat("intoxication");
      if (num > 0.0)
        this.entity.WatchedAttributes.SetFloat("intoxication", Math.Max(0.0f, num - 0.005f));
      this._detoxCounter = 0.0f;
    }

    private bool ReduceHydration(float satLossMultiplier)
    {
      bool flag = false;
      satLossMultiplier *= BtCore.ConfigServer.ThirstSpeedModifier == 0 ? GlobalConstants.HungerSpeedModifier : BtCore.ConfigServer.ThirstSpeedModifier;
      if (this.HydrationLossDelay > 0.0)
      {
        this.HydrationLossDelay -= 10f * satLossMultiplier;
        flag = true;
      }
      else if (this.Hydration < 0.6*this.MaxHydration)
        this.Euhydration = Math.Max(0.0f, this.Euhydration - satLossMultiplier); // 10 times less
      this.UpdateThirstBoosts();
      if (flag)
      {
        this._thirstCounter -= 10f;
        return true;
      }
      float hydration = this.Hydration;
      if (hydration > 0.0)
      {
        this.Hydration = Math.Max(0.0f, hydration - satLossMultiplier * 10f);
        this._sprintCounter = 0;
      }
      entity.ReceiveCapacity(hydration - this.Hydration);
      return false;
    }
    
    public void UpdateThirstBoosts()
    {
      this.UpdateThirstStatBoosts();
      this.UpdateThirstHealthBoost();
    }

    private void UpdateThirstStatBoosts()
    {
      foreach (var stat in BtCore.ConfigServer.ThirstStatMultipliers.Keys)
      {
        StatMultiplier multiplier = BtCore.ConfigServer.ThirstStatMultipliers[stat];
        if (multiplier.Multiplier == 0) continue;
        var multiplierVal = BtCore.ConfigServer.ThirstStatMultipliers[stat].CalcModifier(Hydration/MaxHydration);
        this.entity.Stats.Set(stat, BtCore.Modid + ":thirsty", multiplierVal);
      }
      this.entity.WatchedAttributes.MarkPathDirty("stats");
    }
    
    public void UpdateThirstHealthBoost()
    {
      EntityBehaviorHealth behavior = this.entity.GetBehavior<EntityBehaviorHealth>();
      behavior.MaxHealthModifiers[BtCore.Modid+"thirstHealthMod"] = Euhydration / this.MaxHydration;
      behavior.MarkDirty();
    }
    
    public void UpdateThirstHealthBoost(HydrationProperties hydrationProperties)
    {
      var mul = hydrationProperties.Salty ? 0f : 1f ;
      mul = hydrationProperties.Purity switch
      {
        EnumPurityLevel.Pure => 1.5f,
        EnumPurityLevel.Filtered => 1.3f,
        EnumPurityLevel.Potable => 1.2f,
        EnumPurityLevel.Okay => 1,
        EnumPurityLevel.Stagnant => 0.5f,
        EnumPurityLevel.Yuck => 0,
        _ => 1
      };
      mul *= hydrationProperties.EuhydrationWeight; // 10 times less by default
      this.Euhydration = Math.Clamp(this.Euhydration + mul * Math.Max(hydrationProperties.Hydration, 0), 0, this.MaxHydration);
      this.UpdateThirstHealthBoost();
    }

    private void SlowTick(float dt)
    {
      if (BtCore.IsHoDLoaded)
      {
        float coolingFactor = entity.WatchedAttributes.GetFloat("currentCoolingHot", 0f);
        //BtCore.Logger.Warning("Cooling factor: " + coolingFactor);
        var climate = this.entity.World.BlockAccessor.GetClimateAt(this.entity.Pos.AsBlockPos,
          EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, this.entity.World.Calendar.TotalDays);
        float coolingEffect = coolingFactor * (1f / (1f + (float)Math.Exp(-0.5f * (climate.Temperature - BtCore.ConfigServer.HotTemperatureThreshold))));
        //BtCore.Logger.Warning("Cooling effect: " + coolingEffect);
        var thirstRate = this.entity.Stats.GetBlended(BtCore.Modid + ":thirstrate");
        this.entity.Stats.Set(BtCore.Modid + ":thirstrate", "HoD:cooling", -Math.Min(BtCore.ConfigServer.HoDClothingCoolingMultiplier*coolingEffect, thirstRate - 1));
      }
      else
      {
        this.entity.Stats.Remove(BtCore.Modid + ":thirstrate", "HoD:cooling");
      }

      if (this.entity is EntityPlayer &&
          this.entity.World.PlayerByUid(((EntityPlayer)this.entity).PlayerUID).WorldData.CurrentGameMode ==
          EnumGameMode.Creative)
        return;
      float temperature = this.entity.World.BlockAccessor.GetClimateAt(this.entity.Pos.AsBlockPos,
        EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, this.entity.World.Calendar.TotalDays).Temperature;
      if (temperature <= BtCore.ConfigServer.HotTemperatureThreshold)
      {
        this.entity.Stats.Remove(BtCore.Modid + ":thirstrate", "resistheat");
      }
      else
      {
        float num = GameMath.Clamp(temperature - 30, 0.0f, 40f);
        this.entity.Stats.Set(BtCore.Modid + ":thirstrate", "resistheat",
          this.entity.World.Api.ModLoader.GetModSystem<RoomRegistry>().GetRoomForPosition(this.entity.Pos.AsBlockPos)
            .ExitCount == 0
            ? 0.0f
            : num / 40f, true);
      }

      if (this.Hydration > 0.0)
        return;
      if (BtCore.ConfigServer.ThirstKills) 
      {
        this.entity.ReceiveDamage(new DamageSource()
        { Source = EnumDamageSource.Internal,
          Type = EnumDamageType.Hunger }, 0.125f);
      }
      this._sprintCounter = 0;
      UpdateThirstBoosts();
    }

    public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
    {
      if (damageSource.Type != EnumDamageType.Heal || damageSource.Source != EnumDamageSource.Revive)
        return;
      this.HydrationLossDelay = this.MaxHydration / 2f;
      this.Hydration = this.MaxHydration / 2f;
      this.Euhydration /= 2f;
    }

    public void Vomit()
    {
      Hydration *= BtCore.ConfigServer.VomitHydrationMultiplier;
      HydrationLossDelay = 0;
      Euhydration *= BtCore.ConfigServer.VomitEuhydrationMultiplier;
      if (entity.HasBehavior<EntityBehaviorHunger>())
      {
        var bh = entity.GetBehavior<EntityBehaviorHunger>();
        bh.Saturation = 0.5f * bh.Saturation;
      }
      entity.World.PlaySoundAt(this._vomitSound, entity.Pos.X, entity.Pos.Y, entity.Pos.Z, range: 10f);
      entity.World.RegisterCallback(dt => entity.WatchedAttributes.SetFloat("intoxication", 0.0f), 5000);
    }
  }
  
}
