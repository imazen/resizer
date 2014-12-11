using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace COMInstaller {
    public partial class Main : Form {
        public Main() {
            InitializeComponent();
            Refresh();
        }

        public string InstallFolder {
            get {
                return Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).TrimEnd('\\','/')
                    , "ImageResizingNet"), "v3");

            }
        }

        DllCollection oldFiles = null;
        DllCollection newFiles = null;
        Comparer comparer = null;
        public void Refresh() {
            string[] newFolders = new string[]{".\\"};
            if (Path.GetFileName(Directory.GetCurrentDirectory()).Equals("trial")) 
                newFolders = new string[]{".\\","..\\release"};
            if (Path.GetFileName(Directory.GetCurrentDirectory()).Equals("release")) 
                newFolders = new string[]{".\\","..\\trial"};

            newFiles = new DllCollection(newFolders);
            oldFiles = new DllCollection(new string[]{InstallFolder});
            comparer = new Comparer(oldFiles, newFiles);
            txtAnalysis.Text = "Install path: " + InstallFolder + "\r\n\r\n" + comparer.GetAnalysis();
            btnUninstall.Enabled = (oldFiles.Count > 0);
        }

        public void Uninstall() {
            btnRefresh.Enabled = btnInstall.Enabled = btnUninstall.Enabled = false;
            txtLog.Text = txtAnalysis.Text = "Uninstalling - please be patient- don't click random stuff.";
            Application.DoEvents();
            txtLog.Text = new Uninstaller(oldFiles).Uninstall();
            btnRefresh.Enabled = btnInstall.Enabled = btnUninstall.Enabled = true;
            Refresh();
        }

        public void Install() {
            btnRefresh.Enabled = btnInstall.Enabled = btnUninstall.Enabled = false;
            txtLog.Text = txtAnalysis.Text = "Installing - please be patient- don't click random stuff.";
            Application.DoEvents();
            txtLog.Text = new Installer(newFiles, InstallFolder).Install();
            btnRefresh.Enabled = btnInstall.Enabled = btnUninstall.Enabled = true;
            Refresh();
        }

        private void btnUninstall_Click(object sender, EventArgs e) {
            Uninstall();
        }

        private void btnRefresh_Click(object sender, EventArgs e) {
            Refresh();
        }

        private void btnInstall_Click(object sender, EventArgs e) {
            if (oldFiles.Count > 0) {

                DialogResult result = MessageBox.Show("Uninstall existing version (suggested)?", "Uninstall existing version?", MessageBoxButtons.YesNoCancel);
                if (result == System.Windows.Forms.DialogResult.Cancel) return;
                if (result == System.Windows.Forms.DialogResult.Yes) {
                    Uninstall();
                    return;
                }
            }
            Install();
        }

    }
}
