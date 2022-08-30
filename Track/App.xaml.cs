using System;
using System.Windows;
using System.Drawing;
using Forms = System.Windows.Forms;
using System.Diagnostics;

namespace Track
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly Forms.NotifyIcon notifyIcon;
        public static App i;
        private Icon passive, active;
        public App() {
            notifyIcon = new Forms.NotifyIcon();
            i = this;
            passive = new Icon("favicon.ico");
            active = new Icon("favicon2.ico");
        }
        protected override void OnStartup(StartupEventArgs e) {
            notifyIcon.DoubleClick += new EventHandler(goToMainWindow);

            //notifyIcon.DoubleClick += new EventHandler(notifyIcon_DoubleClick);
            notifyIcon.Icon = passive;
            notifyIcon.Visible = true;
            notifyIcon.Text = "Ginalne Track";
            notifyIcon.ContextMenuStrip = new Forms.ContextMenuStrip();
            notifyIcon.ContextMenuStrip.Items.Add("Open", null, goToMainWindow);
            notifyIcon.ContextMenuStrip.Items.Add("Go To Proman...");
            notifyIcon.ContextMenuStrip.Items.Add(new Forms.ToolStripDropDownButton("Logged : @harbimartin", null,
                new Forms.ToolStripButton("Profile"),
                new Forms.ToolStripButton("Sign-Out"),
                new Forms.ToolStripButton("Change Account...")));
            notifyIcon.ContextMenuStrip.Items.Add("Exit", null, onExit);
            base.OnStartup(e);
        }
        public void setTrackState(bool onTracking) {
            notifyIcon.Icon = onTracking ? active : passive;
        }
        void notifyIcon_DoubleClick(object sender, EventArgs e) {
            Trace.WriteLine("notifyIcon_DoubleClick");
        }
        void goToMainWindow(object sender, EventArgs e) {
            if (ActWindow.singleton.IsVisible) {
                MainWindow.WindowState = WindowState.Normal;
                MainWindow.Activate();
                Trace.WriteLine("Window is null");
            } else {
                ActWindow.singleton.Show();
            }
        }

        void onExit(object sender, EventArgs e) {
            notifyIcon.Visible = false;
            Current.Shutdown();
        }

        protected override void OnExit(ExitEventArgs e) {
            notifyIcon.Dispose();
            notifyIcon.Visible = false;
            base.OnExit(e);
        }
    }
}
