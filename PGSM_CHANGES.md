# PGSM_CHANGES

Delta vs upstream (Chris82111/SMAPIDedicatedServerMod). One entry per change, newest first. Used for rebase conflict triage.

## Unreleased

- (none yet — baseline = upstream as forked, verified building SMAPI 4.5.2 / SDV 1.6.15)

## Verified environments

- Windows 11 desktop (dev box): builds, hosts, same-machine co-op join via 127.0.0.1 (steamless server copy via steam_appid.txt rename)
- Windows 10 ProLiant (no GPU): requires mesa llvmpipe (opengl32.dll + libgallium_wgl.dll beside game exe) + GALLIUM_DRIVER=llvmpipe env var; hosts + LAN join OK. Known issue: mod's window minimize doesn't fire in this environment (planned fix: explicit ShowWindow SW_MINIMIZE, see F3 item 0 in PowerGSM SMAPI_Fork_Plan.md)
