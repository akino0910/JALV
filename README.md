# JALV! - Json Appender Log4Net Viewer

## This is a proof of concept branch for .net core 3

Because .Net Core 3 is not out yet, consider this branch as a proof of concept.

Why:

* .Net Core is a future of .Net and lately all the development I do is in .Net Core. After microsoft announced WPF support with .Net Core 3, I wanted to check if I am able to migrate this project.

What was done:

* Changed .csproj files to new format
* simplified Resource imports and removed obsolete files (AssemblyInfo.cs)
* simplified nuget package references

What works:

* almost everything :-)

What doesn't work:

* "clear filter" button - didn't have time to check why yet

### How to build this version

Because I don't ship binaries with this version, you have to build it by yourself:

* First clone the project

```ps
git clone -b netcore --single-branch https://github.com/stefanjarina/JALV.git
```

At the moment there are basically 2 options.

* Use the new Visual Studio 2019 Preview 4 +
  * In this case simply open the project `src\JALV.sln` in VS 2019, press `Build Solution` and you're good to go

* Use `dotnet` command line tool

```ps
cd JALV\src    # Switch to cloned project to where JALV.sln is locted
dotnet restore # Restore all the nuget packages
cd JALV        # Switch to WPF project
dotnet build   # Build the project to check for any errors
dotnet run     # Run the application
```

* This is basically original WPF app switched to target `netcoreapp3.0` for **JALV** and `netstandard2.0` for **Jalv.Core**
* Because of this rather dirty switch some additional cleaning might occure

## What is JALV

JALV! is a log file viewer for Log4Net with handy features like log merging, filtering, open most recently used files, items sorting and so on. It is easy to use, it requires no configuration, it has intuitive and user-friendly interface and available in several languages. It is a WPF Application based on .NET Framework 4.0 and written in C# language.

![Screenshot](/doc/images/JALV-Win.png?raw=true "JALV Main Window")

## Main features

* Log files merging into one list
* Dynamic log events filtering
* Dynamic show/hide log events by log level
* Favorites log folders list
* Open most recently used files
* Sort and reorder columns
* Copy log event data to clipboard
* Open files by dragging them to the main window

## Supported formats

* Json Appender [log4net.Ext.Json](https://www.nuget.org/packages/log4net.Ext.Json/)
* Xml Appender

## Localizations

* English
* French
* German
* Italian
* Russian
* Japanese
* Chinese
* Greek

## Configuration

JALV itself does not require any setup, but log4net must be setup in your application to write XML content in XmlLayoutSchemaLog4j layout to log files. [Read more...](https://github.com/stefanjarina/JALV/wiki)

## Usage

Download latest binaries, unzip and launch JALV.exe. That's all!
JALV GUI language follows your Windows culture automatically, but you can override this behavior.

## Disclaimer

JALV is a successor of [YALV by Luca Petrini](https://github.com/LukePet/YALV) which seems to be abandoned since 2016.

It was forked from version 1.4.0.0, renamed and released as a version 1.5.0.0 with JSON support.
