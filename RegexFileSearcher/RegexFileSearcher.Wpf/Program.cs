using System;
using Eto.Forms;

namespace RegexFileSearcher.Wpf
{
    class MainClass
    {
        [STAThread]
        public static void Main()
        {
            new Application(Eto.Platforms.Wpf).Run(new MainForm());
        }
    }
}
