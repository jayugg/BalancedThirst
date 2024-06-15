using Vintagestory.API.Common;

namespace BalancedThirst.ModBehavior;

public class HydrationProperties
{
    public int Hydration;
    public float Contamination;
    public JsonItemStack EatenStack;
    
    public HydrationProperties Clone()
    {
        return new HydrationProperties()
        {
            Hydration = this.Hydration,
            Contamination = this.Contamination,
            EatenStack = this.EatenStack?.Clone()
        };
    }
}