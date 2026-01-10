package com.bankassets.minecraftplugin;

import com.bankassets.minecraftplugin.commands.EnderchestCommand;
import com.bankassets.minecraftplugin.commands.InvseeCommand;
import com.bankassets.minecraftplugin.commands.VanishCommand;
import com.bankassets.minecraftplugin.commands.CommandFeedbackCommand;
import java.util.HashSet;
import java.util.Set;
import java.util.UUID;
import org.bukkit.Bukkit;
import org.bukkit.GameRule;
import org.bukkit.World;
import org.bukkit.entity.Player;
import org.bukkit.plugin.PluginManager;
import org.bukkit.plugin.java.JavaPlugin;

public class Main extends JavaPlugin {

    private final Set<UUID> vanishedPlayers = new HashSet<>();

    @Override
    public void onEnable() {
        saveDefaultConfig();
        applyCommandFeedbackSetting(getConfig().getBoolean("command-feedback", true));
        registerCommands();
        PluginManager pluginManager = getServer().getPluginManager();
        pluginManager.registerEvents(new PlayerVisibilityListener(this), this);
        pluginManager.registerEvents(new SleepListener(this), this);
    }

    @Override
    public void onDisable() {
        for (UUID uuid : new HashSet<>(vanishedPlayers)) {
            Player player = getServer().getPlayer(uuid);
            if (player != null) {
                setVanished(player, false);
            }
        }
        vanishedPlayers.clear();
    }

    private void registerCommands() {
        if (getCommand("invsee") != null) {
            getCommand("invsee").setExecutor(new InvseeCommand());
        }
        if (getCommand("enderchest") != null) {
            getCommand("enderchest").setExecutor(new EnderchestCommand());
        }
        if (getCommand("vanish") != null) {
            getCommand("vanish").setExecutor(new VanishCommand(this));
        }
        if (getCommand("commandfeedback") != null) {
            getCommand("commandfeedback").setExecutor(new CommandFeedbackCommand(this));
        }
    }

    void setVanished(Player player, boolean vanish) {
        if (vanish) {
            vanishedPlayers.add(player.getUniqueId());
            for (Player online : Bukkit.getOnlinePlayers()) {
                if (!online.equals(player)) {
                    online.hidePlayer(this, player);
                }
            }
        } else {
            vanishedPlayers.remove(player.getUniqueId());
            for (Player online : Bukkit.getOnlinePlayers()) {
                if (!online.equals(player)) {
                    online.showPlayer(this, player);
                }
            }
        }
    }

    boolean isVanished(Player player) {
        return vanishedPlayers.contains(player.getUniqueId());
    }

    boolean toggleCommandFeedback() {
        boolean enabled = !getConfig().getBoolean("command-feedback", true);
        getConfig().set("command-feedback", enabled);
        saveConfig();
        applyCommandFeedbackSetting(enabled);
        return enabled;
    }

    private void applyCommandFeedbackSetting(boolean enabled) {
        for (World world : Bukkit.getWorlds()) {
            world.setGameRule(GameRule.SEND_COMMAND_FEEDBACK, enabled);
        }
    }
}
