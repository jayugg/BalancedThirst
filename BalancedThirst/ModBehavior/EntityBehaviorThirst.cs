using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BalancedThirst.ModBehavior
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
    private AssetLocation vomitSound = new AssetLocation("sounds/player/hurt1");
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
      get => this._thirstTree?.GetFloat("currenthydration") ?? 1500f;
      set
      {
        this._thirstTree?.SetFloat("currenthydration", value);
        this.entity.WatchedAttributes.MarkPathDirty(AttributeKey);
      }
    }

    public float MaxHydration
    {
      get => this._thirstTree?.GetFloat("maxhydration") ?? 1500f;
      set
      {
        this._thirstTree?.SetFloat("maxhydration", value);
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
        this.Hydration = typeAttributes["currenthydration"].AsFloat(1500f);
        this.MaxHydration = typeAttributes["maxhydration"].AsFloat(1500f);
        this.HydrationLossDelay = 180.0f;
      }
      this._listenerId = this.entity.World.RegisterGameTickListener(this.SlowTick, 6000);
      entity.Stats.Register(BtCore.Modid+":thirstrate");
      this.UpdateThirstHungerBoost();
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
    
    public void ReceiveHydration(HydrationProperties hydrationProperties)
    {
      float maxHydration = this.MaxHydration;
      bool isHydrationMaxed = this.Hydration >= maxHydration;
      this.Hydration = Math.Clamp(this.Hydration + hydrationProperties.Hydration, 0, maxHydration);
      if (!isHydrationMaxed) this.HydrationLossDelay = Math.Max(this.HydrationLossDelay, hydrationProperties.HydrationLossDelay);
      if ((hydrationProperties.Purity < 1.0 &&
          entity.World.Rand.NextDouble() < VomitChance(hydrationProperties.Purity)))
      {
        entity.WatchedAttributes.SetFloat("intoxication", 1.0f);
        entity.World.RegisterCallback(dt => Vomit(), 2000);
      }
      if (hydrationProperties.Salty)
      {
        this.entity.Stats.Set(BtCore.Modid+":thirstrate", "dranksaltwater", 100.0f);
        entity.World.RegisterCallback(dt => entity.Stats.Remove(BtCore.Modid+":thirstrate", "dranksaltwater"), 10000);
      }
      if (hydrationProperties.Scalding) entity.ReceiveDamage(new DamageSource() {Type = EnumDamageType.Heat, Source = EnumDamageSource.Internal}, 3);
      this.UpdateThirstHungerBoost();
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
      satLossMultiplier *= GlobalConstants.HungerSpeedModifier;
      if (this.HydrationLossDelay > 0.0)
      {
        this.HydrationLossDelay -= 10f * satLossMultiplier;
        flag = true;
      }
      else
        this.Hydration = Math.Max(0.0f, this.Hydration - (float) (Math.Max(0.5f, 1f / 1000f * this.Hydration) * (double) satLossMultiplier * 0.25));
      this.UpdateThirstHungerBoost();
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
      return false;
    }
    
    private static float _bParam = 0.3f; // Will be targetable by config
    private static float _cParam = 2f;
    public float HungerModifier => _bParam * (float) Math.Tanh(_cParam*(1f - 2f*Hydration/MaxHydration));

    private void UpdateThirstHungerBoost()
    {
      this.entity.Stats.Set("hungerrate", BtCore.Modid + ":thirsty", HungerModifier);
      entity.WatchedAttributes.MarkPathDirty("stats");
    }

    private void SlowTick(float dt)
    {
      if (this.entity is EntityPlayer && this.entity.World.PlayerByUid(((EntityPlayer) this.entity).PlayerUID).WorldData.CurrentGameMode == EnumGameMode.Creative)
        return;
      float temperature = this.entity.World.BlockAccessor.GetClimateAt(this.entity.Pos.AsBlockPos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, this.entity.World.Calendar.TotalDays).Temperature;
      //float bodyTemperature = entity.GetBehavior<EntityBehaviorBodyTemperature>().CurBodyTemperature = temperature;
      if (temperature <= 30.0)
      {
        this.entity.Stats.Remove(BtCore.Modid+":thirstrate", "resistheat");
      }
      else
      {
        float num = GameMath.Clamp(temperature - 30, 0.0f, 40f);
        this.entity.Stats.Set(BtCore.Modid+":thirstrate", "resistheat", this.entity.World.Api.ModLoader.GetModSystem<RoomRegistry>().GetRoomForPosition(this.entity.Pos.AsBlockPos).ExitCount == 0 ? 0.0f : num / 40f, true);
      }
      UpdateThirstHungerBoost();
    }

    public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
    {
      if (damageSource.Type != EnumDamageType.Heal || damageSource.Source != EnumDamageSource.Revive)
        return;
      this.HydrationLossDelay = this.MaxHydration / 2f;
      this.Hydration = this.MaxHydration / 2f;
    }

    public void Vomit()
    {
      Hydration = 0.6f * Hydration;
      HydrationLossDelay = 0;
      if (entity.HasBehavior<EntityBehaviorHunger>())
      {
        var bh = entity.GetBehavior<EntityBehaviorHunger>();
        bh.Saturation = 0.6f * bh.Saturation;
      }
      entity.World.PlaySoundAt(this.vomitSound, entity.Pos.X, entity.Pos.Y, entity.Pos.Z, range: 10f);
      entity.World.RegisterCallback(dt => entity.WatchedAttributes.SetFloat("intoxication", 0.0f), 5000);
    }
  }
  
}
