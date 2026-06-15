using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WIPAT.BLL.Interfaces;
using WIPAT.BLL.Manager;
using WIPAT.BLL.Managers;
using WIPAT.BLL.Services;
using WIPAT.DAL;
using WIPAT.Entities;

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
            #region 1. Application Configuration

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            #endregion

            #region 2. Infrastructure & Services Initialization

            WipSession session = new WipSession();
            WIPATContext dbContext = new WIPATContext();

            // Initialize Repositories (Interact directly with DB)
            var userRepo = new UserRepository(dbContext);
            var itemsRepo = new ItemsRepository(dbContext);
            var wipRepo = new WipRepository(dbContext);
            var stockRepo = new StockRepository(dbContext, session);
            var orderRepo = new OrderRepository(dbContext, session);
            var forecastRepo = new ForecastRepository(dbContext);
            var miscRepo = new MiscellaneousRepository(dbContext);

            // Initialize Business Logic (Managers/Services - Dependency Injection)
            var excelService = new ExcelService(session, itemsRepo);
            var forecastManager = new ForecastManager(forecastRepo, itemsRepo, excelService);
            var stockManager = new StockManager(stockRepo, itemsRepo, excelService);
            var orderManager = new OrderManager(orderRepo, itemsRepo, excelService);
            var wipManager = new WipManager(wipRepo, forecastRepo, stockRepo, session);
            var NewWipManager = new NewWorkingWipManager(wipRepo, forecastRepo, stockRepo, session);

            #endregion

            #region 3. Splash Screen & Background DB Warmup

            using (SplashScreen splash = new SplashScreen())
            {
                splash.Show();
                Application.DoEvents(); // Force UI paint

                // --- THE FIX: WAKE UP ENTITY FRAMEWORK IN THE BACKGROUND ---
                // This prevents the massive lag spike when opening the first form!
                Task.Run(() =>
                {
                    try { dbContext.Database.Initialize(false); } catch { }
                });

                // Simulate initialization work or loading assets
                for (int i = 0; i < 40; i++)
                {
                    System.Threading.Thread.Sleep(40);
                    Application.DoEvents(); // Keep splash responsive
                }

                // Start fade out animation
                splash.StartFadeOut();

                // Wait until it is fully hidden/closed
                while (splash.Visible)
                {
                    Application.DoEvents();
                }
            }

            #endregion

            #region 4. Login Flow (Dialog First)

            User loggedInUser = null;

            using (var loginForm = new LoginForm(userRepo))
            {
                var result = loginForm.ShowDialog();

                if (result == DialogResult.OK)
                {
                    loggedInUser = loginForm.AuthenticatedUser;
                }
                else
                {
                    return;
                }
            }

            #endregion

            #region 5. Main Application Start

            // We only reach here if loggedInUser is NOT null
            if (loggedInUser != null)
            {
                // Inject the authenticated user into the session
                session.LoggedInUser = loggedInUser;

                // Create the MainForm with all dependencies injected
                var mainForm = new MainForm(
                    loggedInUser,
                    forecastManager,
                    stockManager,
                    orderManager,
                    excelService,
                    wipManager,
                    itemsRepo,
                    stockRepo,
                    wipRepo,
                    NewWipManager
                );

                // Start the message loop with the Main Form
                Application.Run(mainForm);
            }

            #endregion
        }
    }
}