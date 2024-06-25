using System;
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
    }
    
    public void ReceiveCapacity(float capacity)
    {
        this.Capacity = Math.Max(0.0f, this.Capacity + capacity);
    }
    
    private bool Drain(float multiplier = 1)
    {
        float currentLevel = this.CurrentLevel;
        if (currentLevel > 0.0)
        {
            this.CurrentLevel = Math.Max(0.0f, currentLevel - multiplier * 10f);
            return true;
        }
        return false;
    }
}