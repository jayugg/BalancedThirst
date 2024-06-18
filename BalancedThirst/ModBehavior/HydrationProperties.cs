using Vintagestory.API.Common;

namespace BalancedThirst.ModBehavior;

public class HydrationProperties
{
    public float Hydration;
    public float HydrationLossDelay = 10f;
    public float Contamination;
    public bool Scalding;
    public bool Salty;
    
    public HydrationProperties Clone()
    {
        return new HydrationProperties()
        {
            Hydration = this.Hydration,
            HydrationLossDelay = this.HydrationLossDelay,
            Contamination = this.Contamination,
            Scalding = this.Scalding,
            Salty = this.Salty
        };
    }
}