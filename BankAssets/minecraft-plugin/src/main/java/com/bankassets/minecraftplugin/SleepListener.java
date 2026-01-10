package com.bankassets.minecraftplugin;

import java.util.List;
import org.bukkit.World;
import org.bukkit.entity.Player;
import org.bukkit.event.EventHandler;
import org.bukkit.event.Listener;
import org.bukkit.event.player.PlayerBedEnterEvent;

public class SleepListener implements Listener {

    private final Main plugin;

    public SleepListener(Main plugin) {
        this.plugin = plugin;
    }

    @EventHandler
    public void onPlayerBedEnter(PlayerBedEnterEvent event) {
        if (event.getBedEnterResult() != PlayerBedEnterEvent.BedEnterResult.OK) {
            return;
        }
        plugin.getServer().getScheduler().runTask(plugin, () -> trySkipNight(event.getPlayer().getWorld()));
    }

    private void trySkipNight(World world) {
        List<Player> players = world.getPlayers();
        if (players.isEmpty()) {
            return;
        }
        long sleepers = players.stream().filter(Player::isSleeping).count();
        double ratio = (double) sleepers / players.size();
        if (ratio >= 0.5d) {
            world.setTime(0L);
            world.setStorm(false);
            world.setThundering(false);
            for (Player player : players) {
                if (player.isSleeping()) {
                    player.wakeup(true);
                }
            }
            world.getPlayers().forEach(p -> p.sendMessage("§a50% der Spieler schlafen – Nacht wird übersprungen."));
        }
    }
}
