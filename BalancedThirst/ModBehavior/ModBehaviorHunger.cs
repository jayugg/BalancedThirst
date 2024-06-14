using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

#nullable disable
namespace BalancedThirst.ModBehavior
{
  public class ModBehaviorHunger : EntityBehavior
  {
    private ITreeAttribute hungerTree;
    private EntityAgent entityAgent;
    private float hungerCounter;
    private int sprintCounter;
    private long listenerId;
    private long lastMoveMs;
    private ICoreAPI api;
    private float detoxCounter;
    
    private string AttributeKey => BtModSystem.Modid + ":hunger";

    public float SaturationLossDelayFruit
    {
      get => this.hungerTree.GetFloat("saturationlossdelayfruit");
      set
      {
        this.hungerTree.SetFloat("saturationlossdelayfruit", value);
        this.entity.WatchedAttributes.MarkPathDirty(AttributeKey);
      }
    }

    public float SaturationLossDelayVegetable
    {
      get => this.hungerTree.GetFloat("saturationlossdelayvegetable");
      set
      {
        this.hungerTree.SetFloat("saturationlossdelayvegetable", value);
        this.entity.WatchedAttributes.MarkPathDirty(AttributeKey);
      }
    }

    public float SaturationLossDelayProtein
    {
      get => this.hungerTree.GetFloat("saturationlossdelayprotein");
      set
      {
        this.hungerTree.SetFloat("saturationlossdelayprotein", value);
        this.entity.WatchedAttributes.MarkPathDirty(AttributeKey);
      }
    }

    public float SaturationLossDelayGrain
    {
      get => this.hungerTree.GetFloat("saturationlossdelaygrain");
      set
      {
        this.hungerTree.SetFloat("saturationlossdelaygrain", value);
        this.entity.WatchedAttributes.MarkPathDirty(AttributeKey);
      }
    }

    public float SaturationLossDelayDairy
    {
      get => this.hungerTree.GetFloat("saturationlossdelaydairy");
      set
      {
        this.hungerTree.SetFloat("saturationlossdelaydairy", value);
        this.entity.WatchedAttributes.MarkPathDirty(AttributeKey);
      }
    }

    public float Saturation
    {
      get => this.hungerTree.GetFloat("currentsaturation");
      set
      {
        this.hungerTree.SetFloat("currentsaturation", value);
        this.entity.WatchedAttributes.MarkPathDirty(AttributeKey);
      }
    }

    public float MaxSaturation
    {
      get => this.hungerTree.GetFloat("maxsaturation");
      set
      {
        this.hungerTree.SetFloat("maxsaturation", value);
        this.entity.WatchedAttributes.MarkPathDirty(AttributeKey);
      }
    }

    public float FruitLevel
    {
      get => this.hungerTree.GetFloat("fruitLevel");
      set
      {
        this.hungerTree.SetFloat("fruitLevel", value);
        this.entity.WatchedAttributes.MarkPathDirty(AttributeKey);
      }
    }

    public float VegetableLevel
    {
      get => this.hungerTree.GetFloat("vegetableLevel");
      set
      {
        this.hungerTree.SetFloat("vegetableLevel", value);
        this.entity.WatchedAttributes.MarkPathDirty(AttributeKey);
      }
    }

    public float ProteinLevel
    {
      get => this.hungerTree.GetFloat("proteinLevel");
      set
      {
        this.hungerTree.SetFloat("proteinLevel", value);
        this.entity.WatchedAttributes.MarkPathDirty(AttributeKey);
      }
    }

    public float GrainLevel
    {
      get => this.hungerTree.GetFloat("grainLevel");
      set
      {
        this.hungerTree.SetFloat("grainLevel", value);
        this.entity.WatchedAttributes.MarkPathDirty(AttributeKey);
      }
    }

    public float DairyLevel
    {
      get => this.hungerTree.GetFloat("dairyLevel");
      set
      {
        this.hungerTree.SetFloat("dairyLevel", value);
        this.entity.WatchedAttributes.MarkPathDirty(AttributeKey);
      }
    }

    public ModBehaviorHunger(Entity entity)
      : base(entity)
    {
      this.entityAgent = entity as EntityAgent;
    }

    public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
    {
      this.hungerTree = this.entity.WatchedAttributes.GetTreeAttribute(AttributeKey);
      this.api = this.entity.World.Api;
      if (this.hungerTree == null)
      {
        this.entity.WatchedAttributes.SetAttribute(AttributeKey, (IAttribute) (this.hungerTree = (ITreeAttribute) new TreeAttribute()));
        this.Saturation = typeAttributes["currentsaturation"].AsFloat(1500f);
        this.MaxSaturation = typeAttributes["maxsaturation"].AsFloat(1500f);
        this.SaturationLossDelayFruit = 0.0f;
        this.SaturationLossDelayVegetable = 0.0f;
        this.SaturationLossDelayGrain = 0.0f;
        this.SaturationLossDelayProtein = 0.0f;
        this.SaturationLossDelayDairy = 0.0f;
        this.FruitLevel = 0.0f;
        this.VegetableLevel = 0.0f;
        this.GrainLevel = 0.0f;
        this.ProteinLevel = 0.0f;
        this.DairyLevel = 0.0f;
      }
      this.listenerId = this.entity.World.RegisterGameTickListener(new Action<float>(this.SlowTick), 6000);
      this.UpdateNutrientHealthBoost();
    }

    public override void OnEntityDespawn(EntityDespawnData despawn)
    {
      base.OnEntityDespawn(despawn);
      this.entity.World.UnregisterGameTickListener(this.listenerId);
    }

    public override void DidAttack(
      DamageSource source,
      EntityAgent targetEntity,
      ref EnumHandling handled)
    {
      this.ConsumeSaturation(3f);
    }

    public virtual void ConsumeSaturation(float amount) => this.ReduceSaturation(amount / 10f);

    public override void OnEntityReceiveSaturation(
      float saturation,
      EnumFoodCategory foodCat = EnumFoodCategory.Unknown,
      float saturationLossDelay = 10f,
      float nutritionGainMultiplier = 1f)
    {
      float maxSaturation = this.MaxSaturation;
      bool flag = (double) this.Saturation >= (double) maxSaturation;
      this.Saturation = Math.Min(maxSaturation, this.Saturation + saturation);
      switch (foodCat)
      {
        case EnumFoodCategory.Fruit:
          if (!flag)
            this.FruitLevel = Math.Min(maxSaturation, this.FruitLevel + saturation / 2.5f * nutritionGainMultiplier);
          this.SaturationLossDelayFruit = Math.Max(this.SaturationLossDelayFruit, saturationLossDelay);
          break;
        case EnumFoodCategory.Vegetable:
          if (!flag)
            this.VegetableLevel = Math.Min(maxSaturation, this.VegetableLevel + saturation / 2.5f * nutritionGainMultiplier);
          this.SaturationLossDelayVegetable = Math.Max(this.SaturationLossDelayVegetable, saturationLossDelay);
          break;
        case EnumFoodCategory.Protein:
          if (!flag)
            this.ProteinLevel = Math.Min(maxSaturation, this.ProteinLevel + saturation / 2.5f * nutritionGainMultiplier);
          this.SaturationLossDelayProtein = Math.Max(this.SaturationLossDelayProtein, saturationLossDelay);
          break;
        case EnumFoodCategory.Grain:
          if (!flag)
            this.GrainLevel = Math.Min(maxSaturation, this.GrainLevel + saturation / 2.5f * nutritionGainMultiplier);
          this.SaturationLossDelayGrain = Math.Max(this.SaturationLossDelayGrain, saturationLossDelay);
          break;
        case EnumFoodCategory.Dairy:
          if (!flag)
            this.DairyLevel = Math.Min(maxSaturation, this.DairyLevel + saturation / 2.5f * nutritionGainMultiplier);
          this.SaturationLossDelayDairy = Math.Max(this.SaturationLossDelayDairy, saturationLossDelay);
          break;
      }
      this.UpdateNutrientHealthBoost();
    }

    public override void OnGameTick(float deltaTime)
    {
      if (this.entity is EntityPlayer)
      {
        EntityPlayer entity = (EntityPlayer) this.entity;
        EnumGameMode currentGameMode = this.entity.World.PlayerByUid(entity.PlayerUID).WorldData.CurrentGameMode;
        this.detox(deltaTime);
        if (currentGameMode == EnumGameMode.Creative || currentGameMode == EnumGameMode.Spectator)
          return;
        if (entity.Controls.TriesToMove || entity.Controls.Jump || entity.Controls.LeftMouseDown || entity.Controls.RightMouseDown)
          this.lastMoveMs = this.entity.World.ElapsedMilliseconds;
      }
      if (this.entityAgent != null && this.entityAgent.Controls.Sprint)
        ++this.sprintCounter;
      this.hungerCounter += deltaTime;
      if ((double) this.hungerCounter <= 10.0)
        return;
      int num1 = this.entity.World.ElapsedMilliseconds - this.lastMoveMs > 3000L ? 1 : 0;
      float num2 = this.entity.Api.World.Calendar.SpeedOfTime * this.entity.Api.World.Calendar.CalendarSpeedMul;
      float num3 = GlobalConstants.HungerSpeedModifier / 30f;
      if (num1 != 0)
        num3 /= 4f;
      this.ReduceSaturation(num3 * (float) (1.2000000476837158 * (8.0 + (double) this.sprintCounter / 15.0) / 10.0) * this.entity.Stats.GetBlended("hungerrate") * num2);
      this.hungerCounter = 0.0f;
      this.sprintCounter = 0;
      this.detox(deltaTime);
    }

    private void detox(float dt)
    {
      this.detoxCounter += dt;
      if ((double) this.detoxCounter <= 1.0)
        return;
      float num = this.entity.WatchedAttributes.GetFloat("intoxication", 0.0f);
      if ((double) num > 0.0)
        this.entity.WatchedAttributes.SetFloat("intoxication", Math.Max(0.0f, num - 0.005f));
      this.detoxCounter = 0.0f;
    }

    private bool ReduceSaturation(float satLossMultiplier)
    {
      bool flag = false;
      satLossMultiplier *= GlobalConstants.HungerSpeedModifier;
      if ((double) this.SaturationLossDelayFruit > 0.0)
      {
        this.SaturationLossDelayFruit -= 10f * satLossMultiplier;
        flag = true;
      }
      else
        this.FruitLevel = Math.Max(0.0f, this.FruitLevel - (float) ((double) Math.Max(0.5f, 1f / 1000f * this.FruitLevel) * (double) satLossMultiplier * 0.25));
      if ((double) this.SaturationLossDelayVegetable > 0.0)
      {
        this.SaturationLossDelayVegetable -= 10f * satLossMultiplier;
        flag = true;
      }
      else
        this.VegetableLevel = Math.Max(0.0f, this.VegetableLevel - (float) ((double) Math.Max(0.5f, 1f / 1000f * this.VegetableLevel) * (double) satLossMultiplier * 0.25));
      if ((double) this.SaturationLossDelayProtein > 0.0)
      {
        this.SaturationLossDelayProtein -= 10f * satLossMultiplier;
        flag = true;
      }
      else
        this.ProteinLevel = Math.Max(0.0f, this.ProteinLevel - (float) ((double) Math.Max(0.5f, 1f / 1000f * this.ProteinLevel) * (double) satLossMultiplier * 0.25));
      if ((double) this.SaturationLossDelayGrain > 0.0)
      {
        this.SaturationLossDelayGrain -= 10f * satLossMultiplier;
        flag = true;
      }
      else
        this.GrainLevel = Math.Max(0.0f, this.GrainLevel - (float) ((double) Math.Max(0.5f, 1f / 1000f * this.GrainLevel) * (double) satLossMultiplier * 0.25));
      if ((double) this.SaturationLossDelayDairy > 0.0)
      {
        this.SaturationLossDelayDairy -= 10f * satLossMultiplier;
        flag = true;
      }
      else
        this.DairyLevel = Math.Max(0.0f, this.DairyLevel - (float) ((double) Math.Max(0.5f, 1f / 1000f * this.DairyLevel) * (double) satLossMultiplier * 0.25 / 2.0));
      this.UpdateNutrientHealthBoost();
      if (flag)
      {
        this.hungerCounter -= 10f;
        return true;
      }
      float saturation = this.Saturation;
      if ((double) saturation > 0.0)
      {
        this.Saturation = Math.Max(0.0f, saturation - satLossMultiplier * 10f);
        this.sprintCounter = 0;
      }
      return false;
    }

    public void UpdateNutrientHealthBoost()
    {
      float num1 = this.FruitLevel / this.MaxSaturation;
      float num2 = this.GrainLevel / this.MaxSaturation;
      float num3 = this.VegetableLevel / this.MaxSaturation;
      float num4 = this.ProteinLevel / this.MaxSaturation;
      float num5 = this.DairyLevel / this.MaxSaturation;
      EntityBehaviorHealth behavior = this.entity.GetBehavior<EntityBehaviorHealth>();
      behavior.MaxHealthModifiers["nutrientHealthMod"] = (float) (2.5 * ((double) num1 + (double) num2 + (double) num3 + (double) num4 + (double) num5));
      behavior.MarkDirty();
    }

    private void SlowTick(float dt)
    {
      if (this.entity is EntityPlayer && this.entity.World.PlayerByUid(((EntityPlayer) this.entity).PlayerUID).WorldData.CurrentGameMode == EnumGameMode.Creative)
        return;
      bool flag = this.entity.World.Config.GetString("harshWinters").ToBool(true);
      float temperature = this.entity.World.BlockAccessor.GetClimateAt(this.entity.Pos.AsBlockPos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, this.entity.World.Calendar.TotalDays).Temperature;
      if ((double) temperature >= 2.0 || !flag)
      {
        this.entity.Stats.Remove("hungerrate", "resistcold");
      }
      else
      {
        float num = GameMath.Clamp(2f - temperature, 0.0f, 10f);
        this.entity.Stats.Set("hungerrate", "resistcold", this.entity.World.Api.ModLoader.GetModSystem<RoomRegistry>().GetRoomForPosition(this.entity.Pos.AsBlockPos).ExitCount == 0 ? 0.0f : num / 40f, true);
      }
      if ((double) this.Saturation > 0.0)
        return;
      this.entity.ReceiveDamage(new DamageSource()
      {
        Source = EnumDamageSource.Internal,
        Type = EnumDamageType.Hunger
      }, 0.125f);
      this.sprintCounter = 0;
    }

    public override string PropertyName() => AttributeKey;

    public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
    {
      if (damageSource.Type != EnumDamageType.Heal || damageSource.Source != EnumDamageSource.Revive)
        return;
      this.SaturationLossDelayFruit = 60f;
      this.SaturationLossDelayVegetable = 60f;
      this.SaturationLossDelayProtein = 60f;
      this.SaturationLossDelayGrain = 60f;
      this.SaturationLossDelayDairy = 60f;
      this.Saturation = this.MaxSaturation / 2f;
      this.VegetableLevel /= 2f;
      this.ProteinLevel /= 2f;
      this.FruitLevel /= 2f;
      this.DairyLevel /= 2f;
      this.GrainLevel /= 2f;
    }
  }
}
