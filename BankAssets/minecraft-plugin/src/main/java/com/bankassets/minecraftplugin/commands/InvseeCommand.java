package com.bankassets.minecraftplugin.commands;

import com.bankassets.minecraftplugin.Main;
import org.bukkit.Bukkit;
import org.bukkit.command.Command;
import org.bukkit.command.CommandExecutor;
import org.bukkit.command.CommandSender;
import org.bukkit.entity.Player;

public class InvseeCommand implements CommandExecutor {

    private final Main plugin;

    public InvseeCommand(Main plugin) {
        this.plugin = plugin;
    }

    @Override
    public boolean onCommand(CommandSender sender, Command command, String label, String[] args) {
        if (!(sender instanceof Player player)) {
            sender.sendMessage("Dieses Kommando ist nur für Spieler.");
            return true;
        }
        if (!player.hasPermission("bankassets.invsee")) {
            plugin.sendFeedback(player, "§cDu hast keine Berechtigung dafür.");
            return true;
        }
        if (args.length != 1) {
            plugin.sendFeedback(player, "§cBenutzung: /invsee <spieler>");
            return true;
        }
        Player target = Bukkit.getPlayerExact(args[0]);
        if (target == null) {
            plugin.sendFeedback(player, "§cSpieler nicht gefunden oder offline.");
            return true;
        }
        player.openInventory(target.getInventory());
        plugin.sendFeedback(player, "§aInventar von §e" + target.getName() + " §ageöffnet.");
        return true;
    }
}
