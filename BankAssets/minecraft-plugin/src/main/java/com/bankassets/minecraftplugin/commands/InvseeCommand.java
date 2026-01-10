package com.bankassets.minecraftplugin.commands;

import org.bukkit.Bukkit;
import org.bukkit.command.Command;
import org.bukkit.command.CommandExecutor;
import org.bukkit.command.CommandSender;
import org.bukkit.entity.Player;

public class InvseeCommand implements CommandExecutor {

    @Override
    public boolean onCommand(CommandSender sender, Command command, String label, String[] args) {
        if (!(sender instanceof Player player)) {
            sender.sendMessage("Dieses Kommando ist nur für Spieler.");
            return true;
        }
        if (!player.hasPermission("bankassets.invsee")) {
            player.sendMessage("§cDu hast keine Berechtigung dafür.");
            return true;
        }
        if (args.length != 1) {
            player.sendMessage("§cBenutzung: /invsee <spieler>");
            return true;
        }
        Player target = Bukkit.getPlayerExact(args[0]);
        if (target == null) {
            player.sendMessage("§cSpieler nicht gefunden oder offline.");
            return true;
        }
        player.openInventory(target.getInventory());
        player.sendMessage("§aInventar von §e" + target.getName() + " §ageöffnet.");
        return true;
    }
}
