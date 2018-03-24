using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PartialityLauncher {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {

            HashSet<string> args = new HashSet<string>();
            foreach( String s in Environment.GetCommandLineArgs() )
                args.Add( s );

            if( args.Contains( "-quicklaunch" ) ) {
                PatchManager.LoadPatches( Path.GetDirectoryName( Application.ExecutablePath ) );
                PatchManager.PatchGame( Path.GetDirectoryName( Application.ExecutablePath ) );

                GameManager.RunGame( delegate (object send, EventArgs e) { } );
                return;
            } else {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault( false );
                Application.Run( new MainWindow() );
            }
        }
    }
}