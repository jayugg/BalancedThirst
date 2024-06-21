using ProtoBuf;
using Vintagestory.API.MathTools;

namespace BalancedThirst.Network;

[ProtoContract]
public class DowsingRodMessage
{
    [ProtoMember(1)] public BlockPos Position;
}