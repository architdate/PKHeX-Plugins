# About  
This project uses `PKHeX.Core` and PKHeX's `IPlugin` interface to add enhancements to the PKHeX program, namely **Auto**mated **Mod**ifications to simplify creation of legal Pokémon. Please refer to the [Wiki](https://github.com/architdate/PKHeX-Plugins/wiki) for more information regarding the functionalities provided by this project.

This project is owned by [@architdate](https://github.com/architdate) (Discord: thecommondude#8240) and [@kwsch](https://github.com/kwsch) (Discord: Kurt#6024).

[Feature Demonstration Video](https://www.youtube.com/watch?v=pKuElH0hWWA) by AAron#2420.

## Building  
This project requires an IDE that supports compiling .NET based code (Ideally .NET 4.6+). Recommended IDE is Visual Studio 2019.

**Regular Builds**  
Regular builds will usually succeed unless there are changes that are incompatible with the NuGet [PKHeX.Core](https://www.nuget.org/packages/PKHeX.Core) package dependency specified in the `.csproj` files of the projects. If building fails, use the bleeding edge method instead.

- Clone the PKHeX-Plugins repository using: `$ git clone https://github.com/architdate/PKHeX-Plugins.git`.
- Right-click on the solution and click `Rebuild All`.
- These DLLs should be placed into a `plugins` directory where the PKHeX executable is. You may also combine these DLL files using ILMerge.
   - The compiled DLLs for AutoLegality will be in the `AutoLegalityMod/bin/Release/net46` directory:
     * AutoModPlugins.dll
     * LibUsbDotNet.LibUsbDotNet.dll
     * NtrSharp.dll
     * PKHeX.Core.AutoMod.dll
     * PKHeX.Core.Enhancements.dll
     * PKHeX.Core.Injection.dll
   - If you want to use QRPlugins, you will need additional DLLs from `QRPlugins/bin/Release/net46`:
     * BouncyCastle.CryptoExt.dll
     * QRCoder.dll
     * QRPlugins.dll
     * zxing.dll
     * zxing.presentation.dll

**Bleeding Edge Builds**  
Use this build method only if the regular builds fail. The AppVeyor CI will always use the bleeding edge build method. More details regarding this can be seen in the [appveyor.yml](https://github.com/architdate/PKHeX-Plugins/blob/master/appveyor.yml) file.

- Clone the PKHeX repository using: `$ git clone https://github.com/kwsch/PKHeX.git`.
- Clone the PKHeX-Plugins repository using: `$ git clone https://github.com/architdate/PKHeX-Plugins.git`.
- Open the PKHeX solution, change your environment to `Release`, right-click on the `PKHeX.Core` project, and click `Rebuild` to build the project.
- Open the PKHeX-Plugins solution and right-click to `Restore NuGet Packages`.
- Next, replace the most recent NuGet packages with the newly-built `PKHeX.Core.dll` files.
   - Copy the `PKHeX.Core.dll` file located in `PKHeX.Core/bin/Release/net46` the following folders:
       * `C:/Users/%USERNAME%/.nuget/packages/pkhex.core/YY.MM.DD/lib/net46`
   - Copy the `PKHeX.Core.dll` file located in `PKHeX.Core/bin/Release/netstandard2.0` to the following folders: 
       * `C:/Users/%USERNAME%/.nuget/packages/pkhex.core/YY.MM.DD/lib/netstandard2.0`
- Right click the PKHeX-Plugins solution and `Rebuild All`. This should build the mod with the latest `PKHeX.Core` version so that it can be used with the latest commit of PKHeX.
- The compiled DLLs will be in the same location as with the regular builds. 

## Usage  
To use the plugins:
- Create a folder named `plugins` in the same directory as PKHeX.exe.
- Put the compiled plugins from this project in the `plugins` folder. 
- Start PKHeX.exe.
- The plugins should be available for use in `Tools > Auto Legality Mod` drop-down menu.

## Support Server
Come join the dedicated Discord server for this mod! Ask questions, give suggestions, get help, or just hang out. Don't be shy, we don't bite:

[<img src="https://canary.discordapp.com/api/guilds/401014193211441153/widget.png?style=banner2">](https://discord.gg/tDMvSRv)

## Contributing
To contribute to the repository, you can submit a pull request to the repository. Try to follow a format similar to the current codebase. All contributions are greatly appreciated! If you would like to discuss possible contributions without using GitHub, please contact us on the support server above.

## Credits
**Repository Owners**
- [architdate (thecommondude)](https://github.com/architdate)
- [kwsch (Kaphotics)](https://github.com/kwsch)

**Credit must be given where due...**
This project would not be as amazing without the help of the following people who have helped me since the original [Auto-Legality-Mod](https://github.com/architdate/PKHeX-Auto-Legality-Mod).

- [@kwsch](https://github.com/kwsch) for providing the IPlugin interface in PKHeX, which allows loading of this project's Plugin DLL files. Also for the support provided in the support server.
- [@berichan](https://github.com/berichan) for adding USB-Botbase support to LiveHeX.
- [@soopercool101](https://github.com/soopercool101) for many improvements to Smogon StrategyDex imports and various other fixes.
- [@Lusamine](https://github.com/Lusamine) for all the help with stress testing the code with wacky sets!
- [@ReignOfComputer](https://github.com/ReignOfComputer) for the sets found in [RoCs-PC](https://github.com/ReignOfComputer/RoCs-PC) which are used for unit testing.
- TORNADO for help with test cases.
- [@Rino6357](https://github.com/Rino6357) and [@crzyc](https://github.com/crzyc) for initial help with the Wiki.
- [@hp3721](https://github.com/hp3721) for help with adding localization based on PKHeX's implementation.
- [@Bappsack](https://github.com/Bappsack) for his help on Discord in voice chats!
- [@chenzw95](https://github.com/chenzw95) for help with integration.
- [@BernardoGiordano](https://github.com/BernardoGiordano) for many ideas on improving speed.
- [@olliz0r](https://github.com/olliz0r) for developing and maintaining `sys-botbase` as well which is necessary for LiveHeX to work and for [LedyLib](https://github.com/olliz0r/Ledybot/tree/master/LedyLib) from which a lot of the NTR processing code is liberally referenced.
- [@fishguy6564](https://github.com/fishguy6564) for creating `USB-Botbase` (by extending sys-botbase).
- [FlatIcon](https://www.flaticon.com/) for their icons. Author credits (Those Icons, Pixel perfect).
- [Project Pokémon](https://github.com/projectpokemon/) for their Mystery Gift Event Gallery.
- And all the countless users who have helped improve this project with ideas and suggestions!
