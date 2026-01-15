package com.bankassets.minecraftplugin.commands;

import com.bankassets.minecraftplugin.Main;
import org.bukkit.Bukkit;
import org.bukkit.OfflinePlayer;
import org.bukkit.command.Command;
import org.bukkit.command.CommandExecutor;
import org.bukkit.command.CommandSender;

public class CommandAccessCommand implements CommandExecutor {

    private final Main plugin;

    public CommandAccessCommand(Main plugin) {
        this.plugin = plugin;
    }

    @Override
    public boolean onCommand(CommandSender sender, Command command, String label, String[] args) {
        if (sender != Bukkit.getConsoleSender()) {
            sender.sendMessage("Dieses Kommando ist nur für die Konsole.");
            return true;
        }
        if (args.length != 3) {
            sender.sendMessage("Benutzung: /commandaccess <invsee|enderchest> <add|remove> <spieler>");
            return true;
        }
        String listKey = resolveListKey(args[0], sender);
        if (listKey == null) {
            return true;
        }
        boolean isAdd = resolveAction(args[1], sender);
        if (!isAdd && !"remove".equalsIgnoreCase(args[1])) {
            return true;
        }
        OfflinePlayer offlinePlayer = Bukkit.getOfflinePlayer(args[2]);
        if (!offlinePlayer.hasPlayedBefore() && !offlinePlayer.isOnline()) {
            sender.sendMessage("Spieler nicht gefunden oder nie online gewesen.");
            return true;
        }
        boolean changed = isAdd
                ? plugin.addAllowedPlayer(listKey, offlinePlayer.getUniqueId())
                : plugin.removeAllowedPlayer(listKey, offlinePlayer.getUniqueId());
        if (!changed) {
            sender.sendMessage("Keine Änderung vorgenommen (bereits vorhanden/entfernt).");
            return true;
        }
        sender.sendMessage("Eintrag aktualisiert für " + offlinePlayer.getName() + ".");
        return true;
    }

    private String resolveListKey(String input, CommandSender sender) {
        if ("invsee".equalsIgnoreCase(input)) {
            return "invsee-allowed";
        }
        if ("enderchest".equalsIgnoreCase(input)) {
            return "enderchest-allowed";
        }
        sender.sendMessage("Unbekannter Befehlstyp. Nutze invsee oder enderchest.");
        return null;
    }

    private boolean resolveAction(String input, CommandSender sender) {
        if ("add".equalsIgnoreCase(input)) {
            return true;
        }
        if ("remove".equalsIgnoreCase(input)) {
            return false;
        }
        sender.sendMessage("Unbekannte Aktion. Nutze add oder remove.");
        return false;
    }
}
