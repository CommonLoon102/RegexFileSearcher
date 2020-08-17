using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace RegexFileSearcher
{
    class FileHandler
    {
        private readonly string _arguments = "";
        private readonly ProcessStartInfo _defaultFileHandler;

        public FileHandler()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _defaultFileHandler = new ProcessStartInfo { FileName = "xdg-open" };
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _arguments = "/C start \"\" ";
                _defaultFileHandler = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = _arguments,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                };
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _defaultFileHandler = new ProcessStartInfo { FileName = "open" };
            }
        }

        public void Open(string path)
        {
            if (_defaultFileHandler == null)
            {
                throw new FileHandlerException("No editor has been specified.");
            }

            _defaultFileHandler.Arguments += $"\"{path}\"";
            Process.Start(_defaultFileHandler);
            _defaultFileHandler.Arguments = _arguments;
        }
    }
}
