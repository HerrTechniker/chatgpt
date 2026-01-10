package com.bankassets.minecraftplugin;

import com.destroystokyo.paper.event.server.PaperServerListPingEvent;
import java.lang.reflect.Method;
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
        if (event instanceof PaperServerListPingEvent) {
            return;
        }
        int onlinePlayers = Bukkit.getOnlinePlayers().size();
        int visiblePlayers = Math.max(0, onlinePlayers - plugin.getVanishedCount());
        setPlayerCountIfSupported(event, visiblePlayers);
    }

    @EventHandler
    public void onPaperServerListPing(PaperServerListPingEvent event) {
        int onlinePlayers = Bukkit.getOnlinePlayers().size();
        int visiblePlayers = Math.max(0, onlinePlayers - plugin.getVanishedCount());
        event.setNumPlayers(visiblePlayers);
    }

    private void setPlayerCountIfSupported(ServerListPingEvent event, int visiblePlayers) {
        try {
            Method setNumPlayers = event.getClass().getMethod("setNumPlayers", int.class);
            setNumPlayers.invoke(event, visiblePlayers);
        } catch (ReflectiveOperationException ignored) {
            // Spigot does not expose a setter for the visible player count.
        }
    }
}
