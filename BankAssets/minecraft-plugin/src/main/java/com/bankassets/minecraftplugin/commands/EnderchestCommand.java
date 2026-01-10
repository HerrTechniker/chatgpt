package com.bankassets.minecraftplugin.commands;

import org.bukkit.Bukkit;
import org.bukkit.command.Command;
import org.bukkit.command.CommandExecutor;
import org.bukkit.command.CommandSender;
import org.bukkit.entity.Player;

public class EnderchestCommand implements CommandExecutor {

    @Override
    public boolean onCommand(CommandSender sender, Command command, String label, String[] args) {
        if (!(sender instanceof Player player)) {
            sender.sendMessage("Dieses Kommando ist nur für Spieler.");
            return true;
        }
        if (!player.hasPermission("bankassets.enderchest")) {
            player.sendMessage("§cDu hast keine Berechtigung dafür.");
            return true;
        }
        if (args.length == 0) {
            player.openInventory(player.getEnderChest());
            player.sendMessage("§aDeine Enderchest wurde geöffnet.");
            return true;
        }
        if (args.length == 1) {
            Player target = Bukkit.getPlayerExact(args[0]);
            if (target == null) {
                player.sendMessage("§cSpieler nicht gefunden oder offline.");
                return true;
            }
            player.openInventory(target.getEnderChest());
            player.sendMessage("§aEnderchest von §e" + target.getName() + " §ageöffnet.");
            return true;
        }
        player.sendMessage("§cBenutzung: /enderchest [spieler]");
        return true;
    }
}
