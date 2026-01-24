package com.bankassets.minecraftplugin;

import com.bankassets.minecraftplugin.commands.CommandAccessCommand;
import com.bankassets.minecraftplugin.commands.CommandFeedbackCommand;
import com.bankassets.minecraftplugin.commands.EnderchestCommand;
import com.bankassets.minecraftplugin.commands.EffectsCommand;
import com.bankassets.minecraftplugin.commands.InvseeCommand;
import com.bankassets.minecraftplugin.commands.RepairCommand;
import com.bankassets.minecraftplugin.commands.RepairResetCommand;
import com.bankassets.minecraftplugin.commands.VanishCommand;
import java.util.HashSet;
import java.util.List;
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
        applySleepPercentageRule();
        registerCommands();
        PluginManager pluginManager = getServer().getPluginManager();
        pluginManager.registerEvents(new PlayerVisibilityListener(this), this);
        pluginManager.registerEvents(new ServerListPingListener(this), this);
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
            getCommand("invsee").setExecutor(new InvseeCommand(this));
        }
        if (getCommand("enderchest") != null) {
            getCommand("enderchest").setExecutor(new EnderchestCommand(this));
        }
        if (getCommand("vanish") != null) {
            getCommand("vanish").setExecutor(new VanishCommand(this));
        }
        if (getCommand("commandfeedback") != null) {
            getCommand("commandfeedback").setExecutor(new CommandFeedbackCommand(this));
        }
        if (getCommand("commandaccess") != null) {
            getCommand("commandaccess").setExecutor(new CommandAccessCommand(this));
        }
        if (getCommand("repair") != null) {
            getCommand("repair").setExecutor(new RepairCommand(this));
        }
        if (getCommand("effects") != null) {
            getCommand("effects").setExecutor(new EffectsCommand(this));
        }
        if (getCommand("repairreset") != null) {
            getCommand("repairreset").setExecutor(new RepairResetCommand(this));
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

    int getVanishedCount() {
        return vanishedPlayers.size();
    }

    boolean toggleVanish(Player player) {
        boolean shouldVanish = !isVanished(player);
        setVanished(player, shouldVanish);
        if (shouldVanish) {
            Bukkit.broadcastMessage("§8[§4-§8]§7 " + player.getName());
        } else {
            Bukkit.broadcastMessage("§8[§a+§8]§7 " + player.getName());
        }
        return shouldVanish;
    }

    boolean clearVanishOnQuit(Player player) {
        if (!isVanished(player)) {
            return false;
        }
        vanishedPlayers.remove(player.getUniqueId());
        return true;
    }

    boolean toggleCommandFeedback() {
        boolean enabled = !getConfig().getBoolean("command-feedback", true);
        getConfig().set("command-feedback", enabled);
        saveConfig();
        return enabled;
    }

    boolean isCommandFeedbackEnabled() {
        return getConfig().getBoolean("command-feedback", true);
    }

    boolean isInvseeAllowed(Player player) {
        return isPlayerAllowed("invsee-allowed", player.getUniqueId());
    }

    boolean isEnderchestAllowed(Player player) {
        return isPlayerAllowed("enderchest-allowed", player.getUniqueId());
    }

    boolean isEffectsAllowed(Player player) {
        return isPlayerAllowed("effects-allowed", player.getUniqueId());
    }

    boolean addAllowedPlayer(String key, UUID playerId) {
        List<String> allowed = getConfig().getStringList(key);
        String id = playerId.toString();
        if (allowed.contains(id)) {
            return false;
        }
        allowed.add(id);
        getConfig().set(key, allowed);
        saveConfig();
        return true;
    }

    boolean removeAllowedPlayer(String key, UUID playerId) {
        List<String> allowed = getConfig().getStringList(key);
        boolean removed = allowed.remove(playerId.toString());
        if (removed) {
            getConfig().set(key, allowed);
            saveConfig();
        }
        return removed;
    }

    private boolean isPlayerAllowed(String key, UUID playerId) {
        List<String> allowed = getConfig().getStringList(key);
        return allowed.contains(playerId.toString());
    }

    void sendFeedback(Player player, String message) {
        if (isCommandFeedbackEnabled()) {
            player.sendMessage(message);
        }
    }

    private void applySleepPercentageRule() {
        for (World world : Bukkit.getWorlds()) {
            world.setGameRule(GameRule.PLAYERS_SLEEPING_PERCENTAGE, 50);
        }
    }
}
