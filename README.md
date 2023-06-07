# FrostyToolsuite
The most advanced modding platform for games running on DICE's Frostbite game engine.

## maniman303-Fork
- Symlink fixed on Linux. When program is launched through Wine instead of using softlinks it will now use hardlinks. Behavior on Windows is unchanged.
- Disabled drag and drop mod installation when mod manager is run through Wine, as it had 75% chance of freezing the program. On Windows behavior is unchanged.
- Only tested mod manager, editor might still have issues.
- Fixed building plugins when path to project contains spaces - xcopy scripts were pissing proper formatting.
- Added new launch parameter to create direct-to-game shortcuts. Use it like this `FrostyModManager.exe -game MassEffectAndromeda -launch Default`. First option is game id and second is the profile to be used.

## How to find game id
1. Launch Frosty Mod Manager at least once and add your game to it.
2. Go to `%LocalAppData%\Frosty` and open `manager_config.json`.
3. Under `"Games":` you will have your game id.

## How to use on Linux
Well, I wont create any 100% guarantee tutorial as I don't have skill or time for that unfortunately, but I can share how I've setup this version of Frosty Mod Manager on my Steam Deck with Mass Effect Andromeda bought on Steam. For alternative you could try using Steam Tinker Launch.
1. Install the game via Steam.
2. Copy game files to the safe location like `/home/Games/Mass Effect Andromeda`.
3. Download dlls from [here](https://github.com/zeroKilo/MEAExplorerWV/tree/master/AnselSDK64/Release) and place them next to the game exe file, without them mods won't be loaded (Koaloader doesn't seem to work on Linux).
4. Quack the game - you will loose online functionality, but also you will gain offline playability.
5. (Optional) Uninstall game from Steam.
6. Install patched Frosty Mod Manager also in the safe location, like `/home/Games/Frosty Mod Manager`.
7. Install bottles from Discovery.
8. Make bottle using latest wine-ge-7.xx, latest dxvk and latest vkd3d.
9. From bottle dependencies install `wine mono`.
10. From bottle settings change synchronization to `fsync`.
11. Add shortcut to Frosty Mod Manager exe.

And from here you should be able to launch Frosty, add game exe to it, add mods and play it.
Optionally you can add direct game shortcut to Steam:
1. In bottles add another shortcut to Frosty, this time name it eg. *Mass Effect Andromeda*.
2. Edit shortcut and add to the launch parameters  `-game MassEffectAndromeda -launch Default`.
3. Add this shortcut to Steam.

Remember - you have to launch the game through Frosty without shortcut at least once for mods to install. Every time you add / remove a mod you need to launch the game without shortcut once again.

## Setup

1. Download Git https://git-scm.com/download/win.
2. Create an empty folder, go inside it, right click an empty space and hit "Git Bash Here". That should open up a command prompt.
3. Press the green "Code" button in the repository and copy the text under "HTTPS".
4. Type out ``git clone -b 1.0.6 <HTTPS code>`` in the command prompt and hit enter. This should clone the project files into the folder.
5. Open the solution (found under FrostyEditor) with Visual Studio 2019, and make sure the project is set to ``DeveloperDebug`` and ``x64``. Close out of retarget window if prompted.
6. Only build the projects themselves, never the solution.

## License
The Content, Name, Code, and all assets are licensed under a Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
