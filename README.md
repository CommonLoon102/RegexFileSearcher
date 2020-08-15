# RegexFileSearcher
Cross-platform regex file searching tool in .NET Core
![](https://github.com/CommonLoon102/RegexFileSearcher/blob/master/image/screenshot.png?raw=true)
# Running
## Windows
 - Start the app with `RegexFileSearcher.Wpf.exe`
## Linux
 - Install the .NET Core Runtime
 - Start the app from the terminal with `dotnet RegexFileSearcher.Gtk.dll`
## Mac
 - Complete compiling steps below, which will create the file `RegexFileSearcher/RegexFileSearcher/RegexFileSearcher.Mac/bin/Debug/netcoreapp3.1/RegexFileSearcher.Mac.app`
 - Start the app from the terminal with `open RegexFileSearch.Mac.app`
 - Start the app from Finder by double-clicking `RegexFileSearch.Mac` (the .app file extension will be hidden by default)
# Compiling
## Windows
 - Download and open the repo in Visual Studio 2019
 - Publish `RegexFileSearcher.Wpf`
## Linux
 - Install the .NET Core SDK
 - You need to have GTK3 installed too
 - Git clone
 - Publish with `dotnet publish RegexFileSearcher/RegexFileSearcher.Gtk/RegexFileSearcher.Gtk.csproj --configuration Release --output publish --self-contained false --runtime linux-x64 --framework netcoreapp3.1`
## Mac
 - Install Visual Studio 2019 For Mac
 - Git clone
 - `cd RegexFileSearcher/RegexFileSearcher/RegexFileSearcher.Mac`
 - `dotnet build`
 
