using ProtoBuf;
using Vintagestory.API.MathTools;

namespace BalancedThirst.Network;

public class PeeMessage
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
        public BlockPos Position;
    }
}