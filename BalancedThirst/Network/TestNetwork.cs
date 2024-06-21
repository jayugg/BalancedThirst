
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace BalancedThirst.Network
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class NetworkApiTestMessage
    {
        public string message;
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class NetworkApiTestResponse
    {
        public string response;
    }

    /// <summary>
    /// A basic example of client<->server networking using a custom network communication
    /// </summary>
    public class NetworkApiTest : ModSystem
    {
        #region Server
        IServerNetworkChannel serverChannel;
        ICoreServerAPI serverApi;
        
        public override double ExecuteOrder() => 1.02;

        public override void StartServerSide(ICoreServerAPI api)
        {
            serverApi = api;

            serverChannel =
                api.Network.RegisterChannel("networkapitest")
                .RegisterMessageType(typeof(NetworkApiTestMessage))
                .RegisterMessageType(typeof(NetworkApiTestResponse))
                .SetMessageHandler<NetworkApiTestResponse>(OnClientMessage)
            ;

            api.ChatCommands.Create("nwtest")
                .WithDescription("Send a test network message")
                .RequiresPrivilege(Privilege.controlserver)
                .HandleWith(new OnCommandDelegate(OnNwTestCmd));
        }

        private TextCommandResult OnNwTestCmd(TextCommandCallingArgs args)
        {
            serverChannel.BroadcastPacket(new NetworkApiTestMessage()
            {
                message = "Hello World!",
            });
            return TextCommandResult.Success();
        }

        private void OnClientMessage(IPlayer fromPlayer, NetworkApiTestResponse networkMessage)
        {
            serverApi.SendMessageToGroup(
                GlobalConstants.GeneralChatGroup,
                "Received following response from " + fromPlayer.PlayerName + ": " + networkMessage.response,
                EnumChatType.Notification
            );
        }

        #endregion
        #region Client
        IClientNetworkChannel clientChannel;
        ICoreClientAPI clientApi;

        public override void StartClientSide(ICoreClientAPI api)
        {
            clientApi = api;

            clientChannel =
                api.Network.RegisterChannel("networkapitest")
                .RegisterMessageType(typeof(NetworkApiTestMessage))
                .RegisterMessageType(typeof(NetworkApiTestResponse))
                .SetMessageHandler<NetworkApiTestMessage>(OnServerMessage)
            ;
        }

        private void OnServerMessage(NetworkApiTestMessage networkMessage)
        {
            clientApi.ShowChatMessage("Received following message from server: " + networkMessage.message);
            clientApi.ShowChatMessage("Sending response.");
            clientChannel.SendPacket(new NetworkApiTestResponse()
            {
                response = "RE: Hello World!"
            });
        }

        #endregion
    }
}