package com.bankassets.minecraftplugin;

import io.papermc.paper.event.server.PaperServerListPingEvent;
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
        int visiblePlayers = (int) Bukkit.getOnlinePlayers().stream()
                .filter(player -> !plugin.isVanished(player))
                .count();
        setPlayerCountIfSupported(event, visiblePlayers);
    }

    @EventHandler
    public void onPaperServerListPing(PaperServerListPingEvent event) {
        int visiblePlayers = (int) Bukkit.getOnlinePlayers().stream()
                .filter(player -> !plugin.isVanished(player))
                .count();
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
