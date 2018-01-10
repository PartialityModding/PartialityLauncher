using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartialityLauncher {
    public static class ExecutableFinder {

        public const string extension = "_Data";

        public static string FindExecutable(string directory) {
            string dataFolder = Directory.GetDirectories(directory, "*_Data")[0];
            string gameEXE = Path.GetFileName( dataFolder );
            gameEXE = gameEXE.Remove(gameEXE.Length - extension.Length, extension.Length);
            gameEXE += ".exe";

            return gameEXE;
        }
    }
}