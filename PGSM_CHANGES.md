# PGSM_CHANGES

Delta vs upstream (Chris82111/SMAPIDedicatedServerMod). One entry per change, newest first. Used for rebase conflict triage.

## Unreleased

- B1 (2026-10-12, vs upstream v1.2.7): Fixed `Network/Port.cs` — upstream's custom-port feature never assigned `Port.ServerPort` from `config.Port`, so the Harmony prefix on `NetPeerConfiguration.Port` cast a null nullable and `LidgrenServer.initialize()` threw `InvalidOperationException: Nullable object must have a value` whenever `Port` was non-null (server never hosted). Now `ServerPort = config.Port` before patching; prefix also null-guards. Candidate upstream PR. Upstream merge of v1.2.7 was otherwise clean (new: `Network/Port.cs`, `Utils/Language.cs`, `i18n/`, `Language`/`PlayerLimit`/`ModFarmId` config keys).
- F3: Added `Pgsm/PgsmShutdown.cs` — graceful stop on Ctrl+C/SIGINT (Console.CancelKeyPress, cross-platform). Marshals to game thread via flag + UpdateTicked; saves via existing `RestartDayWorker.SavesGameRestartsDay(keepsCurrentDay:true, quit:true)` (kick players → sleep-save on same day → quit on Saved). Emits `[PGSM] STOPPING mode="save|immediate"`. Second Ctrl+C = immediate exit escape hatch. Wired in StartFarmStage alongside PgsmLog.
- F2: Added `Pgsm/PgsmLog.cs` — structured `[PGSM]` log lines (READY, JOIN, LEAVE, CHAT, DAY, INVITECODE) via SMAPI monitor. Wired in `StartFarmStage`: `PgsmLog.Enable(...)` at end of `OnSaveLoaded`, `PgsmLog.Disable()` in `Dispose()`. Values quoted, backslash/quote/newline escaped. Invite code polled per tick until available (codes generate late); poll self-cancels if Steam networking absent (steamless copies).

## Verified environments

- 2026-10-12, pgsm-v1.2.7 build (upstream 1.2.7 + B1): Windows 11 dev box, steamless (steam_appid.txt renamed), `"Port": 24700` — binds 24700 (netstat), client joined via 127.0.0.1:24700 and quit cleanly; JOIN/LEAVE/READY/DAY lines OK. Note: with the Steam client running AND steam_appid.txt present, Steam registers the server as "Stardew Valley — Running" and refuses to launch the regular client (one instance per appid); rename the file on a dev box. Invite code requires Steam, so steamless logs "Invitation code could not be received" (expected).

- Windows 11 desktop (dev box): builds, hosts, same-machine co-op join via 127.0.0.1 (steamless server copy via steam_appid.txt rename)
- Windows 10 ProLiant (no GPU): requires mesa llvmpipe (opengl32.dll + libgallium_wgl.dll beside game exe) + GALLIUM_DRIVER=llvmpipe env var; hosts + LAN join OK. Known issue: mod's window minimize doesn't fire in this environment (planned fix: explicit ShowWindow SW_MINIMIZE, see F3 item 0 in PowerGSM SMAPI_Fork_Plan.md)
