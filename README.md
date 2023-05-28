# FrostyToolsuite
The most advanced modding platform for games running on DICE's Frostbite game engine.

## maniman303-Fork
- Symlink fixed on Linux. When program is launched through Wine instead of using softlinks it will now use hardlinks. Behaviour on Windows is unchanged.
- Disabled drag and drop mod installation when mod manager is run through Wine, as it had 75% chance of freezing the program. On Windows behaviour is unchanged.
- Only tested mod manager, editor might still have issues.
- Fixed building plugins when path to project contains spaces - xcopy scripts were pissing proper formatting.
- Added new launch parameter to create direct-to-game shortcuts. Use it like this `FrostyModManager.exe -game MassEffectAndromeda -launch Default`. First option is game id and second is the profile to be used.

## How to find game id
1. Launch Frosty Mod Manager at least once and add your game to it.
2. Go to `%LocalAppData%\Frosty` and open `manager_config.json`.
3. Under `"Games":` you will have your game id.

## Setup

1. Download Git https://git-scm.com/download/win.
2. Create an empty folder, go inside it, right click an empty space and hit "Git Bash Here". That should open up a command prompt.
3. Press the green "Code" button in the repository and copy the text under "HTTPS".
4. Type out ``git clone -b 1.0.6 <HTTPS code>`` in the command prompt and hit enter. This should clone the project files into the folder.
5. Open the solution (found under FrostyEditor) with Visual Studio 2019, and make sure the project is set to ``DeveloperDebug`` and ``x64``. Close out of retarget window if prompted.
6. Only build the projects themselves, never the solution.

## License
The Content, Name, Code, and all assets are licensed under a Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
