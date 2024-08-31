using System;
using BalancedThirst.Systems;
using BalancedThirst.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
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
    public float GetThirstSpeedModifier => ConfigSystem.ConfigServer.ThirstSpeedModifier == 0 ? GlobalConstants.HungerSpeedModifier : ConfigSystem.ConfigServer.ThirstSpeedModifier;

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
      get => this._thirstTree?.GetFloat("currenthydration") ?? ConfigSystem.ConfigServer.MaxHydration;
      set
      {
        this._thirstTree?.SetFloat("currenthydration", value);
        this.entity.WatchedAttributes.MarkPathDirty(AttributeKey);
      }
    }

    public float MaxHydration
    {
      get => this._thirstTree?.GetFloat("maxhydration") ?? ConfigSystem.ConfigServer.MaxHydration;
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
    
    public float Dehydration
    {
      get => this._thirstTree?.GetFloat("dehydration") ?? 0;
      set
      {
        this._thirstTree?.SetFloat("dehydration", value);
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
        this.Hydration = typeAttributes["currenthydration"].AsFloat(ConfigSystem.ConfigServer.MaxHydration);
        this.MaxHydration = typeAttributes["maxhydration"].AsFloat(ConfigSystem.ConfigServer.MaxHydration);
        this.HydrationLossDelay = 180.0f;
        this.Euhydration = 0f;
        this.Dehydration = 0f;
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
          purity = ConfigSystem.ConfigServer.PurePurityLevel;
          break;
        case EnumPurityLevel.Filtered:
          purity = ConfigSystem.ConfigServer.FilteredPurityLevel;
          break;
        case EnumPurityLevel.Potable:
          purity = ConfigSystem.ConfigServer.PotablePurityLevel;
          break;
        case EnumPurityLevel.Okay:
          purity = ConfigSystem.ConfigServer.OkayPurityLevel;
          break;
        case EnumPurityLevel.Stagnant:
          purity = ConfigSystem.ConfigServer.StagnantPurityLevel;
          break;
        case EnumPurityLevel.Yuck:
          purity = ConfigSystem.ConfigServer.RotPurityLevel;
          break;
        case EnumPurityLevel.Distilled:
          return 0;
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
      this.Dehydration += 0.01f*hydrationProperties.Dehydration;
      this.Dehydration = (float) Math.Clamp(this.Dehydration, 0, 9);
      entity.WatchedAttributes.SetFloat("drankSaltwater", hydrationProperties.Dehydration);
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
        var tree = player.WatchedAttributes.GetTreeAttribute(AttributeKey);
        EnumGameMode currentGameMode = player.World.PlayerByUid(player.PlayerUID).WorldData.CurrentGameMode;
        if (currentGameMode is EnumGameMode.Creative or EnumGameMode.Spectator)
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
    }

    private bool ReduceHydration(float satLossMultiplier)
    {
      bool flag = false;
      satLossMultiplier *= GetThirstSpeedModifier;
      if (this.HydrationLossDelay > 0.0)
      {
        this.HydrationLossDelay -= 10f * satLossMultiplier;
        flag = true;
      }
      else if (this.Hydration < 0.6*this.MaxHydration)
        this.Euhydration = Math.Max(0.0f, this.Euhydration - satLossMultiplier); // 10 times less
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
      this.UpdateThirstBoosts();
      return false;
    }
    
    public void UpdateThirstBoosts()
    {
      this.UpdateThirstStatBoosts();
      this.UpdateThirstHealthBoost();
    }

    private void UpdateThirstStatBoosts()
    {
      foreach (var stat in ConfigSystem.ConfigServer.ThirstStatMultipliers.Keys)
      {
        StatMultiplier multiplier = ConfigSystem.ConfigServer.ThirstStatMultipliers[stat];
        if (multiplier.Multiplier == 0) continue;
        var multiplierVal = ConfigSystem.ConfigServer.ThirstStatMultipliers[stat].CalcModifier(Hydration/MaxHydration);
        this.entity.Stats?.Set(stat, BtCore.Modid + ":thirsty", multiplierVal);
      }
      this.entity?.WatchedAttributes?.MarkPathDirty("stats");
    }
    
    public void UpdateThirstHealthBoost()
    {
      EntityBehaviorHealth behavior = this.entity.GetBehavior<EntityBehaviorHealth>();
      behavior.MaxHealthModifiers[BtCore.Modid+"thirstHealthMod"] = Euhydration / this.MaxHydration;
      behavior.MarkDirty();
    }
    
    public void UpdateThirstHealthBoost(HydrationProperties hydrationProperties)
    {
      var mul = hydrationProperties.Purity switch
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
      if (this.entity is EntityPlayer player &&
          player.World.PlayerByUid(player.PlayerUID).WorldData.CurrentGameMode == EnumGameMode.Creative)
        return;
      
      if (this.Dehydration > 0)
      {
        this.entity.Stats.Set(BtCore.Modid + ":thirstrate", "dehydration", Dehydration);
        Dehydration = Math.Max(0, Dehydration - 0.02f*Hydration/MaxHydration * (Math.Abs(Hydration - MaxHydration) < 1e-4 ? 5 : 1));
      }
      else
      {
        this.entity.Stats.Remove(BtCore.Modid + ":thirstrate", "dehydration");
      }
      
      var climate = this.entity.World.BlockAccessor.GetClimateAt(this.entity.Pos.AsBlockPos,
        EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, this.entity.World.Calendar.TotalDays);
      
      if (climate.Temperature > ConfigSystem.ConfigServer.HotTemperatureThreshold)
      {
        float temperatureDifference = climate.Temperature - ConfigSystem.ConfigServer.HotTemperatureThreshold;
        if (BtCore.IsHoDLoaded)
        {
          float coolingFactor = entity.WatchedAttributes.GetFloat("currentCoolingHot", 0f);
          temperatureDifference -= coolingFactor;
        }

        if (this.entity.HasBehavior<EntityBehaviorBodyTemperature>())
        {
          var behavior = this.entity.GetBehavior<EntityBehaviorBodyTemperature>();
          var clothingPenalty = (float) (behavior.GetField<float>("clothingBonus") * 1/ (1 + Math.Exp(-temperatureDifference)));
          float wetnessBonus = (float) Math.Max(0.0, behavior.Wetness - 0.1) * 15f;
          temperatureDifference += clothingPenalty - wetnessBonus;
        }
        
        float temperatureFactor = 0.01f*temperatureDifference*ConfigSystem.ConfigServer.ThirstRatePerDegrees/(1 + (float)Math.Exp(-temperatureDifference));
        var thirstRateUpdate = this.entity.World.Api.ModLoader.GetModSystem<RoomRegistry>()
          .GetRoomForPosition(this.entity.Pos.AsBlockPos)
          .ExitCount == 0
          ? 0.0f
          : temperatureFactor;
        this.entity.Stats.Set(BtCore.Modid + ":thirstrate", "resistheat", thirstRateUpdate);
      }
      else
      {
        this.entity.Stats.Remove(BtCore.Modid + ":thirstrate", "resistheat");
      }
      
      if (this.Hydration > 0.0)
        return;
      if (ConfigSystem.ConfigServer.ThirstKills) 
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
      this.HydrationLossDelay = 60f;
      this.Hydration = this.MaxHydration / 2f;
      this.Euhydration /= 2f;
    }

    public void Vomit()
    {
      Hydration *= ConfigSystem.ConfigServer.VomitHydrationMultiplier;
      HydrationLossDelay = 0;
      Euhydration *= ConfigSystem.ConfigServer.VomitEuhydrationMultiplier;
      if (entity.HasBehavior<EntityBehaviorHunger>())
      {
        var bh = entity.GetBehavior<EntityBehaviorHunger>();
        bh.Saturation = 0.5f * bh.Saturation;
      }
      entity.World.PlaySoundAt(this._vomitSound, entity.Pos.X, entity.Pos.Y, entity.Pos.Z, range: 10f);
      entity.World.RegisterCallback(dt => entity.WatchedAttributes.SetFloat("intoxication", 0.0f), 5000);
    }

    public void ResetStats()
    {
      Hydration = MaxHydration;
      Dehydration = 0;
    }
  }
  
}
