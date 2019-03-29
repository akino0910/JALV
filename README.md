# JALV! - Json Appender Log4Net Viewer

**MASTER:** [![Build status](https://ci.appveyor.com/api/projects/status/pv3y4thnj0qau96b/branch/master?svg=true)](https://ci.appveyor.com/project/stefanjarina/jalv/branch/master)
 **DEVELOP:** [![Build status](https://ci.appveyor.com/api/projects/status/pv3y4thnj0qau96b/branch/develop?svg=true)](https://ci.appveyor.com/project/stefanjarina/jalv/branch/develop)

JALV! is a log file viewer for Log4Net with handy features like log merging, filtering, open most recently used files, items sorting and so on. It is easy to use, it requires no configuration, it has intuitive and user-friendly interface and available in several languages. It is a WPF Application based on .NET Framework 4.0 and written in C# language.

![Screenshot](/doc/images/JALV-Win.png?raw=true "JALV Main Window")
[More Screenshots](https://github.com/stefanjarina/JALV/wiki/Screenshots)

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
