using DedicatedServer.Chat;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;

namespace DedicatedServer.Pgsm
{
    /// <summary>
    /// PGSM structured logging. Emits single-line, machine-parseable [PGSM] lines
    /// via the SMAPI monitor for consumption by PowerGSM's log parse rules.
    /// See PGSM_CHANGES.md.
    /// </summary>
    internal static class PgsmLog
    {
        private static IMonitor monitor;
        private static IModHelper helper;
        private static EventDrivenChatBox chatBox;
        private static bool enabled = false;
        private static bool inviteCodeLogged = false;

        public static void Enable(IModHelper modHelper, IMonitor modMonitor, EventDrivenChatBox box)
        {
            if (enabled) { return; }
            enabled = true;

            helper = modHelper;
            monitor = modMonitor;
            chatBox = box;

            helper.Events.Multiplayer.PeerConnected += OnPeerConnected;
            helper.Events.Multiplayer.PeerDisconnected += OnPeerDisconnected;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.UpdateTicked += PollInviteCode;
            if (chatBox != null)
            {
                chatBox.ChatReceived += OnChatReceived;
            }

            Emit($"READY farm=\"{Escape(Game1.MasterPlayer?.farmName.Value)}\"");
        }

        public static void Disable()
        {
            if (!enabled) { return; }
            enabled = false;

            helper.Events.Multiplayer.PeerConnected -= OnPeerConnected;
            helper.Events.Multiplayer.PeerDisconnected -= OnPeerDisconnected;
            helper.Events.GameLoop.DayStarted -= OnDayStarted;
            helper.Events.GameLoop.UpdateTicked -= PollInviteCode;
            if (chatBox != null)
            {
                chatBox.ChatReceived -= OnChatReceived;
            }

            chatBox = null;
            inviteCodeLogged = false;
        }

        private static void OnPeerConnected(object sender, PeerConnectedEventArgs e)
        {
            Emit($"JOIN name=\"{Escape(FarmerName(e.Peer.PlayerID))}\" id=\"{e.Peer.PlayerID}\"");
        }

        private static void OnPeerDisconnected(object sender, PeerDisconnectedEventArgs e)
        {
            Emit($"LEAVE name=\"{Escape(FarmerName(e.Peer.PlayerID))}\" id=\"{e.Peer.PlayerID}\"");
        }

        private static void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            Emit($"DAY season=\"{Game1.currentSeason}\" day=\"{Game1.dayOfMonth}\" year=\"{Game1.year}\"");
        }

        private static void OnChatReceived(object sender, ChatEventArgs e)
        {
            // ChatKind: 0 = normal, 1 = error/notification, 2 = user notification, 3 = private message
            Emit($"CHAT name=\"{Escape(FarmerName(e.SourceFarmerId))}\" id=\"{e.SourceFarmerId}\" kind=\"{e.ChatKind}\" msg=\"{Escape(e.Message)}\"");
        }

        /// <summary>
        /// Invite codes are generated some time after world load (see ModEntry wait
        /// comment). Poll each tick until available, log once, stop polling.
        /// </summary>
        private static void PollInviteCode(object sender, UpdateTickedEventArgs e)
        {
            if (inviteCodeLogged)
            {
                helper.Events.GameLoop.UpdateTicked -= PollInviteCode;
                return;
            }

            string code = null;
            try
            {
                code = Game1.server?.getInviteCode();
            }
            catch (Exception)
            {
                // Steam/GOG networking unavailable (e.g. steamless server copy) — no code will ever appear.
                helper.Events.GameLoop.UpdateTicked -= PollInviteCode;
                return;
            }

            if (!string.IsNullOrEmpty(code))
            {
                inviteCodeLogged = true;
                helper.Events.GameLoop.UpdateTicked -= PollInviteCode;
                Emit($"INVITECODE code=\"{Escape(code)}\"");
            }
        }

        private static string FarmerName(long id)
        {
            try
            {
                var farmer = Game1.getFarmer(id);
                return farmer?.Name ?? "unknown";
            }
            catch (Exception)
            {
                return "unknown";
            }
        }

        private static string Escape(string value)
        {
            if (value == null) { return ""; }
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ");
        }

        private static void Emit(string line)
        {
            monitor?.Log($"[PGSM] {line}", LogLevel.Info);
        }
    }
}
