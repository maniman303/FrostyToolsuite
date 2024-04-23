# FrostyToolsuite Linux version
The most advanced modding platform for games running on DICE's Frostbite game engine.

Uses [Koaloader](https://github.com/acidicoala/Koaloader) and [CryptHook](https://github.com/maniman303/CryptHook).

## Changes in Linux version

- Fixed mod deployment.
- Fixed game icons.
- Added `Install mods` button.
- Fixed `BCryptVerifySignature` patching with [Koaloader](https://github.com/acidicoala/Koaloader) and [CryptHook](https://github.com/maniman303/CryptHook).
- Disabled mods drag and drop (broken on Linux).
- Disabled auto update.

## Required setup on Linux
- Download latest [Koaloader](https://github.com/acidicoala/Koaloader/releases) release.
- Find `version.dll` under version-64, rename it to `koaloader.dll`, copy file to FrostyModManager/ThirdParty folder.

## Linux and Steam guide

- Install Bottles from Flatpak.
- Make sure Bottles have access to game directory (you can add it with Flatseal).
- Create a new application bottle, use latest soda or wine-ge runner (proton-ge is not suggested).
- If you are NOT using Proton make sure to install wine mono in bottle dependencies.
- Add FrostyModManager to shortcuts in the bottle, launch it.
- Add game exe manually. Scanning for games won't work unless you export registry entries from Proton prefix.
- Add mods with `Add mod` button, install mods with `Install mods` button.
- After mods installation take note of launch options provided by manager, add these options to the Steam game under game properties.

## Setup

1. Download Git https://git-scm.com/download/win.
2. Create an empty folder, go inside it, right click an empty space and hit "Git Bash Here". That should open up a command prompt.
3. Press the green "Code" button in the repository and copy the text under "HTTPS".
4. Type out ``git clone -b 1.0.6 <HTTPS code>`` in the command prompt and hit enter. This should clone the project files into the folder.
5. Open the solution (found under FrostyEditor) with Visual Studio 2019, and make sure the project is set to ``DeveloperDebug`` and ``x64``. Close out of retarget window if prompted.
6. Only build the projects themselves, never the solution.

## License
The Content, Name, Code, and all assets are licensed under a Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
