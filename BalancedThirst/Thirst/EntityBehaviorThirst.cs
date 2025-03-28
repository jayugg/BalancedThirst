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
    private readonly EntityAgent _entityAgent;
    private float _thirstCounter;
    private int _sprintCounter;
    private long _listenerId;
    private long _lastMoveMs;
    private ICoreAPI _api;
    private readonly AssetLocation _vomitSound = new("sounds/player/hurt1");
    public float GetThirstSpeedModifier => ConfigSystem.ConfigServer.ThirstSpeedModifier == 0 ? GlobalConstants.HungerSpeedModifier : ConfigSystem.ConfigServer.ThirstSpeedModifier;

    public override string PropertyName() => AttributeKey;
    private string AttributeKey => BtCore.Modid + ":thirst";

    public float HydrationLossDelay
    {
      get => _thirstTree?.GetFloat("hydrationlossdelay") ?? 0f;
      set
      {
        _thirstTree?.SetFloat("hydrationlossdelay", value);
        entity.WatchedAttributes.MarkPathDirty(AttributeKey);
      }
    }

    public float Hydration
    {
      get => Math.Min(_thirstTree?.GetFloat("currenthydration") ?? MaxHydration, MaxHydration);
      set
      {
        _thirstTree?.SetFloat("currenthydration", Math.Min(value, MaxHydration));
        entity.WatchedAttributes.MarkPathDirty(AttributeKey);
      }
    }
    
    public float MaxHydrationModifier
    {
      get => _thirstTree?.GetFloat("maxhydrationmodifier") ?? 1;
      set
      {
        _thirstTree?.SetFloat("maxhydrationmodifier", value);
        entity.WatchedAttributes.MarkPathDirty(AttributeKey);
      }
    }

    public float MaxHydration {
      get
      {
        var maxHydration = (float) Math.Round(MaxHydrationModifier*ConfigSystem.ConfigServer.MaxHydration);
        _thirstTree?.SetFloat("maxhydration", maxHydration);
        entity.WatchedAttributes.MarkPathDirty(AttributeKey);
        return maxHydration;
      }
    }

    public float Euhydration
    {
      get => _thirstTree?.GetFloat("euhydration") ?? 0f;
      set
      {
        _thirstTree?.SetFloat("euhydration", value);
        entity.WatchedAttributes.MarkPathDirty(AttributeKey);
      }
    }
    
    public float Dehydration
    {
      get => _thirstTree?.GetFloat("dehydration") ?? 0;
      set
      {
        _thirstTree?.SetFloat("dehydration", value);
        entity.WatchedAttributes.MarkPathDirty(AttributeKey);
      }
    }

    public EntityBehaviorThirst(Entity entity)
      : base(entity)
    {
      _entityAgent = entity as EntityAgent;
    }

    public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
    {
      _thirstTree = entity.WatchedAttributes.GetTreeAttribute(AttributeKey);
      _api = entity.World.Api;
      if (_thirstTree == null || _thirstTree.GetFloat("maxhydration") == 0)
      {
        entity.WatchedAttributes.SetAttribute(AttributeKey, _thirstTree = new TreeAttribute());
        MaxHydrationModifier = typeAttributes["maxhydrationmodifier"].AsFloat(1);
        Hydration = Math.Min(typeAttributes["currenthydration"].AsFloat(MaxHydration), MaxHydration);
        HydrationLossDelay = 0f;
        Euhydration = 0f;
        Dehydration = 0f;
      }
      _listenerId = entity.World.RegisterGameTickListener(SlowTick, 6000);
      entity.Stats.Register(BtCore.Modid+":thirstrate");
      UpdateThirstBoosts();
    }

    public override void OnEntityDespawn(EntityDespawnData despawn)
    {
      base.OnEntityDespawn(despawn);
      entity.World.UnregisterGameTickListener(_listenerId);
    }

    public override void DidAttack(
      DamageSource source,
      EntityAgent targetEntity,
      ref EnumHandling handled)
    {
      ConsumeHydration(3f);
    }

    public virtual void ConsumeHydration(float amount) => ReduceHydration(amount / 10f);

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
        default:
          throw new ArgumentOutOfRangeException(nameof(purityLevel), purityLevel, null);
      }

      return VomitChance(purity);
    }
    
    public void ReceiveHydration(HydrationProperties hydrationProperties)
    {
      if (_api.Side == EnumAppSide.Client) return;
      var maxHydration = MaxHydration;
      var isHydrationMaxed = Hydration >= maxHydration;
      Hydration = Math.Clamp(Hydration + hydrationProperties.Hydration, 0, maxHydration);
      if (!isHydrationMaxed) HydrationLossDelay = Math.Max(HydrationLossDelay, hydrationProperties.HydrationLossDelay);
      if (ConfigSystem.ConfigServer.EnableDehydration)
      {
        Dehydration += 0.01f * hydrationProperties.Dehydration;
        Dehydration = Math.Clamp(Dehydration, 0, 9);
      }
      entity.WatchedAttributes.SetFloat("drankSaltwater", hydrationProperties.Dehydration);
      if (entity.World.Rand.NextDouble() < VomitChance(hydrationProperties.Purity))
      {
        entity.WatchedAttributes.SetFloat("intoxication", 1.0f);
        entity.World.RegisterCallback(_ => Vomit(), 2000);
      }
      if (hydrationProperties.Scalding) entity.ReceiveDamage(new DamageSource() {Type = EnumDamageType.Heat, Source = EnumDamageSource.Internal}, 3);
      UpdateThirstStatBoosts();
      if (!isHydrationMaxed) UpdateThirstHealthBoost(hydrationProperties);
    }

    public override void OnGameTick(float deltaTime)
    {
      if (entity is EntityPlayer player)
      {
        var tree = player.WatchedAttributes.GetTreeAttribute(AttributeKey);
        var currentGameMode = player.World.PlayerByUid(player.PlayerUID).WorldData.CurrentGameMode;
        if (currentGameMode is EnumGameMode.Creative or EnumGameMode.Spectator)
          return;
        if (player.Controls.TriesToMove || player.Controls.Jump || player.Controls.LeftMouseDown || player.Controls.RightMouseDown)
          _lastMoveMs = entity.World.ElapsedMilliseconds;
      }
      if (_entityAgent != null && _entityAgent.Controls.Sprint)
        ++_sprintCounter;
      _thirstCounter += deltaTime;
      if ( _thirstCounter <= 10.0)
        return;
      var num1 = entity.World.ElapsedMilliseconds - _lastMoveMs > 3000L ? 1 : 0;
      var num2 = entity.Api.World.Calendar.SpeedOfTime * entity.Api.World.Calendar.CalendarSpeedMul;
      var num3 = GlobalConstants.HungerSpeedModifier / 30f;
      if (num1 != 0)
        num3 /= 4f;
      ReduceHydration(num3 * (float) (1.2000000476837158 * (8.0 + _sprintCounter / 15.0) / 10.0) * entity.Stats.GetBlended("thirstrate") * num2);
      _thirstCounter = 0.0f;
      _sprintCounter = 0;
    }

    private void ReduceHydration(float satLossMultiplier)
    {
      var flag = false;
      satLossMultiplier *= GetThirstSpeedModifier;
      if (HydrationLossDelay > 0.0)
      {
        HydrationLossDelay -= 10f * satLossMultiplier;
        flag = true;
      }
      else if (Hydration < 0.6*MaxHydration)
        Euhydration = Math.Max(0.0f, Euhydration - satLossMultiplier); // 10 times less
      if (flag)
      {
        _thirstCounter -= 10f;
        return;
      }
      var hydration = Hydration;
      if (hydration > 0.0)
      {
        Hydration = Math.Max(0.0f, hydration - satLossMultiplier * 10f);
        _sprintCounter = 0;
      }
      UpdateThirstBoosts();
    }
    
    public void UpdateThirstBoosts()
    {
      UpdateThirstStatBoosts();
      UpdateThirstHealthBoost();
    }

    private void UpdateThirstStatBoosts()
    {
      foreach (var stat in ConfigSystem.ConfigServer.ThirstStatMultipliers.Keys)
      {
        var multiplier = ConfigSystem.ConfigServer.ThirstStatMultipliers[stat];
        if (multiplier.Multiplier == 0) continue;
        var multiplierVal = ConfigSystem.ConfigServer.ThirstStatMultipliers[stat].CalcModifier(Hydration/MaxHydration);
        entity.Stats?.Set(stat, BtCore.Modid + ":thirsty", multiplierVal);
      }
      entity?.WatchedAttributes?.MarkPathDirty("stats");
    }

    private void UpdateThirstHealthBoost()
    {
      var behavior = entity.GetBehavior<EntityBehaviorHealth>();
      behavior.SetMaxHealthModifiers(BtCore.Modid+"thirstHealthMod", Euhydration / MaxHydration);
      behavior.MarkDirty();
    }

    private void UpdateThirstHealthBoost(HydrationProperties hydrationProperties)
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
      Euhydration = Math.Clamp(Euhydration + mul * Math.Max(hydrationProperties.Hydration, 0), 0, MaxHydration);
      UpdateThirstHealthBoost();
    }

    private void SlowTick(float dt)
    {
      if (entity is EntityPlayer player &&
          player.World.PlayerByUid(player.PlayerUID).WorldData.CurrentGameMode == EnumGameMode.Creative)
        return;
      
      if (ConfigSystem.ConfigServer.EnableDehydration && Dehydration > 0)
      {
        entity.Stats.Set(BtCore.Modid + ":thirstrate", "dehydration", Dehydration);
        Dehydration = Math.Max(0, Dehydration - 0.02f*Hydration/MaxHydration * (Math.Abs(Hydration - MaxHydration) < 1e-4 ? 5 : 1));
      }
      else
      {
        entity.Stats.Remove(BtCore.Modid + ":thirstrate", "dehydration");
      }
      
      var climate = entity.World.BlockAccessor.GetClimateAt(entity.Pos.AsBlockPos,
        EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, entity.World.Calendar.TotalDays);
      
      if (climate.Temperature > ConfigSystem.ConfigServer.HotTemperatureThreshold)
      {
        var temperatureDifference = climate.Temperature - ConfigSystem.ConfigServer.HotTemperatureThreshold;
        if (BtCore.IsHoDLoaded)
        {
          var coolingFactor = entity.WatchedAttributes.GetFloat("currentCoolingHot");
          temperatureDifference -= coolingFactor;
        }

        if (entity.HasBehavior<EntityBehaviorBodyTemperature>())
        {
          var behavior = entity.GetBehavior<EntityBehaviorBodyTemperature>();
          var clothingPenalty = (float) (behavior.GetField<float>("clothingBonus") * 1/ (1 + Math.Exp(-temperatureDifference)));
          var wetnessBonus = (float) Math.Max(0.0, behavior.Wetness - 0.1) * 15f;
          temperatureDifference += clothingPenalty - wetnessBonus;
        }
        
        var temperatureFactor = 0.01f*temperatureDifference*ConfigSystem.ConfigServer.ThirstRatePerDegrees/(1 + (float)Math.Exp(-temperatureDifference));
        var thirstRateUpdate = entity.World.Api.ModLoader.GetModSystem<RoomRegistry>()
          .GetRoomForPosition(entity.Pos.AsBlockPos)
          .ExitCount == 0
          ? 0.0f
          : temperatureFactor;
        entity.Stats.Set(BtCore.Modid + ":thirstrate", "resistheat", thirstRateUpdate);
      }
      else
      {
        entity.Stats.Remove(BtCore.Modid + ":thirstrate", "resistheat");
      }
      
      if (Hydration > 0.0)
        return;
      if (ConfigSystem.ConfigServer.ThirstKills) 
      {
        entity.ReceiveDamage(new DamageSource()
        { Source = EnumDamageSource.Internal,
          Type = EnumDamageType.Hunger }, 0.125f);
      }
      _sprintCounter = 0;
      UpdateThirstBoosts();
    }

    public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
    {
      if (damageSource.Type != EnumDamageType.Heal || damageSource.Source != EnumDamageSource.Revive)
        return;
      HydrationLossDelay = 60f;
      Hydration = MaxHydration / 2f;
      Euhydration /= 2f;
    }

    private void Vomit()
    {
      Hydration *= ConfigSystem.ConfigServer.VomitHydrationMultiplier;
      HydrationLossDelay = 0;
      Euhydration *= ConfigSystem.ConfigServer.VomitEuhydrationMultiplier;
      var satLoss = 0f;
      if (entity.HasBehavior<EntityBehaviorHunger>())
      {
        var bh = entity.GetBehavior<EntityBehaviorHunger>();
        bh.Saturation = 0.5f * bh.Saturation;
        satLoss = bh.Saturation;
      }
      entity.World.PlaySoundAt(_vomitSound, entity.Pos.X, entity.Pos.Y, entity.Pos.Z, range: 10f);
      entity.World.RegisterCallback(_ => entity.WatchedAttributes.SetFloat("intoxication", 0.0f), 5000);
      var vomitStack = new ItemStack(entity.World.GetItem(new AssetLocation("balancedthirst:vomit")), (int) satLoss / 10);
      if (vomitStack.StackSize > 10) entity.World.SpawnItemEntity(vomitStack, entity.Pos.AsBlockPos.ToVec3d().Add(0.5, 0.1, 0.5));
      entity.World.SpawnCubeParticles(entity.Pos.AheadCopy(0.25).XYZ.Add(0.0, entity.SelectionBox.Y2 / 2.0, 0.0), vomitStack, 0.75f, 100, 0.45f);
    }
  }
  
}
