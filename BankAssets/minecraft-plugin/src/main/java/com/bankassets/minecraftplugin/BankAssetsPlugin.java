package com.bankassets.minecraftplugin;

import java.util.HashSet;
import java.util.Set;
import java.util.UUID;
import org.bukkit.Bukkit;
import org.bukkit.command.Command;
import org.bukkit.command.CommandSender;
import org.bukkit.command.PluginCommand;
import org.bukkit.entity.Player;
import org.bukkit.plugin.java.JavaPlugin;

public class BankAssetsPlugin extends JavaPlugin {

    private final Set<UUID> vanishedPlayers = new HashSet<>();

    @Override
    public void onEnable() {
        registerCommand("invsee");
        registerCommand("endersee");
        registerCommand("enderchest");
        registerCommand("vanish");
        getServer().getPluginManager().registerEvents(new PlayerVisibilityListener(this), this);
        getServer().getPluginManager().registerEvents(new SleepListener(this), this);
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

    private void registerCommand(String name) {
        PluginCommand command = getCommand(name);
        if (command != null) {
            command.setExecutor(this);
        }
    }

    @Override
    public boolean onCommand(CommandSender sender, Command command, String label, String[] args) {
        if (!(sender instanceof Player player)) {
            sender.sendMessage("Dieses Kommando ist nur für Spieler.");
            return true;
        }

        return switch (command.getName().toLowerCase()) {
            case "invsee" -> handleInvsee(player, args);
            case "endersee" -> handleEndersee(player, args);
            case "enderchest" -> handleEnderchest(player);
            case "vanish" -> handleVanish(player);
            default -> false;
        };
    }

    private boolean handleInvsee(Player player, String[] args) {
        if (!player.hasPermission("bankassets.invsee")) {
            player.sendMessage("§cDu hast keine Berechtigung dafür.");
            return true;
        }
        if (args.length != 1) {
            player.sendMessage("§cBenutzung: /invsee <spieler>");
            return true;
        }
        Player target = Bukkit.getPlayerExact(args[0]);
        if (target == null) {
            player.sendMessage("§cSpieler nicht gefunden oder offline.");
            return true;
        }
        player.openInventory(target.getInventory());
        player.sendMessage("§aInventar von §e" + target.getName() + " §ageöffnet.");
        return true;
    }

    private boolean handleEndersee(Player player, String[] args) {
        if (!player.hasPermission("bankassets.endersee")) {
            player.sendMessage("§cDu hast keine Berechtigung dafür.");
            return true;
        }
        if (args.length != 1) {
            player.sendMessage("§cBenutzung: /endersee <spieler>");
            return true;
        }
        Player target = Bukkit.getPlayerExact(args[0]);
        if (target == null) {
            player.sendMessage("§cSpieler nicht gefunden oder offline.");
            return true;
        }
        player.openInventory(target.getEnderChest());
        player.sendMessage("§aEnderchest von §e" + target.getName() + " §ageöffnet.");
        return true;
    }

    private boolean handleEnderchest(Player player) {
        if (!player.hasPermission("bankassets.enderchest")) {
            player.sendMessage("§cDu hast keine Berechtigung dafür.");
            return true;
        }
        player.openInventory(player.getEnderChest());
        player.sendMessage("§aDeine Enderchest wurde geöffnet.");
        return true;
    }

    private boolean handleVanish(Player player) {
        if (!player.hasPermission("bankassets.vanish")) {
            player.sendMessage("§cDu hast keine Berechtigung dafür.");
            return true;
        }
        boolean shouldVanish = !vanishedPlayers.contains(player.getUniqueId());
        setVanished(player, shouldVanish);
        if (shouldVanish) {
            player.sendMessage("§aDu bist jetzt unsichtbar.");
        } else {
            player.sendMessage("§aDu bist wieder sichtbar.");
        }
        return true;
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
}
