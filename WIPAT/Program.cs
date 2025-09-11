using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WIPAT
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // EPPlus 7 style
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);


            // Show splash screen
            using (SplashScreen splash = new SplashScreen())
            {
                splash.Show();
                Application.DoEvents(); // force paint

                // Simulate some work (replace with real init)
                for (int i = 0; i < 50; i++)
                {
                    System.Threading.Thread.Sleep(50);
                    Application.DoEvents(); // keep splash responsive
                }

                // Start fade out
                splash.StartFadeOut();

                // Wait until it is hidden
                while (splash.Visible)
                    Application.DoEvents();
            }

            // Run main form
            //Application.Run(new MainForm());
            Application.Run(new LoginForm());
        }
    }
}
