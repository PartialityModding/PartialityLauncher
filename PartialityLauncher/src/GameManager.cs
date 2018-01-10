using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace PartialityLauncher {
    public static class GameManager {

        public static void RunGame(EventHandler exitHook) {

            string applicationFolder = Path.GetDirectoryName( Application.ExecutablePath );
            string appIDPath = applicationFolder + "\\appid.txt";

            string gameEXE = ExecutableFinder.FindExecutable( applicationFolder );

            if( !File.Exists( appIDPath ) ) {
                string input = Prompt.ShowDialog( "Please provide the game's APPID (There's a file in the game's folder that can help you)", "APPID" );
                File.WriteAllText( appIDPath, input );
            }

            string appID = File.ReadAllText( appIDPath );

            Process gameProcess = new Process();
            gameProcess.StartInfo.FileName = "steam://rungameid/" + appID;

            gameProcess.Start();
            gameProcess.WaitForExit();

            Console.WriteLine("Game opened, searching for process");

            Thread.Sleep(10000);
            Process[] searchResult = Process.GetProcessesByName( Path.GetFileNameWithoutExtension( gameEXE ) );

            if(searchResult.Length == 0 ) {
                Console.WriteLine("No game process found!");
                return;
            } else if(searchResult.Length > 1){
                Console.WriteLine("More than one processes for the game found, using the first");
            } else {
                Console.WriteLine("Process found.");
            }

            gameProcess = searchResult[0];
            gameProcess.EnableRaisingEvents = true;
            gameProcess.Exited += exitHook;
        }

    }
}