using System;
using BalancedThirst.Config;
using BalancedThirst.ModBehavior;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace BalancedThirst.Systems
{
    // From HydrateOrDiedrate by Chronolegionnaire: https://github.com/Chronolegionnaire/HydrateOrDiedrate/tree/master
    public static class BtCommands
    {
        public static void Register(ICoreServerAPI api, ConfigServer loadedConfig)
        {
            api.ChatCommands
                .Create("setthirst")
                .WithDescription("Sets the player's thirst level.")
                .RequiresPrivilege("controlserver")
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("playerName"), api.ChatCommands.Parsers.Float("thirstValue"))
                .HandleWith((args) => OnSetThirstCommand(api, loadedConfig, args));
        }

        private static TextCommandResult OnSetThirstCommand(ICoreServerAPI api, ConfigServer loadedConfig, TextCommandCallingArgs args)
        {
            string playerName = args[0] as string;
            float thirstValue = (float)args[1];

            IServerPlayer targetPlayer;

            if (string.IsNullOrEmpty(playerName))
            {
                targetPlayer = args.Caller.Player as IServerPlayer;
            }
            else
            {
                targetPlayer = GetPlayerByName(api, playerName);
                if (targetPlayer == null)
                {
                    return TextCommandResult.Error($"Player '{playerName}' not found.");
                }
            }

            var thirstBehavior = targetPlayer?.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior == null) return TextCommandResult.Error("Thirst behavior not found.");

            thirstBehavior.Hydration = thirstValue;
            thirstBehavior.UpdateThirstBoosts();

            return TextCommandResult.Success($"Thirst set to {thirstValue} for player '{targetPlayer.PlayerName}'.");
        }

        private static IServerPlayer GetPlayerByName(ICoreServerAPI api, string playerName)
        {
            foreach (var player1 in api.World.AllOnlinePlayers)
            {
                var player = (IServerPlayer)player1;
                if (player.PlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                {
                    return player;
                }
            }
            return null;
        }
    }
}