package com.bankassets.minecraftplugin.commands;

import com.bankassets.minecraftplugin.Main;
import org.bukkit.Material;
import org.bukkit.command.Command;
import org.bukkit.command.CommandExecutor;
import org.bukkit.command.CommandSender;
import org.bukkit.entity.Player;
import org.bukkit.inventory.ItemStack;
import org.bukkit.inventory.meta.Repairable;

public class RepairResetCommand implements CommandExecutor {

    private final Main plugin;

    public RepairResetCommand(Main plugin) {
        this.plugin = plugin;
    }

    @Override
    public boolean onCommand(CommandSender sender, Command command, String label, String[] args) {
        if (!(sender instanceof Player player)) {
            sender.sendMessage("Dieses Kommando ist nur für Spieler.");
            return true;
        }
        if (!player.hasPermission("bankassets.repairreset")) {
            plugin.sendFeedback(player, "§cDu hast keine Berechtigung dafür.");
            return true;
        }
        if (args.length != 0) {
            plugin.sendFeedback(player, "§cBenutzung: /repairreset");
            return true;
        }
        ItemStack item = player.getInventory().getItemInMainHand();
        if (item.getType() == Material.AIR) {
            plugin.sendFeedback(player, "§cDu musst ein Item in der Hand halten.");
            return true;
        }
        if (!(item.getItemMeta() instanceof Repairable repairable)) {
            plugin.sendFeedback(player, "§cDieses Item hat keine Reparaturkosten.");
            return true;
        }
        if (repairable.getRepairCost() == 0) {
            plugin.sendFeedback(player, "§eKeine Reparaturkosten zum Entfernen gefunden.");
            return true;
        }
        repairable.setRepairCost(0);
        item.setItemMeta(repairable);
        plugin.sendFeedback(player, "§aReparaturkosten entfernt.");
        return true;
    }
}
