using System;
using BalancedThirst.Config;
using BalancedThirst.ModBehavior;
using BalancedThirst.Thirst;
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
                .Create("setHydration")
                .WithDescription("Sets the player's hydration level.")
                .RequiresPrivilege("controlserver")
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("playerName"),
                    api.ChatCommands.Parsers.Float("hydrationValue"))
                .HandleWith((args) => OnSetThirstCommand(api, loadedConfig, args));

            api.ChatCommands
                .Create("setBladder")
                .WithDescription("Sets the player's bladder level.")
                .RequiresPrivilege("controlserver")
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("playerName"),
                    api.ChatCommands.Parsers.Float("bladderValue"))
                .HandleWith((args) => OnSetBladderCommand(api, loadedConfig, args));
        }

        private static TextCommandResult OnSetThirstCommand(ICoreServerAPI api, ConfigServer loadedConfig,
            TextCommandCallingArgs args)
        {
            string playerName = args[0] as string;
            float hydration = (float)args[1];

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

            thirstBehavior.Hydration = hydration;
            thirstBehavior.UpdateThirstBoosts();

            return TextCommandResult.Success($"Hydration set to {hydration} for player '{targetPlayer.PlayerName}'.");
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

        private static TextCommandResult OnSetBladderCommand(ICoreServerAPI api, ConfigServer loadedConfig,
            TextCommandCallingArgs args)
        {
            string playerName = args[0] as string;
            float newLevel = (float)args[1];

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

            var bladderBehavior = targetPlayer?.Entity.GetBehavior<EntityBehaviorBladder>();
            if (bladderBehavior == null) return TextCommandResult.Error("Bladder behavior not found.");

            bladderBehavior.CurrentLevel = newLevel;

            return TextCommandResult.Success($"Bladder set to {newLevel} for player '{targetPlayer.PlayerName}'.");
        }
    }
}