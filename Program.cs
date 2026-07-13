//Program.cs
using System;
using System.IO;
using System.Windows.Forms;

namespace winProyComunicacion
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            string rutaConfig = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TatoText",
                "config.json");
            string nombreInicial = "";
            try
            {
                if (File.Exists(rutaConfig))
                {
                    string json = File.ReadAllText(rutaConfig);
                    using (var doc = System.Text.Json.JsonDocument.Parse(json))
                    {
                        if (doc.RootElement.TryGetProperty("NombreUsuario", out var prop))
                        {
                            nombreInicial = prop.GetString() ?? "";
                        }
                    }
                }
            }
            catch { }

            Application.Run(new Main());
        }
    }
}