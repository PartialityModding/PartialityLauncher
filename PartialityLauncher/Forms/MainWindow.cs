using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PartialityLauncher {
    public partial class MainWindow: Form {

        public static List<string> logDisplay = new List<string>();
        private bool selectedList = false;
        private bool enableQueue = false;

        public MainWindow() {
            InitializeComponent();
        }

        private void Run_Game_Click(object sender, EventArgs e) {
            PatchManager.SavePatchInfos( Path.GetDirectoryName( Application.ExecutablePath ).ToString() );
            RunGameButton.Enabled = false;
            EnabledModBox.Enabled = false;
            DisabledModBox.Enabled = false;
            RefreshButton.Enabled = false;
            SwapButton.Enabled = false;
            RestoreBackupButton.Enabled = false;

            PatchManager.PatchGame( Path.GetDirectoryName( Application.ExecutablePath ).ToString() );

            GameManager.RunGame(EnableElements);
        }


        public void EnableElements(object sender, EventArgs e) {
            enableQueue = true;
            Console.WriteLine("Enable Elements");
        }

        private void MainWindow_Load(object sender, EventArgs e) {

            Timer timer = new Timer();
            timer.Interval = ( 500 ); //0.5 seconds
            timer.Tick += new EventHandler( timer_Tick );
            timer.Start();

            LoadGameStats();
            LoadPatches();
        }

        private void timer_Tick(object sender, EventArgs e) {
            foreach( Control c in Controls )
                c.Refresh();
        }

        private void LoadGameStats() {
            string executionLocation = Application.ExecutablePath;
            string gameExecutable = ExecutableFinder.FindExecutable( Path.GetDirectoryName( executionLocation ).ToString() );
            string exePath = Path.Combine( Path.GetDirectoryName( executionLocation ).ToString(), gameExecutable );

            GameLabel.Text = Path.GetFileNameWithoutExtension( exePath );
            GameIcon.Image = Icon.ExtractAssociatedIcon( exePath ).ToBitmap();
        }

        private void LoadPatches() {
            PatchManager.LoadPatches( Path.GetDirectoryName( Application.ExecutablePath ) );
            BuildPatchLists();
        }

        private void EnabledModBox_SelectedValueChanged(object sender, EventArgs e) {
            selectedList = true;
            if( EnabledModBox.SelectedItem == null )
                return;
            DisabledModBox.ClearSelected();
        }

        private void DisabledModBox_SelectedValueChanged(object sender, EventArgs e) {
            selectedList = false;
            if( DisabledModBox.SelectedItem == null )
                return;
            EnabledModBox.ClearSelected();
        }

        private void SwapButton_Click(object sender, EventArgs e) {
            try {
                string selectedPatch = (string)( selectedList ? EnabledModBox.SelectedItem : DisabledModBox.SelectedItem );

                if( selectedPatch == null || selectedPatch == string.Empty )
                    return;

                if( selectedList ) {
                    PatchManager.DisablePatch( selectedPatch );
                    BuildPatchLists();
                } else {
                    PatchManager.EnablePatch( selectedPatch );
                    BuildPatchLists();
                }
            } catch( System.Exception error ) {
                Console.WriteLine( error );
            }
        }

        private void RefreshButton_Click(object sender, EventArgs e) {
            PatchManager.SavePatchInfos( Path.GetDirectoryName( Application.ExecutablePath ).ToString() );
            PatchManager.LoadPatches( Path.GetDirectoryName( Application.ExecutablePath ) );
            BuildPatchLists();
        }

        private void BuildPatchLists() {
            EnabledModBox.Items.Clear();
            DisabledModBox.Items.Clear();

            foreach( string s in PatchManager.allAvaliableMods.Keys ) {
                if( PatchManager.enabledPatches.Contains( s ) ) {
                    EnabledModBox.Items.Add( s );
                } else {
                    DisabledModBox.Items.Add( s );
                }
            }
        }

        private void RestoreBackupButton_Click(object sender, EventArgs e) {
            string[] infs = Directory.GetDirectories( Path.GetDirectoryName( Application.ExecutablePath ), "Managed", SearchOption.AllDirectories );
            if( infs.Length > 0 ) {
                PatchManager.RestoreBackup(infs[0]);
                RestoreBackupButton.Refresh();
            }
        }

        private void RestoreBackupButton_Paint(object sender, PaintEventArgs e) {
            string[] infs = Directory.GetDirectories( Path.GetDirectoryName( Application.ExecutablePath ), "Managed", SearchOption.AllDirectories );
            if( infs.Length > 0 && Directory.Exists( infs[0] + "_backup" ) ) {
                RestoreBackupButton.Visible = true;
            } else {
                RestoreBackupButton.Visible = false;
            }
        }

        private void MainWindow_Paint(object sender, PaintEventArgs e) {

            if( enableQueue ) {
                RunGameButton.Enabled = true;
                EnabledModBox.Enabled = true;
                DisabledModBox.Enabled = true;
                RefreshButton.Enabled = true;
                SwapButton.Enabled = true;
                RestoreBackupButton.Enabled = true;

                enableQueue = false;
            }
        }
    }
}