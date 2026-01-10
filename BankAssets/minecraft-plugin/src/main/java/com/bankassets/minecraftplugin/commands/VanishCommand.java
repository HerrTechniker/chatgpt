package com.bankassets.minecraftplugin.commands;

import com.bankassets.minecraftplugin.Main;
import org.bukkit.command.Command;
import org.bukkit.command.CommandExecutor;
import org.bukkit.command.CommandSender;
import org.bukkit.entity.Player;

public class VanishCommand implements CommandExecutor {

    private final Main plugin;

    public VanishCommand(Main plugin) {
        this.plugin = plugin;
    }

    @Override
    public boolean onCommand(CommandSender sender, Command command, String label, String[] args) {
        if (!(sender instanceof Player player)) {
            sender.sendMessage("Dieses Kommando ist nur für Spieler.");
            return true;
        }
        if (!player.hasPermission("bankassets.vanish")) {
            plugin.sendFeedback(player, "§cDu hast keine Berechtigung dafür.");
            return true;
        }
        boolean shouldVanish = !plugin.isVanished(player);
        plugin.setVanished(player, shouldVanish);
        if (shouldVanish) {
            plugin.sendFeedback(player, "§aDu bist jetzt unsichtbar.");
        } else {
            plugin.sendFeedback(player, "§aDu bist wieder sichtbar.");
        }
        return true;
    }
}
