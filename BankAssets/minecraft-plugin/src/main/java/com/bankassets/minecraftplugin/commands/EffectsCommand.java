package com.bankassets.minecraftplugin.commands;

import com.bankassets.minecraftplugin.Main;
import java.util.Comparator;
import java.util.stream.Collectors;
import org.bukkit.Bukkit;
import org.bukkit.command.Command;
import org.bukkit.command.CommandExecutor;
import org.bukkit.command.CommandSender;
import org.bukkit.entity.Player;
import org.bukkit.potion.PotionEffect;

public class EffectsCommand implements CommandExecutor {

    private final Main plugin;

    public EffectsCommand(Main plugin) {
        this.plugin = plugin;
    }

    @Override
    public boolean onCommand(CommandSender sender, Command command, String label, String[] args) {
        if (!(sender instanceof Player player)) {
            sender.sendMessage("Dieses Kommando ist nur für Spieler.");
            return true;
        }
        if (!player.hasPermission("bankassets.effects")) {
            plugin.sendFeedback(player, "§cDu hast keine Berechtigung dafür.");
            return true;
        }
        if (!plugin.isEffectsAllowed(player)) {
            plugin.sendFeedback(player, "§cDu darfst diesen Command nicht benutzen.");
            return true;
        }
        if (args.length != 1) {
            plugin.sendFeedback(player, "§cBenutzung: /effects <spieler>");
            return true;
        }
        Player target = Bukkit.getPlayerExact(args[0]);
        if (target == null) {
            plugin.sendFeedback(player, "§cSpieler nicht gefunden oder offline.");
            return true;
        }
        if (target.getActivePotionEffects().isEmpty()) {
            plugin.sendFeedback(player, "§a" + target.getName() + " hat keine aktiven Effekte.");
            return true;
        }
        String effects = target.getActivePotionEffects().stream()
                .sorted(Comparator.comparing(effect -> effect.getType().getKey().getKey()))
                .map(this::formatEffect)
                .collect(Collectors.joining("§7, §e"));
        plugin.sendFeedback(player, "§aEffekte von §e" + target.getName() + "§a: §e" + effects);
        return true;
    }

    private String formatEffect(PotionEffect effect) {
        String name = effect.getType().getKey().getKey().replace('_', ' ');
        int level = effect.getAmplifier() + 1;
        int seconds = effect.getDuration() / 20;
        return name + " " + level + " (" + seconds + "s)";
    }
}
