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
    public override string PropertyName() => AttributeKey;
    private string AttributeKey => BtModSystem.Modid + ":thirst";

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
      BtModSystem.Logger.Warning("Initializing thirst behaviour");
      this._thirstTree = this.entity.WatchedAttributes.GetTreeAttribute(AttributeKey);
      this._api = this.entity.World.Api;
      if (this._thirstTree == null)
      {
        BtModSystem.Logger.Warning("Thirst tree is null");
        BtModSystem.Logger.Warning("Entity is");
        BtModSystem.Logger.Warning(this.entity.Code.ToString());
        this.entity.WatchedAttributes.SetAttribute(AttributeKey, _thirstTree = new TreeAttribute());
        BtModSystem.Logger.Warning("Thirst tree set");
        this.Hydration = typeAttributes["currenthydration"].AsFloat(1500f);
        this.MaxHydration = typeAttributes["maxhydration"].AsFloat(1500f);
        this.HydrationLossDelay = 180.0f;
      }
      this._listenerId = this.entity.World.RegisterGameTickListener(this.SlowTick, 6000);
      entity.Stats.Register(BtModSystem.Modid+":thirstrate");
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

public override void OnEntityReceiveSaturation(
    float saturation,
    EnumFoodCategory foodCat = EnumFoodCategory.Unknown,
    float saturationLossDelay = 10f,
    float nutritionGainMultiplier = 1f)
{
    float maxHydration = this.MaxHydration;
    bool isHydrationMaxed = this.Hydration >= maxHydration;

    // Adjust saturation and saturation loss delay based on food category
    switch (foodCat)
    {
        case EnumFoodCategory.Fruit:
            saturation *= 0.3f;
            saturationLossDelay *= 0.3f;
            break;
        case EnumFoodCategory.Vegetable:
            saturation *= 0.25f;
            saturationLossDelay *= 0.25f;
            break;
        case EnumFoodCategory.Dairy:
            saturation *= 0.2f;
            saturationLossDelay *= 0.2f;
            break;
        case EnumFoodCategory.Protein:
            saturation *= 0.1f;
            saturationLossDelay *= 0.1f;
            break;
        case EnumFoodCategory.Grain:
            saturation *= -0.1f;
            saturationLossDelay *= -0.1f;
            break;
    }

    this.Hydration = Math.Min(maxHydration, Math.Max(0, this.Hydration + saturation));
    if (!isHydrationMaxed) this.HydrationLossDelay = Math.Max(this.HydrationLossDelay, saturationLossDelay);
    this.UpdateThirstHungerBoost();
}

    public override void OnGameTick(float deltaTime)
    {
      if (this.entity is EntityPlayer player)
      {
        if (entity.World.Side == EnumAppSide.Server)
        {
          BtModSystem.Logger.Notification("Thirst Rate: " + this.entity.Stats.GetBlended("thirstrate"));
          BtModSystem.Logger.Notification("Hunger Modifier: " + this.HungerModifier);
          BtModSystem.Logger.Notification("Hunger Rate: " + entity.Stats.GetBlended("hungerrate"));
          BtModSystem.Logger.Notification("Current hydration (Behaviour): " + this.Hydration + " / " + this.MaxHydration);
        }
        var tree = player.WatchedAttributes.GetTreeAttribute(AttributeKey);
        BtModSystem.Logger.Warning("Tree");
        BtModSystem.Logger.Notification("Current hydration (Player): " + tree.GetFloat("currenthydration") + " / " + tree.GetFloat("maxhydration"));

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
      this.entity.Stats.Set("hungerrate", BtModSystem.Modid + ":thirsty", HungerModifier);
      entity.WatchedAttributes.MarkPathDirty("stats");
    }

    private void SlowTick(float dt)
    {
      if (this.entity is EntityPlayer && this.entity.World.PlayerByUid(((EntityPlayer) this.entity).PlayerUID).WorldData.CurrentGameMode == EnumGameMode.Creative)
        return;
      float temperature = this.entity.World.BlockAccessor.GetClimateAt(this.entity.Pos.AsBlockPos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, this.entity.World.Calendar.TotalDays).Temperature;
      float bodyTemperature = entity.GetBehavior<EntityBehaviorBodyTemperature>().CurBodyTemperature = temperature;
      if (temperature <= 30.0)
      {
        this.entity.Stats.Remove(BtModSystem.Modid+":thirstrate", "resistheat");
      }
      else
      {
        float num = GameMath.Clamp(30f - temperature, 0.0f, 10f);
        this.entity.Stats.Set(BtModSystem.Modid+":thirstrate", "resistheat", this.entity.World.Api.ModLoader.GetModSystem<RoomRegistry>().GetRoomForPosition(this.entity.Pos.AsBlockPos).ExitCount == 0 ? 0.0f : num / 40f, true);
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
  }
}
