# donniebot
another discord bot written in c#

feel free to do pull requests or whatever idrc

how 2 build: 
1. `git clone --recurse-submodules https://github.com/jakedacatman/donniebot`
2. `cd donniebot/src`
3. `dotnet build`

if the packages do not install do `dotnet add package --prerelease <packagename>` in `donniebot/src`
  
you need libsodium, ffmpeg, and opus installed; in Arch you can run `sudo pacman -S libopus libsodium ffmpeg` to install them all
