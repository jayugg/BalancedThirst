using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace BalancedThirst.Blocks;

public class BlockWaterStorageContainer : BlockLiquidContainerTopOpened
{
    protected virtual float TransitionRateMul => 0.8f;
    public string LastVariant => this.Code.SecondCodePart().Split("-").Last();
    
    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        dsc.AppendLine(Lang.Get(BtCore.Modid+":Stored water perish speed: {0}", TransitionRateMul.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }
}