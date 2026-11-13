using System.ComponentModel;

namespace ClickerApp;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        try
        {
            Application.Run(new ClickerForm());
        }
        catch (Win32Exception exception)
        {
            MessageBox.Show(
                $"Windows işlemi başlatılamadı: {exception.Message}",
                "Sabri Abi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }    
}