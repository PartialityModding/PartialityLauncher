using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Reflection;
using System.Security.Cryptography;
using MonoMod;
using Mono.Cecil;
using MonoMod.RuntimeDetour.HookGen;

namespace PartialityLauncher {
    public static class PatchManager {
        public readonly static HashAlgorithm ChecksumHasher = MD5.Create();

        public static void PatchGame() {
            string executablePath = Assembly.GetEntryAssembly().Location;
            string executableDirectory = Directory.GetParent( executablePath ).FullName;
            string monoModPath = Path.Combine( executableDirectory, "MonoMod.exe" );
            string hookGenPath = Path.Combine( executableDirectory, "MonoMod.RuntimeDetour.HookGen.exe" );
            string runtimeDetourDLL = "MonoMod.RuntimeDetour.dll";
            string mmUtilsDLL = "MonoMod.Utils.dll";
            string jsonDLL = "YamlDotNet.dll";

            string gameDirectory = Directory.GetParent( GameManager.exePath ).FullName;
            string hashesFolder = Path.Combine( gameDirectory, "PartialityHashes" );
            string modDependencies = Path.Combine( gameDirectory, "ModDependencies" );
            string dataDirectory = Path.Combine( gameDirectory, Path.GetFileNameWithoutExtension( GameManager.exePath ) + "_Data" );
            string managedFolder = Path.Combine( dataDirectory, "Managed" );
            string codeDll = Path.Combine( managedFolder, "Assembly-CSharp.dll" );
            string hookGenDLL = Path.Combine( managedFolder, "HOOKS-Assembly-CSharp.dll" );
            string engineDll = Path.Combine( managedFolder, "UnityEngine.dll" );
            string coreModuleDLL = Path.Combine( managedFolder, "UnityEngine.CoreModule.dll" );

            string backupFolder = managedFolder + "_backup";

            Process currentProcess = Process.GetCurrentProcess();
            Process monomodProcess = new Process();

            monomodProcess.StartInfo.FileName = monoModPath;
            //monomodProcess.StartInfo.CreateNoWindow = true;
            monomodProcess.StartInfo.UseShellExecute = false;
            monomodProcess.StartInfo.RedirectStandardOutput = true;

            //Create backup if there isn't one
            if( !Directory.Exists( backupFolder ) ) {
                Directory.CreateDirectory( backupFolder );
                CopyFilesRecursively( managedFolder, backupFolder );
            }

            //Install the default patch for Partiality
            {

                string engineDLLName = "UnityEngine.dll";

                if( File.Exists( coreModuleDLL ) ) {
                    engineDLLName = "UnityEngine.CoreModule.dll";
                    engineDll = coreModuleDLL;
                }

                string moddedDLL = Path.Combine( Path.GetDirectoryName( engineDll ), "patched" + engineDLLName );
                string defaultPatchLocation = Path.Combine( executableDirectory, "PartialityPatch.dll" );
                string partialityModLocation = Path.Combine( Path.GetDirectoryName( engineDll ), "Partiality.dll" );

                bool shouldPatch = false;

                if( !File.Exists( Path.Combine( hashesFolder, "ENGINEHASH.hash" ) ) ) {
                    shouldPatch = true;
                } else {
                    shouldPatch = !ModMetadata.CompareHashes( defaultPatchLocation, Path.Combine( hashesFolder, "ENGINEHASH.hash" ) );
                }

                //Delete mod if it exists
                if( File.Exists( partialityModLocation ) )
                    File.Delete( partialityModLocation );
                //Copy mod to folder with assembly-chsharp.dll
                File.Copy( Path.Combine( executableDirectory, "Partiality.dll" ), partialityModLocation );

                if( shouldPatch ) {

                    //Restore backup
                    File.Delete( engineDll );
                    File.Copy( Path.Combine( backupFolder, engineDLLName ), engineDll );

                    //Set monomod arguments to "[UnityEngine.dll] [PartialityPatch.dll] [patched_UnityEngine.dll]"

                    monomodProcess.StartInfo.Arguments = ( '"' + engineDll + '"' ) + " " + ( '"' + defaultPatchLocation + '"' ) + " " + ( '"' + moddedDLL + '"' );

                    monomodProcess.Start();
                    string mmoutput = monomodProcess.StandardOutput.ReadToEnd();
                    Console.WriteLine( mmoutput );
                    monomodProcess.WaitForExit();

                    int exitCode = monomodProcess.ExitCode;
                    Console.WriteLine( "MMEC:" + exitCode );
                    Console.WriteLine( mmoutput );

                    //Replace file
                    if( File.Exists( moddedDLL ) ) {
                        //Move modded .dll over original .dll
                        File.Delete( engineDll );
                        File.Copy( moddedDLL, engineDll );
                        File.Delete( moddedDLL );
                    }

                    byte[] newHash = ChecksumHasher.ComputeHash( File.ReadAllBytes( defaultPatchLocation ) );
                    File.WriteAllBytes( Path.Combine( hashesFolder, "ENGINEHASH.hash" ), newHash );
                }
            }

            //Install custom patches
            {

                string[] files = Directory.GetFiles( modDependencies );


                //Copy mod dependencies
                foreach( string dependency in files ) {
                    string fileName = Path.GetFileName( dependency );
                    //Delete the dependency if it already exists
                    if( File.Exists( Path.Combine( managedFolder, fileName ) ) )
                        File.Delete( Path.Combine( managedFolder, fileName ) );
                    //Copy the file
                    File.Copy( dependency, Path.Combine( managedFolder, fileName ) );
                }

                bool shouldPatch = false;
                string moddedDLL = Path.Combine( Path.GetDirectoryName( engineDll ), "patched_Assembly-CSharp.dll" );
                string epListLocation = Path.Combine( hashesFolder, "ENABLEDPATCHES.enp" );


                //Check if we have the same enabled/disabled mods as last time, if we do, then 
                {
                    string totalEnabledPatches = "Partiality+";
                    foreach( ModMetadata md in GameManager.modMetas ) {
                        if( ( md.isStandalone || md.isPatch ) && md.isEnabled ) {
                            totalEnabledPatches += Path.GetFileNameWithoutExtension( md.modPath ) + "+";
                        }
                    }

                    DebugLogger.Log( totalEnabledPatches );

                    if( File.Exists( epListLocation ) ) {
                        string getList = File.ReadAllText( epListLocation );
                        shouldPatch = getList != totalEnabledPatches;
                        if( shouldPatch )
                            File.WriteAllText( epListLocation, totalEnabledPatches );
                    } else {
                        shouldPatch = true;
                        File.WriteAllText( epListLocation, totalEnabledPatches );
                    }
                }

                //If all the same mods are enabled, check if any mods are dirty. If they are, we gotta re-patch.
                if( !shouldPatch ) {
                    foreach( ModMetadata md in GameManager.modMetas ) {
                        if( md.isDirty ) {
                            shouldPatch = true;
                            break;
                        }
                    }
                }

                if( shouldPatch ) {
                    DebugLogger.Log( "Patching Assembly-CSharp" );

                    string backupDll = Path.Combine( backupFolder, "Assembly-CSharp.dll" );

                    foreach( ModMetadata md in GameManager.modMetas ) {
                        if( md.isStandalone && md.isEnabled )
                            backupDll = md.modPath;
                    }

                    //Restore backup
                    File.Delete( codeDll );
                    File.Copy( backupDll, codeDll );

                    List<string> failedPatches = new List<string>();

                    foreach( ModMetadata md in GameManager.modMetas ) {
                        if( md.isPatch && md.isEnabled ) {

                            monomodProcess.StartInfo.Arguments = ( '"' + codeDll + '"' ) + " " + ( '"' + md.modPath + '"' ) + " " + ( '"' + moddedDLL + '"' );

                            monomodProcess.Start();
                            string mmoutput = monomodProcess.StandardOutput.ReadToEnd();
                            monomodProcess.WaitForExit();

                            int exitCode = monomodProcess.ExitCode;
                            DebugLogger.Log( "MMEC:" + exitCode );
                            DebugLogger.Log( mmoutput );

                            if( exitCode != 0 ) {
                                failedPatches.Add( Path.GetFileNameWithoutExtension( md.modPath ) );
                            }

                            //Replace file
                            if( File.Exists( moddedDLL ) ) {
                                //Move modded .dll over original .dll
                                File.Delete( codeDll );
                                File.Copy( moddedDLL, codeDll );
                                File.Delete( moddedDLL );
                            }
                        }
                    }

                    if( failedPatches.Count > 0 )
                        Eto.Forms.MessageBox.Show( "Some mods failed to apply correctly! Please send your LOG.txt (in the Partiality folder) to someone who can help, probably from the people who made the mod." );

                    //Set mods to all not be dirty, and save them.
                    foreach( ModMetadata md in GameManager.modMetas ) {
                        md.isDirty = false;
                    }

                    try {
                        GameManager.SaveAllMetadata();
                    } catch( System.Exception e ) {
                        DebugLogger.Log( e );
                    }
                }
            }

            //HookGen stuff
            {

                //Delete Legacy DLL
                if( File.Exists( Path.Combine( managedFolder, "HOOKS-Assembly-CSharp.dll" ) ) )
                    File.Delete( Path.Combine( managedFolder, "HOOKS-Assembly-CSharp.dll" ) );

                if( File.Exists( hookGenDLL ) ) {
                    File.Delete( hookGenDLL );
                }

                //Delete files if they existed, so we can update them.
                if( File.Exists( Path.Combine( managedFolder, runtimeDetourDLL ) ) )
                    File.Delete( Path.Combine( managedFolder, runtimeDetourDLL ) );
                if( File.Exists( Path.Combine( managedFolder, mmUtilsDLL ) ) )
                    File.Delete( Path.Combine( managedFolder, mmUtilsDLL ) );
                if( File.Exists( Path.Combine( managedFolder, jsonDLL ) ) )
                    File.Delete( Path.Combine( managedFolder, jsonDLL ) );

                //Copy files
                File.Copy( Path.Combine( executableDirectory, runtimeDetourDLL ), Path.Combine( managedFolder, runtimeDetourDLL ) );
                File.Copy( Path.Combine( executableDirectory, mmUtilsDLL ), Path.Combine( managedFolder, mmUtilsDLL ) );
                File.Copy( Path.Combine( executableDirectory, jsonDLL ), Path.Combine( managedFolder, jsonDLL ) );

                string pathIn = codeDll;
                string pathOut = hookGenDLL;

                using( MonoModder mm = new MonoModder {
                    InputPath = pathIn,
                    OutputPath = pathOut
                } ) {
                    mm.Read();
                    mm.MapDependencies();
                    if( File.Exists( pathOut ) ) {
                        mm.Log( string.Format( "Clearing {0}", pathOut ) );
                        File.Delete( pathOut );
                    }
                    mm.Log( "[HookGen] Starting HookGenerator" );
                    HookGenerator gen = new HookGenerator( mm, Path.GetFileName( pathOut ) );
                    using( ModuleDefinition mOut = gen.OutputModule ) {
                        gen.HookPrivate = true;
                        gen.Generate();
                        mOut.Write( pathOut );
                    }
                    mm.Log( "[HookGen] Done." );
                }
            }

            //File.WriteAllText( gameDirectory + "\\PARTIALITY_OUTPUT.txt", );
        }

        public static void CopyFilesRecursively(string source, string target) {
            CopyFilesRecursively( new DirectoryInfo( source ), new DirectoryInfo( target ) );
        }
        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target) {
            foreach( DirectoryInfo dir in source.GetDirectories() )
                CopyFilesRecursively( dir, target.CreateSubdirectory( dir.Name ) );
            foreach( FileInfo file in source.GetFiles() )
                file.CopyTo( Path.Combine( target.FullName, file.Name ) );
        }
    }
}