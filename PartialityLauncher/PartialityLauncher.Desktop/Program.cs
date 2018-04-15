using System;
using Eto.Forms;
using Eto.Drawing;
using System.Collections.Generic;

namespace PartialityLauncher.Desktop {
    class Program {
        [STAThread]
        static void Main(string[] args) {
            HashSet<string> hashArgs = new HashSet<string>( args );

            if( hashArgs.Contains( "-quicklaunch" ) ) {
                try {
                    GameManager.LoadLastGame( null );
                    //GameManager.ClearMetas();
                    GameManager.LoadModMetas();
                    foreach( ModMetadata md in GameManager.modMetas ) {
                        if( md.isPatch )
                            md.isDirty = true;
                    }
                    GameManager.PatchGame();
                    GameManager.StartGame();
                } catch (System.Exception e ) {
                    DebugLogger.Log( e );
                }
                DebugLogger.WriteToFile();
            } else {
                try {
                    new Application( Eto.Platform.Detect ).Run( new MainForm( args ) );
                } catch (System.Exception e ) {
                    DebugLogger.Log(e);
                }
                GameManager.SaveAllMetadata();
                DebugLogger.WriteToFile();
            }
        }
    }
}