using System;
using BalancedThirst.Config;
using BalancedThirst.ModBehavior;
using BalancedThirst.Thirst;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace BalancedThirst.Systems;

// From HydrateOrDiedrate by Chronolegionnaire: https://github.com/Chronolegionnaire/HydrateOrDiedrate/tree/master
public static class BtCommands
{
    public static void Register(ICoreServerAPI api)
    {
        api.ChatCommands
            .Create("resetThirstStats")
            .WithDescription("Resets the player's stat modifiers from thirst and bladder.")
            .RequiresPrivilege("controlserver")
            .WithArgs(api.ChatCommands.Parsers.OptionalWord("playerName"))
            .HandleWith((args) => OnResetStatsCommand(api, args));
            
        api.ChatCommands
            .Create("setHydration")
            .WithDescription("Sets the player's hydration level.")
            .RequiresPrivilege("controlserver")
            .WithArgs(api.ChatCommands.Parsers.OptionalWord("playerName"),
                api.ChatCommands.Parsers.Float("hydrationValue"))
            .HandleWith((args) => OnSetThirstCommand(api, args));
    }

    private static TextCommandResult OnResetStatsCommand(ICoreServerAPI api, TextCommandCallingArgs args)
    {
        var playerName = args[0] as string;

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

        ResetModBoosts(targetPlayer?.Entity);
        return TextCommandResult.Success($"Thirst stats reset for player '{targetPlayer.PlayerName}'.");
    }

    private static TextCommandResult OnSetThirstCommand(ICoreServerAPI api,
        TextCommandCallingArgs args)
    {
        var playerName = args[0] as string;
        var hydration = (float)args[1];

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
    
    public static void ResetModBoosts(EntityPlayer player)
    {
        if (player == null) return;
        player.Attributes?.GetTreeAttribute(BtCore.Modid + ":thirst")?.SetFloat("dehydration", 0);
        if ((player.Api.Side & EnumAppSide.Server) != 0)
        {
            player.GetBehavior<EntityBehaviorThirst>().Dehydration = 0;
        }
        foreach (var stat in ConfigSystem.ConfigServer.ThirstStatMultipliers.Keys)
        {
            player.Stats.Remove(stat, BtCore.Modid + ":thirsty");
        }
        player.Stats.Remove(BtCore.Modid + ":thirstrate", "HoD:cooling");
        player.Stats.Remove(BtCore.Modid + ":thirstrate", "resistheat");
        player.Stats.Remove(BtCore.Modid + ":thirstrate", "dehydration");
        player.Stats.Remove("walkspeed", "bladderfull");
        player.Stats.Remove("walkspeed", "bowelfull");
    }
}