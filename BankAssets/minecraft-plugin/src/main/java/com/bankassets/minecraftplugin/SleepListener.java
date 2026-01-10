package com.bankassets.minecraftplugin;

import java.util.List;
import org.bukkit.GameRule;
import org.bukkit.World;
import org.bukkit.entity.Player;
import org.bukkit.event.EventHandler;
import org.bukkit.event.Listener;
import org.bukkit.event.player.PlayerBedEnterEvent;
import net.md_5.bungee.api.ChatMessageType;
import net.md_5.bungee.api.TextComponent;

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
        plugin.getServer().getScheduler().runTask(plugin, () -> notifySleepStatus(event.getPlayer().getWorld()));
    }

    private void notifySleepStatus(World world) {
        world.setGameRule(GameRule.PLAYERS_SLEEPING_PERCENTAGE, 50);
        List<Player> players = world.getPlayers();
        if (players.isEmpty()) {
            return;
        }
        long sleepers = players.stream().filter(Player::isSleeping).count();
        int requiredSleepers = (int) Math.ceil(players.size() * 0.5d);
        int missing = Math.max(0, requiredSleepers - (int) sleepers);
        String message = missing == 0
                ? "Sleeping through this night"
                : sleepers + "/" + players.size() + " players sleeping";
        world.getPlayers().forEach(player -> player.spigot().sendMessage(
                ChatMessageType.ACTION_BAR,
                TextComponent.fromLegacyText(message)));
    }
}
