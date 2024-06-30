using ProtoBuf;
using Vintagestory.API.MathTools;

namespace BalancedThirst.Network;

public class DrinkMessage
{
    [ProtoContract]
    public class Request
    {
        [ProtoMember(1)]
        public BlockPos Position;
    }

    [ProtoContract]
    public class Response
    {
        [ProtoMember(1)]
        public Vec3d Position;
    }
}