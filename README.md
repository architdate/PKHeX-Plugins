# PKHeX-Plugins
Plugins for PKHeX
Uses the Plugin Interface in the base PKHeX repository.

## Building
This needs any IDE that supports compiling .NET based code (Ideally 4.6+). Recommended IDE is Visual Studio 2017

- First Clone this repository using: `$ git clone https://github.com/architdate/PKHeX-Plugins.git`
- Right click on the solution and click `Rebuild All`
- All the compiled DLL's will be present in the `*/bin` folder where `*` represents the mod name

## Usage
To use the plugins:
- Create a folder called `plugins` in the same directory as PKHeX
- Put the compiled plugins in the folder
- Start PKHeX and the plugins should be available for use in `Tools > Auto Legality Mod` menu

## Contributing
To contribute to the repository, you can submit a pull request to the repository. Try to follow a format similar to the plugins inside the repository already
All contributions are greatly appreciated!

## Support Server:
Come join the dedicated server for this mod! Ask questions, give suggestions, get help, or just hang out. Don't be shy, we don't bite:

[<img src="https://canary.discordapp.com/api/guilds/401014193211441153/widget.png?style=banner2">](https://discord.gg/9ptDkpV)

## Credits:
@kwsch (Kaphotics): for having the IPlugin interface which allows loading of Plugin DLL files into PKHeX. Also for the support provided in my discord server
