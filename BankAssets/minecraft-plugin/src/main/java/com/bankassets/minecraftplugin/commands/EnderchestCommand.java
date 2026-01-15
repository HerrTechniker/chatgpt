package com.bankassets.minecraftplugin.commands;

import com.bankassets.minecraftplugin.Main;
import java.lang.reflect.Method;
import org.bukkit.Bukkit;
import org.bukkit.OfflinePlayer;
import org.bukkit.command.Command;
import org.bukkit.command.CommandExecutor;
import org.bukkit.command.CommandSender;
import org.bukkit.entity.Player;
import org.bukkit.inventory.Inventory;

public class EnderchestCommand implements CommandExecutor {

    private final Main plugin;

    public EnderchestCommand(Main plugin) {
        this.plugin = plugin;
    }

    @Override
    public boolean onCommand(CommandSender sender, Command command, String label, String[] args) {
        if (!(sender instanceof Player player)) {
            sender.sendMessage("Dieses Kommando ist nur für Spieler.");
            return true;
        }
        if (!player.hasPermission("bankassets.enderchest")) {
            plugin.sendFeedback(player, "§cDu hast keine Berechtigung dafür.");
            return true;
        }
        if (args.length == 0) {
            player.openInventory(player.getEnderChest());
            plugin.sendFeedback(player, "§aDeine Enderchest wurde geöffnet.");
            return true;
        }
        if (!plugin.isEnderchestAllowed(player)) {
            plugin.sendFeedback(player, "§cDu darfst die Enderchest anderer Spieler nicht öffnen.");
            return true;
        }
        if (args.length == 1) {
            Player target = Bukkit.getPlayerExact(args[0]);
            if (target != null) {
                player.openInventory(target.getEnderChest());
                plugin.sendFeedback(player, "§aEnderchest von §e" + target.getName() + " §ageöffnet.");
                return true;
            }
            OfflinePlayer offlinePlayer = Bukkit.getOfflinePlayer(args[0]);
            if (!offlinePlayer.hasPlayedBefore()) {
                plugin.sendFeedback(player, "§cSpieler nicht gefunden oder offline.");
                return true;
            }
            Inventory enderChest = getOfflineEnderChest(offlinePlayer);
            if (enderChest == null) {
                plugin.sendFeedback(player, "§cDie Enderchest von Offline-Spielern ist auf diesem Server nicht verfügbar.");
                return true;
            }
            player.openInventory(enderChest);
            plugin.sendFeedback(player, "§aEnderchest von §e" + offlinePlayer.getName() + " §ageöffnet.");
            return true;
        }
        plugin.sendFeedback(player, "§cBenutzung: /enderchest [spieler]");
        return true;
    }

    private Inventory getOfflineEnderChest(OfflinePlayer offlinePlayer) {
        try {
            Method method = offlinePlayer.getClass().getMethod("getEnderChest");
            Object result = method.invoke(offlinePlayer);
            if (result instanceof Inventory inventory) {
                return inventory;
            }
        } catch (ReflectiveOperationException ignored) {
            return null;
        }
        return null;
    }
}
