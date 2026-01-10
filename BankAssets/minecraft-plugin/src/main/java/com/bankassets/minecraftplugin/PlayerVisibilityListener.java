package com.bankassets.minecraftplugin;

import org.bukkit.event.EventHandler;
import org.bukkit.event.Listener;
import org.bukkit.event.player.PlayerJoinEvent;
import org.bukkit.event.player.PlayerQuitEvent;

public class PlayerVisibilityListener implements Listener {

    private final Main plugin;

    public PlayerVisibilityListener(Main plugin) {
        this.plugin = plugin;
    }

    @EventHandler
    public void onPlayerJoin(PlayerJoinEvent event) {
        event.setJoinMessage("§8[§a+§8]§7 " + event.getPlayer().getName());
        for (var player : event.getPlayer().getServer().getOnlinePlayers()) {
            if (plugin.isVanished(player)) {
                event.getPlayer().hidePlayer(plugin, player);
            }
        }
    }

    @EventHandler
    public void onPlayerQuit(PlayerQuitEvent event) {
        event.setQuitMessage("§8[§4-§8]§7 " + event.getPlayer().getName());
    }
}
