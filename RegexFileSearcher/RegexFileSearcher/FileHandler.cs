using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RegexFileSearcher
{
    internal static class FileHandler
    {
        public static void Open(string path)
        {
            ProcessStartInfo processStartInfo = null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                processStartInfo = new()
                {
                    FileName = "cmd.exe",
                    Arguments = "/C start \"\" ",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                };
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                processStartInfo = new()
                {
                    FileName = "xdg-open"
                };
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                processStartInfo = new()
                {
                    FileName = "open"
                };
            }

            if (processStartInfo is null)
            {
                throw new FileHandlerException("No editor has been specified.");
            }

            processStartInfo.Arguments += $"\"{path}\"";
            Process.Start(processStartInfo);
        }
    }
}
