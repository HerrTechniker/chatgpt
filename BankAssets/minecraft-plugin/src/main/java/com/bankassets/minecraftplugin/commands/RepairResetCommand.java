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
        boolean removedComponent = unsetRepairCostComponent(item);
        boolean changed = false;
        if (repairable.getRepairCost() != 0) {
            repairable.setRepairCost(0);
            item.setItemMeta(repairable);
            changed = true;
        }
        if (!changed && !removedComponent) {
            plugin.sendFeedback(player, "§eKeine Reparaturkosten zum Entfernen gefunden.");
            return true;
        }
        plugin.sendFeedback(player, "§aReparaturkosten entfernt.");
        return true;
    }

    private boolean unsetRepairCostComponent(ItemStack item) {
        try {
            Class<?> componentTypes = Class.forName("io.papermc.paper.datacomponent.DataComponentTypes");
            Object repairCostType = componentTypes.getField("REPAIR_COST").get(null);
            Class<?> componentTypeClass = Class.forName("io.papermc.paper.datacomponent.DataComponentType");
            var unsetData = item.getClass().getMethod("unsetData", componentTypeClass);
            unsetData.invoke(item, repairCostType);
            return true;
        } catch (ReflectiveOperationException ignored) {
            return false;
        }
    }
}
