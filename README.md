# donniebot
another discord bot written in c#

feel free to do pull requests or whatever idrc

how 2 build: 
1. `git clone --recurse-submodules https://github.com/jakedacatman/donniebot`
2. `cd donniebot/src`
3. `dotnet build`

if the packages do not install do `dotnet add package --prerelease <packagename>` in `donniebot/src`
  
you need libsodium, ffmpeg, and opus installed; in Arch you can run `sudo pacman -S libopus libsodium ffmpeg` to install them all

also needs the Impact, [Twemoji Mozilla](https://github.com/mozilla/twemoji-colr/releases), and [HanaMinA](https://osdn.net/projects/hanazono-font/downloads/64385/hanazono-20160201.zip) fonts installed; you can take Impact from a Windows installation and install HanaMinA from the Arch package `ttf-hanazono`
