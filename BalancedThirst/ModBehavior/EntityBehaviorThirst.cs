using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
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

    private readonly string _attributeKey = BtCore.Modid + ":thirst";

    public float SlakeLossDelay
    {
      get => this._thirstTree?.GetFloat("slakelossdelay") ?? 180;
      set
      {
        this._thirstTree?.SetFloat("slakelossdelay", value);
        this.entity.WatchedAttributes.MarkPathDirty(_attributeKey);
      }
    }

    public float Slake
    {
      get => this._thirstTree?.GetFloat("currentslake") ?? 1500f;
      set
      {
        this._thirstTree?.SetFloat("currentslake", value);
        this.entity.WatchedAttributes.MarkPathDirty(_attributeKey);
      }
    }

    public float MaxSlake
    {
      get => this._thirstTree?.GetFloat("maxslake") ?? 1500f;
      set
      {
        this._thirstTree?.SetFloat("maxslake", value);
        this.entity.WatchedAttributes.MarkPathDirty(_attributeKey);
      }
    }

    public EntityBehaviorThirst(Entity entity)
      : base(entity)
    {
      this._entityAgent = entity as EntityAgent;
    }

    public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
    {
      this._thirstTree = this.entity.WatchedAttributes.GetTreeAttribute(_attributeKey);
      this._api = this.entity.World.Api;
      if (this._thirstTree == null)
      {
        this.entity.WatchedAttributes.SetAttribute(_attributeKey, _thirstTree = new TreeAttribute());
        this.Slake = typeAttributes["currentslake"].AsFloat(1500f);
        this.MaxSlake = typeAttributes["maxslake"].AsFloat(1500f);
        this.SlakeLossDelay = 180.0f;
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
      this.ConsumeSlake(3f);
    }

    public virtual void ConsumeSlake(float amount) => this.ReduceSlake(amount / 10f);
    
    public override void OnEntityReceiveSaturation(
      float saturation,
      EnumFoodCategory foodCat = EnumFoodCategory.Unknown,
      float saturationLossDelay = 10f,
      float nutritionGainMultiplier = 1f)
    {
    float maxSlake = this.MaxSlake;
    bool isSlakeMaxed = this.Slake >= maxSlake;
    
    // Adjust slake and slake loss delay based on food category
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
        default:
          break;
    }

    this.Slake = Math.Min(maxSlake, Math.Max(0, this.Slake + saturation));
    if (!isSlakeMaxed) this.SlakeLossDelay = Math.Max(this.SlakeLossDelay, saturationLossDelay);
    this.UpdateThirstHungerBoost();
}

    public override void OnGameTick(float deltaTime)
    {
      if (this.entity is EntityPlayer player)
      {
        if (entity.World.Side == EnumAppSide.Server)
        {
          //BtCore.Logger.Notification("Thirst Rate: " + this.entity.Stats.GetBlended("thirstrate"));
          //BtCore.Logger.Notification("Hunger Modifier: " + this.HungerModifier);
          //BtCore.Logger.Notification("Hunger Rate: " + entity.Stats.GetBlended("hungerrate"));
          //BtCore.Logger.Notification("Current slake (Behaviour): " + this.Slake + " / " + this.MaxSlake);
        }

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
      this.ReduceSlake(num3 * (float) (1.2000000476837158 * (8.0 + this._sprintCounter / 15.0) / 10.0) * this.entity.Stats.GetBlended("thirstrate") * num2);
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

    private bool ReduceSlake(float slakeLossMultiplier)
    {
      bool flag = false;
      slakeLossMultiplier *= GlobalConstants.HungerSpeedModifier;
      if (this.SlakeLossDelay > 0.0)
      {
        this.SlakeLossDelay -= 10f * slakeLossMultiplier;
        flag = true;
      }
      else
        this.Slake = Math.Max(0.0f, this.Slake - (float) (Math.Max(0.5f, 1f / 1000f * this.Slake) * (double) slakeLossMultiplier * 0.25));
      this.UpdateThirstHungerBoost();
      if (flag)
      {
        this._thirstCounter -= 10f;
        return true;
      }
      float slake = this.Slake;
      if (slake > 0.0)
      {
        this.Slake = Math.Max(0.0f, slake - slakeLossMultiplier * 10f);
        this._sprintCounter = 0;
      }
      return false;
    }
    
    private static float _bParam = 0.3f; // Will be targetable by config
    private static float _cParam = 2f;
    public float HungerModifier => _bParam * (float) Math.Tanh(_cParam*(1f - 2f*Slake/MaxSlake));

    private void UpdateThirstHungerBoost()
    {
      this.entity.Stats.Set("hungerrate", BtCore.Modid + ":thirsty", HungerModifier);
      entity.WatchedAttributes.MarkPathDirty("stats");
    }

    private void SlowTick(float dt)
    {
      if (this.entity is EntityPlayer && this.entity.World.PlayerByUid(((EntityPlayer) this.entity).PlayerUID).WorldData.CurrentGameMode == EnumGameMode.Creative)
        return;
      
      bool flag = this.entity.World.Config.GetString("harshWinters").ToBool(true);
      float temperature = this.entity.World.BlockAccessor.GetClimateAt(this.entity.Pos.AsBlockPos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, this.entity.World.Calendar.TotalDays).Temperature;
      float bodyTemperature = entity.GetBehavior<EntityBehaviorBodyTemperature>().CurBodyTemperature = temperature;
      if (temperature <= 30.0 && !flag)
      {
        this.entity.Stats.Remove(BtCore.Modid+":thirstrate", "resistheat");
      }
      else
      {
        float num = GameMath.Clamp(30f - temperature, 0.0f, 10f);
        this.entity.Stats.Set(BtCore.Modid+":thirstrate", "resistheat", this.entity.World.Api.ModLoader.GetModSystem<RoomRegistry>().GetRoomForPosition(this.entity.Pos.AsBlockPos).ExitCount == 0 ? 0.0f : num / 40f, true);
      }
      UpdateThirstHungerBoost();
    }

    public override string PropertyName() => BtCore.Modid + ":thirst";

    public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
    {
      if (damageSource.Type != EnumDamageType.Heal || damageSource.Source != EnumDamageSource.Revive)
        return;
      this.SlakeLossDelay = this.MaxSlake / 2f;
      this.Slake = this.MaxSlake / 2f;
    }
  }
}
