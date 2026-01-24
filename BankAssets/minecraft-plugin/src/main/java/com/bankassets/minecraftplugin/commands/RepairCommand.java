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
        if (args.length > 1) {
            plugin.sendFeedback(player, "§cBenutzung: /repair [all]");
            return true;
        }
        if (args.length == 1) {
            if (!"all".equalsIgnoreCase(args[0])) {
                plugin.sendFeedback(player, "§cBenutzung: /repair [all]");
                return true;
            }
            int repaired = repairInventory(player);
            if (repaired == 0) {
                plugin.sendFeedback(player, "§cKeine Items zum Reparieren gefunden.");
                return true;
            }
            plugin.sendFeedback(player, "§a" + repaired + " Items wurden repariert.");
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
        if (damageable.getDamage() == 0) {
            plugin.sendFeedback(player, "§eDieses Item ist bereits vollständig repariert.");
            return true;
        }
        damageable.setDamage(0);
        item.setItemMeta(damageable);
        plugin.sendFeedback(player, "§aDas Item wurde repariert.");
        return true;
    }

    private int repairInventory(Player player) {
        int repaired = 0;
        ItemStack[] contents = player.getInventory().getContents();
        for (int i = 0; i < contents.length; i++) {
            ItemStack item = contents[i];
            if (repairItem(item)) {
                repaired++;
            }
        }
        return repaired;
    }

    private boolean repairItem(ItemStack item) {
        if (item == null || item.getType() == Material.AIR) {
            return false;
        }
        if (!(item.getItemMeta() instanceof Damageable damageable)) {
            return false;
        }
        if (damageable.getDamage() == 0) {
            return false;
        }
        damageable.setDamage(0);
        item.setItemMeta(damageable);
        return true;
    }
}
