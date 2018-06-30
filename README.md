# PKHeX-Plugins
Plugins for PKHeX
Uses the Plugin Interface in the base PKHeX repository.

## Building
This needs any IDE that supports compiling .NET based code (Ideally 4.6+). Recommended IDE is Visual Studio 2017

First Clone this repository using: `$ git clone https://github.com/architdate/PKHeX-Plugins.git`

**To compile Auto Legality Mod Plugin**
- Clone the base PKHeX repository and open in Visual Studio 2017
- Right click on the solution and add an existing project
- Add the `AutoLegalityMod` folder as the project
- Right click the `References` in `AutoLegalityMod` project and add the `PKHeX.Core` project as a dependancy
- Right click the solution and click `Rebuild All`
- The build should be available in the `AutoLegalityMod/bin/` folder

**To compile other plugins**
- Other plugins are independent solutions on their own, however they can be added on to the PKHeX repository for building with the latest `PKHeX.Core.dll` file.
To compile: 
- Just open the plugin solution file in Visual Studio 2017
- Add any dependency DLL's by right clicking `References` and adding the reference to the DLL files
- Build the solution by right clicking the solution and clicking `Rebuild All`
- The build will be available in the `/bin/` folder within the plugin solution

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
