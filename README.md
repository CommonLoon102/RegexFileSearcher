RegexFileSearcher
=================
Cross-platform regex file searching tool in .NET Core
![](https://github.com/CommonLoon102/RegexFileSearcher/blob/master/image/screenshot.png?raw=true)
Running
-------
### Windows
 - Start the app with `RegexFileSearcher.Wpf.exe`
### Linux
 - Install the .NET Core Runtime
 - Start the app from the terminal with `dotnet RegexFileSearcher.Gtk.dll`
### Mac
 - Complete compiling steps below, which will create the file `RegexFileSearcher/RegexFileSearcher.Mac/bin/Release/netcoreapp3.1/osx-x64/osx-x64/RegexFileSearcher.Mac.app`
 - Start the app from the terminal with `open RegexFileSearcher/RegexFileSearcher.Mac/bin/Release/netcoreapp3.1/osx-x64/osx-x64/RegexFileSearcher.Mac.app`
 - Start the app from Finder by double-clicking `RegexFileSearch.Mac` (the .app file extension will be hidden by default)
Compiling
---------
### Windows
 - Download and open the repo in Visual Studio 2019
 - Publish `RegexFileSearcher.Wpf`
### Linux
 - Install the .NET Core SDK
 - You need to have GTK3 installed too
 - Git clone
 - Publish with `dotnet publish RegexFileSearcher/RegexFileSearcher.Gtk/RegexFileSearcher.Gtk.csproj --configuration Release --output publish --self-contained false --runtime linux-x64 --framework netcoreapp3.1`
### Mac
 - Install Visual Studio 2019 for Mac
 - Git clone
 - Publish with `dotnet publish RegexFileSearcher/RegexFileSearcher.Mac/RegexFileSearcher.Mac.csproj --configuration Release --output publish --self-contained false --runtime osx-x64 --framework netcoreapp3.1`
 Debugging
 ---------
### Windows
 - With `Visual Studio 2019 Community`
 - Or with `Visual Studio Code`
### Linux
 - With `Visual Studio Code`
### Mac
 - With `Visual Studio 2019 for Mac`, I guess
