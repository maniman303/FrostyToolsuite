# FrostyToolSuite Linux version
The most advanced modding platform for games running on DICE's Frostbite game engine.

Uses [WinmmProxy](https://github.com/maniman303/winmm-proxy), [CryptHook](https://github.com/maniman303/CryptHook) and [Wine-symlink-helper](https://github.com/maniman303/wine-symlink-helper).

## Changes in this fork

- Fixed symbolic linking of files in mod deployment.
- Added hard linking option for mod deployment (on by default).
- Reworked actions tab and fixed it's performance.
- Fixed drag and drop on Linux.
- Fixed game icons on Linux.
- Added `Install mods` button.
- Fixed `BCryptVerifySignature` patching with [WinmmProxy](https://github.com/maniman303/winmm-proxy) and [CryptHook](https://github.com/maniman303/CryptHook).
- Fixed multi threading issues in the program.
- Fixed minimize, maximize, close buttons on Linux.
- Disabled auto update.
- Updated project to .NET framework 4.8.1.
- Added exceptions logging for mod installation.
- Added basic Wine installation validation.

## Linux and Steam guide

- Install Bottles from Flatpak.
- Make sure Bottles have access to ALL USER FILES (you can add it with Flatseal, without it Frosty or even wine explorer can crash).
- Create a new application bottle, use latest soda or wine kron4ek (not tkg) runner (proton-ge is not suggested).
- If you are NOT using Proton make sure to install wine mono in bottle dependencies (usually auto installed).
- Add Frosty Mod Manager directory as a new drive in bottle settings.
- Add game directory as another new drive in bottle settings.
- Run Explorer from Legacy Wine tools from your bottle to verify that drives are accessible.
- Add FrostyModManager to shortcuts in the bottle, launch it.
- Add game exe manually. Scanning for games won't work unless you export registry entries from Proton prefix.
- Add mods with Add mods  button, install mods with Install mods button.
- After mods installation take note of launch options provided by manager, add these options to the Steam game under game properties.

## Differences between hard and soft links

### Hard links
Pros:
- Fast
- Safe
- Reliable

Cons:
- Game directory with hard links reports ~twice the size it actually takes

### Symbolic links
Pros:
- No issues with reported game directory size
- Clear indication in file explorer if file is modded or not

Cons:
- Slower
- Unsafe if not approached carefully

## Symbolic links on Linux aka Mission Impossible

Even if Linux supports symbolic links just fine, Wine does not implement their support at all. But fortunately there is a loop hole, that allowed me to *reimplement* symbolic links under Wine.

I've implemented simple [Wine-symlink-helper](https://github.com/maniman303/wine-symlink-helper) program, that allows me to perform basic symbolic link operations under Frosty. For whatever reason output redirection doesn't work, so I had to substitute proper communication with exit code reading. Still, it does its job.

To translate Windows paths to Linux I'm using `winepath.exe` from Wine.

Unfortunately, there are some issues with this hack of a proper solution.
- Performance. Running a whole new process to create/remove/check file increases time spent on the operation tens or hundreds times. Frosty has a case where it has to scan provided directory to check if any symbolic link exists there, including sub directories. Even when using BFS search with running checks in parallel multitasking, scanning hard linked Dragon Age Inquisition mod directory (without a single symbolic link) takes on my Steam Deck 3 minutes. On Windows the same algorithm took a mere second. Because of this I had to reimplement algorithm with C++ in [Wine-symlink-helper](https://github.com/maniman303/wine-symlink-helper), which does the job well, but long term I don't see it viable to every single every single use case.
- Compatibility is also an issue here, because we can't assume that the binary will always work. Because of this, in code, I try to test support for symbolic and hard links, but it's not a guarantee.

## Setup

1. Download Git https://git-scm.com/download/win.
2. Create an empty folder, go inside it, right click an empty space and hit "Git Bash Here". That should open up a command prompt.
3. Press the green "Code" button in the repository and copy the text under "HTTPS".
4. Type out ``git clone <HTTPS code>`` in the command prompt and hit enter. This should clone the project files into the folder.
5. Open the solution (found under FrostyEditor) with Visual Studio 2019, and make sure the project is set to ``DeveloperDebug`` and ``x64``. Close out of retarget window if prompted.
6. Only build the projects themselves, never the solution.

## License
The Content, Name, Code, and all assets are licensed under a Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
