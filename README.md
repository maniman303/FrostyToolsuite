# FrostyToolsuite Linux version
The most advanced modding platform for games running on DICE's Frostbite game engine.

Uses [VersionProxy](https://github.com/maniman303/frosty-version-proxy) and [CryptHook](https://github.com/maniman303/CryptHook).

## Changes in this fork

- Fixed symbolic linking of files in mod deployment.
- Added hard linking option for mod deployment (on by default).
- Fixed game icons on Linux.
- Added `Install mods` button.
- Fixed `BCryptVerifySignature` patching with [VersionProxy](https://github.com/maniman303/frosty-version-proxy) and [CryptHook](https://github.com/maniman303/CryptHook).
- Fixed multi threading issues in the program.
- Disabled auto update.
- Updated project to .NET framework 4.8.1.

## Linux and Steam guide

- Install Bottles from Flatpak.
- Make sure Bottles have access to game directory (you can add it with Flatseal).
- Create a new application bottle, use latest soda or wine-ge runner (proton-ge is not suggested).
- If you are NOT using Proton make sure to install wine mono in bottle dependencies.
- Add FrostyModManager to shortcuts in the bottle, launch it.
- Add game exe manually. Scanning for games won't work unless you export registry entries from Proton prefix.
- Add mods with `Add mod` button, install mods with `Install mods` button.
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

According to [WineFAQ](https://wiki.winehq.org/FAQ) you can run native Linux binaries from *Windows* program (with some caveats, but more on this later), but do so you need to update one key in registry. And yet, this is only half way true, as actually this registry key is passed down as an environmental variable to all processes. And in both Windows and Linux code, we can adjust environmental variables of the new process we are about to run. In my implementation - just to be safe - I edit registry and update variables.

With this knowledge I'm using following Linux binaries to achieve sym links functionality:
- `/bin/ls` to test if file or directory is a symbolic link
- `/bin/ln` to create symbolic links
- `/bin/rm` to safely remove symbolic link
- `/bin/find` to test if provided directory contains symlinks

To translate Windows paths to Linux I'm using `winepath.exe` from Wine.

Unfortunately, there are some issues with this hack of a proper solution.
- We cannot redirect output of Linux native binary into out Windows .NET program. Idk why, but because of this, while using `/bin/ls` or `/bin/find`, I had to run these binaries in `cmd` and redirect the output to a real file, so later I could read the result from this file. I believe I don't have to mention it's a multi threading safety nightmare.
- Performance. Running a whole new process (or sometimes even two) to create/remove/check file increases time spent on the operation tens or hundreds times. Frosty has a case where it has to scan provided directory to check if any symbolic link exists there, including sub directories. Even when using BFS search with running checks in parallel multitasking, scanning hard linked Dragon Age Inquisition mod directory (without a single symbolic link) takes on my Steam Deck 3 minutes. On Windows the same algorithm took a mere second. Because of this I had to be creative and use `/bin/find`, which does the job well, but long term I don't see it viable to look for different binaries and commands for every single use case.
- Compatibility is also an issue here, because I cannot guarantee that every distro will have binaries, mentioned earlier, existing or in right place. Because of this, in code, I try to test support for symbolic and hard links, but it's not a guarantee.

But there is another approach I would like to investigate in the future: [WineLib](https://wiki.winehq.org/Winelib).

## Setup

1. Download Git https://git-scm.com/download/win.
2. Create an empty folder, go inside it, right click an empty space and hit "Git Bash Here". That should open up a command prompt.
3. Press the green "Code" button in the repository and copy the text under "HTTPS".
4. Type out ``git clone <HTTPS code>`` in the command prompt and hit enter. This should clone the project files into the folder.
5. Open the solution (found under FrostyEditor) with Visual Studio 2019, and make sure the project is set to ``DeveloperDebug`` and ``x64``. Close out of retarget window if prompted.
6. Only build the projects themselves, never the solution.

## License
The Content, Name, Code, and all assets are licensed under a Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
