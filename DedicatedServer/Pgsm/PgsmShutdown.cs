using DedicatedServer.HostAutomatorStages;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Threading;

namespace DedicatedServer.Pgsm
{
    /// <summary>
    /// PGSM graceful shutdown. Handles Ctrl+C / SIGINT (both OSes; .NET maps
    /// SIGINT to Console.CancelKeyPress on Linux) by saving the game via the
    /// existing RestartDayWorker sleep-save path, then quitting.
    /// Day is preserved (keepsCurrentDay) so a stop/start cycle does not
    /// advance the calendar. See PGSM_CHANGES.md.
    /// </summary>
    internal static class PgsmShutdown
    {
        private static int requested = 0; // interlocked flag; signal handler thread -> game thread
        private static bool enabled = false;
        private static bool worldReady = false;

        public static void Enable()
        {
            if (enabled) { return; }
            enabled = true;
            worldReady = true;

            Console.CancelKeyPress += OnCancelKeyPress;
            MainController.helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        public static void Disable()
        {
            if (!enabled) { return; }
            enabled = false;
            worldReady = false;

            Console.CancelKeyPress -= OnCancelKeyPress;
            MainController.helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
        }

        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            // Cancel default immediate termination; request graceful save+exit.
            // Second Ctrl+C while shutdown is pending falls through to default
            // handling (immediate exit) as an escape hatch.
            if (Interlocked.Exchange(ref requested, 1) == 0)
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Runs on the game thread. Picks up the shutdown request flag set by
        /// the console signal handler thread.
        /// </summary>
        private static void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (Volatile.Read(ref requested) == 0) { return; }

            MainController.helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

            if (!worldReady || !Context.IsWorldReady)
            {
                MainController.monitor?.Log("[PGSM] STOPPING mode=\"immediate\"", LogLevel.Info);
                Environment.Exit(0);
                return;
            }

            MainController.monitor?.Log("[PGSM] STOPPING mode=\"save\"", LogLevel.Info);

            // Kick players, sleep-save on the current day, then quit after Saved fires.
            RestartDayWorker.SavesGameRestartsDay(
                time: 0,
                keepsCurrentDay: true,
                quit: true,
                action: null);
        }
    }
}
