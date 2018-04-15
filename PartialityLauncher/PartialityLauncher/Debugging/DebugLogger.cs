using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace PartialityLauncher {
    public static class DebugLogger {

        private static StringBuilder sb = new StringBuilder();

        public static void Log(object obj) {
            Debug.WriteLine( obj.ToString() );
            sb.AppendLine( obj.ToString() );
        }

        public static void WriteToFile() {
            string executablePath = Assembly.GetEntryAssembly().Location;
            string executableDirectory = Directory.GetParent( executablePath ).FullName;
            string aboveDirectory = Directory.GetParent( executableDirectory ).FullName;

            string filePath = aboveDirectory + "\\LOG.txt";
            File.WriteAllText( filePath, sb.ToString() );
        }
    }
}