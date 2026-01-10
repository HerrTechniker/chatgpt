package com.bankassets.minecraftplugin.commands;

import com.bankassets.minecraftplugin.Main;
import org.bukkit.command.Command;
import org.bukkit.command.CommandExecutor;
import org.bukkit.command.CommandSender;

public class CommandFeedbackCommand implements CommandExecutor {

    private final Main plugin;

    public CommandFeedbackCommand(Main plugin) {
        this.plugin = plugin;
    }

    @Override
    public boolean onCommand(CommandSender sender, Command command, String label, String[] args) {
        if (!sender.hasPermission("bankassets.commandfeedback")) {
            sendFeedback(sender, "§cDu hast keine Berechtigung dafür.");
            return true;
        }
        boolean enabled = plugin.toggleCommandFeedback();
        if (enabled) {
            sendFeedback(sender, "§aCommandrückmeldungen wurden aktiviert.");
        } else {
            sendFeedback(sender, "§aCommandrückmeldungen wurden deaktiviert.");
        }
        return true;
    }

    private void sendFeedback(CommandSender sender, String message) {
        if (sender instanceof org.bukkit.entity.Player player) {
            plugin.sendFeedback(player, message);
        } else {
            sender.sendMessage(message);
        }
    }
}
