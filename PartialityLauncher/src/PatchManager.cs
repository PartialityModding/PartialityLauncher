using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace PartialityLauncher {
    public static class PatchManager {

        public static HashSet<string> enabledPatches = new HashSet<string>();
        public static HashSet<string> disablesPatches = new HashSet<string>();
        public static Dictionary<string, string> allAvaliableMods = new Dictionary<string, string>();

        private static object waitLock = new object();
        private static bool isWaiting = false;

        public static void LoadPatches(string gameDirectory) {
            allAvaliableMods.Clear();
            enabledPatches.Clear();
            disablesPatches.Clear();

            string patchDirectory = Path.Combine( gameDirectory + "\\Patches" );

            if( !Directory.Exists( patchDirectory ) ) {
                Directory.CreateDirectory( patchDirectory );
                return;
            }

            string[] modFiles = Directory.GetFiles( patchDirectory, "*.dll", SearchOption.AllDirectories );
            string[] disableTags = Directory.GetFiles( patchDirectory, "*.dis", SearchOption.AllDirectories );

            foreach( string s in disableTags ) {
                disablesPatches.Add( Path.GetFileNameWithoutExtension( s ) );
            }

            foreach( string s in modFiles ) {
                string fileName = Path.GetFileNameWithoutExtension( s );

                allAvaliableMods.Add( fileName, s );
                if( !disablesPatches.Contains( fileName ) )
                    enabledPatches.Add( fileName );
            }
        }

        public static void EnablePatch(string patchName) {
            if( disablesPatches.Contains( patchName ) == false )
                return;

            enabledPatches.Add( patchName );
            disablesPatches.Remove( patchName );
        }
        public static void DisablePatch(string patchName) {
            if( enabledPatches.Contains( patchName ) == false )
                return;

            enabledPatches.Remove( patchName );
            disablesPatches.Add( patchName );
        }

        public static void SavePatchInfos(string gameDirectory) {

            string patchDirectory = Path.Combine( gameDirectory + "\\Patches" );
            string[] disableTags = Directory.GetFiles( patchDirectory, "*.dis", SearchOption.AllDirectories );

            foreach( string s in disableTags ) {
                string fileName = Path.GetFileNameWithoutExtension( s );
                if( enabledPatches.Contains( fileName ) && File.Exists( s ) ) {
                    try {
                        File.Delete( s );
                    } catch( System.Exception e ) {
                        Console.WriteLine( e );
                    }
                }
            }

            foreach( string s in disablesPatches ) {
                string filePath = ( patchDirectory + "\\" + s + ".dis" );
                if( !File.Exists( filePath ) )
                    File.Create( filePath ).Close();

            }
        }

        public static void PatchGame(string gameDirectory) {

            SavePatchInfos( gameDirectory );
            RestoreBackup( Directory.GetDirectories( gameDirectory, "Managed", SearchOption.AllDirectories )[0] );
            BackupApplication( Directory.GetDirectories( gameDirectory, "Managed", SearchOption.AllDirectories )[0] );

            string MonoModEXEPath = gameDirectory + "\\MonoMod\\MonoMod.exe";

            if( !File.Exists( MonoModEXEPath ) ) {
                Console.WriteLine( "Monomod doesn't exist! It must have been deleted." );
                return;
            }

            string[] searchOptions = Directory.GetFiles( gameDirectory, "Assembly-CSharp.dll", SearchOption.AllDirectories );
            if( searchOptions.Length == 0 ) {
                Console.WriteLine( "Assembly-CSharp.dll doesn't exist. Maybe this game doesn't have one?..." );
                return;
            }

            string csharpPath = searchOptions[0];

            searchOptions = Directory.GetFiles( gameDirectory, "UnityEngine.dll", SearchOption.AllDirectories );
            if( searchOptions.Length == 0 ) {
                Console.WriteLine( "UnityEngine.dll doesn't exist. Maybe this game doesn't have one?..." );
                return;
            }

            string unityEngine = searchOptions[0];


            Process currentProcess = Process.GetCurrentProcess();
            Process monomodProcess = new Process();

            monomodProcess.StartInfo.FileName = MonoModEXEPath;
            monomodProcess.StartInfo.UseShellExecute = false;
            monomodProcess.StartInfo.RedirectStandardOutput = true;

            StringBuilder sb = new StringBuilder();

            //First, patch the UnityEngine.dll with Partiality. Partiality gets special treatement because it's special.
            {
                string moddedDLL = Path.GetDirectoryName( unityEngine ) + "\\patched_UnityEngine.dll";
                string partialityLocation = gameDirectory + "\\PartialityPatch.dll";
                string partialityModLocation = Path.GetDirectoryName( unityEngine ) + "\\Partiality.dll";

                if( File.Exists( partialityModLocation ) )
                    File.Delete( partialityModLocation );

                File.Copy( Directory.GetFiles( gameDirectory, "Partiality.dll", SearchOption.TopDirectoryOnly )[0], partialityModLocation );
                Console.WriteLine( ( '"' + unityEngine + '"' ) + " " + ( '"' + partialityLocation + '"' ) + " " + ( '"' + moddedDLL + '"' ) );
                monomodProcess.StartInfo.Arguments = ( '"' + unityEngine + '"' ) + " " + ( '"' + partialityLocation + '"' ) + " " + ( '"' + moddedDLL + '"' );

                monomodProcess.Start();
                string mmoutput = monomodProcess.StandardOutput.ReadToEnd();
                Console.WriteLine( mmoutput );
                monomodProcess.WaitForExit();

                int exitCode = monomodProcess.ExitCode;
                Console.WriteLine( "MMEC:" + exitCode );
                sb.Append( mmoutput );


                //Move modded .dll over original .dll
                File.Delete( unityEngine );
                File.Copy( moddedDLL, unityEngine );
                File.Delete( moddedDLL );
            }


            //Now, patch all the other .dll's
            {

                //Copy dependencies
                CopyFilesRecursively( new DirectoryInfo( gameDirectory + "\\PatchDependencies" ), new DirectoryInfo( Path.GetDirectoryName( csharpPath ) ) );

                string moddedDLL = Path.GetDirectoryName( csharpPath ) + "\\patched_Assembly-CSharp.dll";

                foreach( string s in enabledPatches ) {
                    if( !allAvaliableMods.ContainsKey( s ) )
                        continue;

                    string patchLocation = allAvaliableMods[s];
                    monomodProcess.StartInfo.Arguments = ( '"' + csharpPath + '"' ) + " " + ( '"' + patchLocation + '"' ) + " " + ( '"' + moddedDLL + '"' );

                    monomodProcess.Start();
                    string mmop = monomodProcess.StandardOutput.ReadToEnd();
                    Console.WriteLine( mmop );
                    monomodProcess.WaitForExit();

                    sb.AppendLine("----------------");
                    sb.AppendLine(mmop);

                    int exitCode = monomodProcess.ExitCode;
                    Console.WriteLine( "MMEC:" + exitCode );
                }

                //Move modded .dll over original .dll
                File.Delete( csharpPath );
                File.Copy( moddedDLL, csharpPath );
                File.Delete( moddedDLL );
            }

            File.WriteAllText( gameDirectory + "\\MONOMOD_OUTPUT.txt", sb.ToString() );

        }

        public static void BackupApplication(string managedFolder) {
            string backupFolder = managedFolder + "_backup";

            if( Directory.Exists( backupFolder ) ) {
                return;
            }
            DirectoryInfo to = Directory.CreateDirectory( backupFolder );
            DirectoryInfo from = new DirectoryInfo( managedFolder );

            CopyFilesRecursively( from, to );
        }

        public static void RestoreBackup(string managedFolder) {
            string backupFolder = managedFolder + "_backup";

            if( !Directory.Exists( backupFolder ) ) {
                return;
            }
            DirectoryInfo to = new DirectoryInfo( backupFolder );
            DirectoryInfo from = new DirectoryInfo( managedFolder );

            Directory.Delete( from.FullName, true );
            Directory.CreateDirectory( managedFolder );
            CopyFilesRecursively( to, from );
            Directory.Delete( backupFolder, true );
        }

        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target) {
            foreach( DirectoryInfo dir in source.GetDirectories() )
                CopyFilesRecursively( dir, target.CreateSubdirectory( dir.Name ) );
            foreach( FileInfo file in source.GetFiles() )
                file.CopyTo( Path.Combine( target.FullName, file.Name ) );
        }
    }
}