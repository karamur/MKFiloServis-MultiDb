using KOAFiloServis.DataTransfer.UI;

namespace KOAFiloServis.DataTransfer;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
