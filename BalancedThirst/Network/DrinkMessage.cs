using ProtoBuf;

namespace BalancedThirst.Network;

public class DrinkMessage
{
    [ProtoContract]
    public class Request
    {
        [ProtoMember(1)]
        public string message;
    }

    [ProtoContract]
    public class Response
    {
        [ProtoMember(1)]
        public string response;
    }
}