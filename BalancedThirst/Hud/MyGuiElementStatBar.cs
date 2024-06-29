using Vintagestory.API.Client;

namespace BalancedThirst.Hud;

public class MyGuiElementStatbar : GuiElementStatbar
{
    public float HideWhenLessThan { get; set; }
    
    public float MinValue { get; set; }
    public float MaxValue { get; set; }
    
    
    public void SetValues(float value, float min, float max)
    {
        this.MinValue = min;
        this.MaxValue = max;
        base.SetValues(value, min, max);
    }
    
    public MyGuiElementStatbar(ICoreClientAPI capi, ElementBounds bounds, double[] color, bool rightToLeft, bool hideable) : base(capi, bounds, color, rightToLeft, hideable)
    {
    }

    public override void RenderInteractiveElements(float deltaTime)
    {
        if (this.GetValue() < this.HideWhenLessThan*MaxValue)
            return;
        base.RenderInteractiveElements(deltaTime);
    }

}