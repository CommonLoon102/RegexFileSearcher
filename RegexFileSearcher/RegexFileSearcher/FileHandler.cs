using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RegexFileSearcher
{
    internal static class FileHandler
    {
        private static readonly string _arguments = "";
        private static readonly ProcessStartInfo _defaultFileHandler;

        static FileHandler()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _defaultFileHandler = new() { FileName = "xdg-open" };
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _arguments = "/C start \"\" ";
                _defaultFileHandler = new()
                {
                    FileName = "cmd.exe",
                    Arguments = _arguments,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                };
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _defaultFileHandler = new() { FileName = "open" };
            }
        }

        public static void Open(string path)
        {
            if (_defaultFileHandler is null)
            {
                throw new FileHandlerException("No editor has been specified.");
            }

            _defaultFileHandler.Arguments += $"\"{path}\"";
            Process.Start(_defaultFileHandler);
            _defaultFileHandler.Arguments = _arguments;
        }
    }
}
