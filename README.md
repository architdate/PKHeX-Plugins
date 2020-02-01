# About
This project uses `PKHeX.Core` and PKHeX's `IPlugin` interface to provide PKHeX program enhancements, namely **Auto**mated **Mod**ifications to create Legal Pokémon. Please refer to the [Wiki](https://github.com/architdate/PKHeX-Plugins/wiki) for more information regarding the functionalities provided by this project.

This project is owned by [@architdate](https://github.com/architdate) (Discord: thecommondude#8240) and [@kwsch](https://github.com/kwsch) (Discord: Kurt#6024)

[Feature Demonstration Video](https://www.youtube.com/watch?v=pKuElH0hWWA) by AAron#2420

## Building
This project requires an IDE that supports compiling .NET based code (Ideally .NET 4.6+). Recommended IDE is Visual Studio 2019 

**Building Regular Builds**
Regular builds will usually work unless there are changes that have been commited to the mod that do not work with the nuget [PKHeX.Core](https://www.nuget.org/packages/PKHeX.Core) package dependancy specified in the `.csproj` files of the projects. If building fails, use the bleeding edge build method

- First Clone this repository using: `$ git clone https://github.com/architdate/PKHeX-Plugins.git`
- Right click on the solution and click `Rebuild All`
- The compiled DLL's will be present in the `AutoLegalityMod/bin` folder. You will need to have `AutoModPlugins.dll` and `PKHeX.Core.AutoMod.dll` files in your plugins folder. `BouncyCastle.CryptoExt.dll` should be in the same directory as `PKHeX.exe`. You may also combine all three of these dll files using ILMerge

**Building Bleeding Edge Builds**
Use this build method only if the regular builds fail. The AppVeyor CI will always use the bleeding edge build method. More details regarding this can be seen in the [appveyor.yml](https://github.com/architdate/PKHeX-Plugins/blob/master/appveyor.yml) file.

- First Clone the PKHeX repository using: `$ git clone https://github.com/kwsch/PKHeX.git`
- Clone this repo using: `$ git clone https://github.com/architdate/PKHeX-Plugins.git`
- Open the PKHeX solution and right click on the `PKHeX.Core` project and click `Rebuild` to build the project with `Release` as the environment.
- Open the PKHeX-Plugins solution and do `nuget restore`.
- Copy the `PKHeX.Core.dll` file located in `PKHeX.Core/bin/Release/net46` folder and copy it to the following folders: 
    * `PKHeX-Plugins/packages/PKHeX.Core.YY.MM.DD/lib/net46`
    * `C:/Users/%USERNAME%/.nuget/packages/pkhex.core/YY.MM.DD/lib/net46`
- Copy the `PKHeX.Core.dll` file located in `PKHeX.Core/bin/Release/netstandard2.0` folder and copy it to the following folders: 
    * `PKHeX-Plugins/packages/PKHeX.Core.YY.MM.DD/lib/netstandard2.0`
    * `C:/Users/%USERNAME%/.nuget/packages/pkhex.core/YY.MM.DD/lib/netstandard2.0`
- Right click the PKHeX-Plugins solution and choose `Rebuild All`. This should build the mod with the latest `PKHeX.Core` version so that it can be used with the latest commit of PKHeX

## Usage
To use the plugins:
- Create a folder called `plugins` in the same directory as PKHeX.exe
- Put the compiled plugins from this project in the `plugins` folder
- Start PKHeX.
- The plugins should be available for use in `Tools > Auto Legality Mod` drop-down menu

## Support Server:
Come join the dedicated Discord server for this mod! Ask questions, give suggestions, get help, or just hang out. Don't be shy, we don't bite:

[<img src="https://canary.discordapp.com/api/guilds/401014193211441153/widget.png?style=banner2">](https://discord.gg/tDMvSRv)

## Contributing
To contribute to the repository, you can submit a pull request to the repository. Try to follow a format similar to the current codebase. All contributions are greatly appreciated! If you would like to discuss possible contributions without using GitHub, please contact us using the Support Server above.

## Credits:
- [@kwsch](https://github.com/kwsch): for providing the IPlugin interface in PKHeX, which allows loading of this project's Plugin DLL files. Also for the support provided in the support server.
- [@olliz0r](https://github.com/olliz0r): For developing and maintaining `sys-botbase` which is necessary for LiveHeX to work.
- [@Rino6357](https://github.com/Rino6357) and [@crzyc](https://github.com/crzyc) for their help with the GitHub Wiki associated with this project!
- [Lusamine](https://github.com/Lusamine) for all their help with stress testing the code with wacky sets!
- [FlatIcon](https://www.flaticon.com/): for their icons. Author credits (Those Icons, Pixel perfect)
