using System;
using Eto.Forms;

namespace RegexFileSearcher.Gtk
{
    class MainClass
    {
        [STAThread]
        public static void Main()
        {
            new Application(Eto.Platforms.Gtk).Run(new MainForm());
        }
    }
}
