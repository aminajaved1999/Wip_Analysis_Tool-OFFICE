using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WIPAT
{
    using System;
    using System.Configuration;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Windows.Forms;

    public partial class SplashScreen : Form
    {
        Timer fadeInTimer = new Timer();
        Timer fadeOutTimer = new Timer();

        public SplashScreen()
        {
            InitializeComponent();
            LoadAppInfo();

            this.Opacity = 50; // start transparent


            // fade out
            fadeOutTimer.Interval = 50;
            fadeOutTimer.Tick += (s, e) =>
            {
                if (this.Opacity > 0)
                    this.Opacity -= 0.05;
                else
                {
                    fadeOutTimer.Stop();
                    this.Hide();
                }
            };
        }

        private void LoadAppInfo()
        {
            // Read from App.config
            titleLabel.Text = ConfigurationManager.AppSettings["AppName"];
            taglineLabel.Text = ConfigurationManager.AppSettings["AppTagline"];
            versionLabel.Text = "v" + ConfigurationManager.AppSettings["AppVersion"];
            footerLabel.Text = ConfigurationManager.AppSettings["AppDeveloper"];
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            fadeInTimer.Start(); // start fade-in when shown
        }

        public void StartFadeOut()
        {
            if (this.Visible)
                fadeOutTimer.Start();
        }

    }
}
