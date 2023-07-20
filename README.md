# MidiDump

A .NET program that dumps specific things from .mid RB/GH charts (currently, section names).

This program started out as a heavily modified version of GMS's [RipCheck](https://github.com/GenericMadScientist/RipCheck). Without it, I probably would never have done anything remotely like this.

## Usage

This program requires the .NET 7 runtime or higher to run. You should be prompted to install it upon running the program if it is not installed; if you are not prompted or have some other issue, then you may find the runtime [here](https://dotnet.microsoft.com/en-us/download/visual-st1udio-sdks). You do *not* need the SDK! Only the runtime is necessary.

Currently, the only mode of operation supported is dumping section names, but other modes could be added pretty easily in the future as needed.

### Section Names

To dump section names, you will need to supply it with a list of directories to search. You should also provide a list of known sections and a list of .dta files to source localization strings from. Although both of these are optional, they are highly recommended, as the other modes of operation are not particularly meant to be used.

#### Search Directories

There are two ways of setting search directories:

- Provide each folder to search via the command-line, one after the other. (Be sure to surround the paths in quotes!)
- Create a `paths.txt` file in the program's folder and specify each directory on a new line. No quotes are needed.

#### Known Sections

To provide a list of known sections, create a `known_sections.txt` file inside the program's folder and put each section in one the following formats:

- `[section name] - "Display Name"`
- `[prc_name] - "Display Name"`

The handling of this file is very lenient: as long as you have brackets around the event name and quotes around the display name, it wil parse correctly. Any lines not meeting these criteria are ignored.

For general purposes, use [the C3 charting docs' list of section names](http://docs.c3universe.com/rbndocs/index.php?title=All_Practice_Sections) for this file.

#### .dta Files

To supply .dta files, you have two options:

- Create a folder named `DTA` in the program's folder and place them there.
- Create a `dta.txt` file in the program's folder and specify each file path on a new line. No quotes are needed.

### Results

The program will output results to a `results.txt` file inside its folder.

## License

This program is licensed under the MIT license. See [LICENSE](LICENSE) for details.
