using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace BalancedThirst.ModBehavior;

public class EntityBehaviorBladder : EntityBehavior
{
    private ITreeAttribute _bladderTree;
    private ICoreAPI _api;
    private ILoadedSound pouringLoop;
    public override string PropertyName() => AttributeKey;
    private long _listenerId;
    private string AttributeKey => BtCore.Modid + ":bladder";
    public static SimpleParticleProperties WaterParticles;

    public float Capacity
    {
        get => this._bladderTree?.GetFloat("capacity") ?? BtCore.ConfigServer.MaxHydration;
        set
        {
            this._bladderTree?.SetFloat("capacity", value);
            this.entity.WatchedAttributes.MarkPathDirty(AttributeKey);
        }
    }

    public float CurrentLevel
    {
        get => this._bladderTree?.GetFloat("currentlevel") ?? BtCore.ConfigServer.MaxHydration;
        set
        {
            this._bladderTree?.SetFloat("currentlevel", value);
            this.entity.WatchedAttributes.MarkPathDirty(AttributeKey);
        }
    }
    
    public EntityBehaviorBladder(Entity entity) : base(entity)
    {
    }

    public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
    {
        this._bladderTree = this.entity.WatchedAttributes.GetTreeAttribute(AttributeKey);
        this._api = this.entity.World.Api;
        if (this._bladderTree == null || this._bladderTree.GetFloat("capacity") == 0)
        {
            this.entity.WatchedAttributes.SetAttribute(AttributeKey, _bladderTree = new TreeAttribute());
            this.CurrentLevel = typeAttributes["currentlevel"].AsFloat(BtCore.ConfigServer.MaxHydration);
            this.Capacity = typeAttributes["capacity"].AsFloat(BtCore.ConfigServer.MaxHydration);
        }
        WaterParticles = new SimpleParticleProperties(1f, 1f, -1, new Vec3d(), new Vec3d(), new Vec3f(-1.5f, 0.0f, -1.5f), new Vec3f(1.5f, 3f, 1.5f), minSize: 0.33f, maxSize: 0.75f)
            {
                AddPos = new Vec3d(1.0 / 16.0, 0.125, 1.0 / 16.0),
                SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -1f),
                ClimateColorMap = "climateWaterTint",
                AddQuantity = 1f
            };
    }
    
    public void ReceiveCapacity(float capacity)
    {
        this.Capacity = Math.Max(0.0f, this.Capacity + capacity);
    }
    
    public bool Drain(float multiplier = 1)
    {
        float currentLevel = this.CurrentLevel;
        if (currentLevel > 0.0)
        {
            this.CurrentLevel = Math.Max(0.0f, currentLevel - multiplier * 10f);
            return true;
        }
        return false;
    }
    
    public void After350ms(float dt)
    {
        ICoreClientAPI api = this._api as ICoreClientAPI;
        IClientPlayer player = api.World.Player;
        EntityPlayer entity = player.Entity;
        if (entity.Controls.HandUse == EnumHandInteract.HeldItemInteract)
            api.World.PlaySoundAt(new AssetLocation("sounds/effect/watering"), (Entity) entity, (IPlayer) player);
        if (entity.Controls.HandUse != EnumHandInteract.HeldItemInteract)
            return;
        if (this.pouringLoop != null)
        {
            this.pouringLoop.FadeIn(0.3f, (Action<ILoadedSound>) null);
        }
        else
        {
            this.pouringLoop = api.World.LoadSound(new SoundParams()
            {
                DisposeOnFinish = false,
                Location = new AssetLocation("sounds/effect/watering-loop.ogg"),
                Position = new Vec3f(),
                RelativePosition = true,
                ShouldLoop = true,
                Range = 16f,
                Volume = 0.2f,
                Pitch = 0.5f
            });
            this.pouringLoop.Start();
            this.pouringLoop.FadeIn(0.15f, (Action<ILoadedSound>) null);
        }
    }
}