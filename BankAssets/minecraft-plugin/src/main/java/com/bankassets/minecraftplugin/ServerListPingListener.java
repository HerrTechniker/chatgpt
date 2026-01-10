package com.bankassets.minecraftplugin;

import org.bukkit.Bukkit;
import org.bukkit.event.EventHandler;
import org.bukkit.event.Listener;
import org.bukkit.event.server.ServerListPingEvent;

public class ServerListPingListener implements Listener {

    private final Main plugin;

    public ServerListPingListener(Main plugin) {
        this.plugin = plugin;
    }

    @EventHandler
    public void onServerListPing(ServerListPingEvent event) {
        int onlinePlayers = Bukkit.getOnlinePlayers().size();
        int visiblePlayers = Math.max(0, onlinePlayers - plugin.getVanishedCount());
        event.setNumPlayers(visiblePlayers);
    }
}
