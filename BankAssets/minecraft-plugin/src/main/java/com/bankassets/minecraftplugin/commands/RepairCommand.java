package com.bankassets.minecraftplugin.commands;

import com.bankassets.minecraftplugin.Main;
import org.bukkit.Material;
import org.bukkit.command.Command;
import org.bukkit.command.CommandExecutor;
import org.bukkit.command.CommandSender;
import org.bukkit.entity.Player;
import org.bukkit.inventory.ItemStack;
import org.bukkit.inventory.meta.Damageable;

public class RepairCommand implements CommandExecutor {

    private final Main plugin;

    public RepairCommand(Main plugin) {
        this.plugin = plugin;
    }

    @Override
    public boolean onCommand(CommandSender sender, Command command, String label, String[] args) {
        if (!(sender instanceof Player player)) {
            sender.sendMessage("Dieses Kommando ist nur für Spieler.");
            return true;
        }
        if (!player.hasPermission("bankassets.repair")) {
            plugin.sendFeedback(player, "§cDu hast keine Berechtigung dafür.");
            return true;
        }
        ItemStack item = player.getInventory().getItemInMainHand();
        if (item.getType() == Material.AIR) {
            plugin.sendFeedback(player, "§cDu musst ein Item in der Hand halten.");
            return true;
        }
        if (!(item.getItemMeta() instanceof Damageable damageable)) {
            plugin.sendFeedback(player, "§cDieses Item kann nicht repariert werden.");
            return true;
        }
        damageable.setDamage(0);
        item.setItemMeta(damageable);
        plugin.sendFeedback(player, "§aDas Item wurde repariert.");
        return true;
    }
}
