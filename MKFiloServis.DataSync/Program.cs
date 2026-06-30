using MKFiloServis.DataSync.UI;
using System;
using System.Windows.Forms;

namespace MKFiloServis.DataSync;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // CLI modu: argüman varsa konsol modu
        if (args.Length > 0)
        {
            return CliRunner.RunAsync(args).GetAwaiter().GetResult();
        }

        // UI modu
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        return 0;
    }
}


