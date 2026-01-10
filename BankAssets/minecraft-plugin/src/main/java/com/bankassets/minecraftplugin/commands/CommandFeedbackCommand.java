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
            sender.sendMessage("§cDu hast keine Berechtigung dafür.");
            return true;
        }
        boolean enabled = plugin.toggleCommandFeedback();
        if (enabled) {
            sender.sendMessage("§aCommandrückmeldungen wurden aktiviert.");
        } else {
            sender.sendMessage("§aCommandrückmeldungen wurden deaktiviert.");
        }
        return true;
    }
}
