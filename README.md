# About
This project uses `PKHeX.Core` and PKHeX's `IPlugin` interface to provide PKHeX program enhancements, namely **Auto**mated **Mod**ifications to create Legal Pokémon. Please refer to the [Wiki](https://github.com/architdate/PKHeX-Plugins/wiki) for more information regarding the functionalities provided by this project.

This project is owned by [@architdate](https://github.com/architdate) (Discord: thecommondude#8240) and [@kwsch](https://github.com/kwsch) (Discord: Kurt#6024)

## Building
This project requires an IDE that supports compiling .NET based code (Ideally .NET 4.6+). Recommended IDE is Visual Studio 2019 

- First Clone this repository using: `$ git clone https://github.com/architdate/PKHeX-Plugins.git`
- Right click on the solution and click `Rebuild All`
- All the compiled DLL's will be present in the `*/bin` folder where `*` represents the mod name

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
[@kwsch](https://github.com/kwsch): for providing the IPlugin interface in PKHeX, which allows loading of this project's Plugin DLL files. Also for the support provided in the support server.
