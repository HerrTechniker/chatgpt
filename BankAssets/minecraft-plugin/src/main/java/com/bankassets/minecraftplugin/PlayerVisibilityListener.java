package com.bankassets.minecraftplugin;

import org.bukkit.event.EventHandler;
import org.bukkit.event.Listener;
import org.bukkit.event.player.PlayerJoinEvent;

public class PlayerVisibilityListener implements Listener {

    private final Main plugin;

    public PlayerVisibilityListener(Main plugin) {
        this.plugin = plugin;
    }

    @EventHandler
    public void onPlayerJoin(PlayerJoinEvent event) {
        for (var player : event.getPlayer().getServer().getOnlinePlayers()) {
            if (plugin.isVanished(player)) {
                event.getPlayer().hidePlayer(plugin, player);
            }
        }
    }
}
