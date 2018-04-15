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
            string monoModPath = executableDirectory + "\\MonoMod.exe";
            string hookGenPath = executableDirectory + "\\MonoMod.RuntimeDetour.HookGen.exe";
            string runtimeDetourDLL = "\\MonoMod.RuntimeDetour.dll";
            string mmUtilsDLL = "\\MonoMod.Utils.dll";

            string gameDirectory = Directory.GetParent( GameManager.exePath ).FullName;
            string hashesFolder = gameDirectory + "\\PartialityHashes";
            string modDependencies = gameDirectory + "\\ModDependencies";
            string dataDirectory = gameDirectory + "\\" + Path.GetFileNameWithoutExtension( GameManager.exePath ) + "_Data";
            string managedFolder = dataDirectory + "\\Managed";
            string codeDll = managedFolder + "\\Assembly-CSharp.dll";
            string hookGenDLL = managedFolder + "\\HOOKED-Assembly-CSharp.dll";
            string engineDll = managedFolder + "\\UnityEngine.dll";

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
                string moddedDLL = Path.GetDirectoryName( engineDll ) + "\\patched_UnityEngine.dll";
                string defaultPatchLocation = executableDirectory + "\\PartialityPatch.dll";
                string partialityModLocation = Path.GetDirectoryName( engineDll ) + "\\Partiality.dll";

                bool shouldPatch = false;

                if( !File.Exists( hashesFolder + "\\ENGINEHASH.hash" ) ) {
                    shouldPatch = true;
                } else {
                    shouldPatch = !ModMetadata.CompareHashes( defaultPatchLocation, hashesFolder + "\\ENGINEHASH.hash" );
                }

                //Delete mod if it exists
                if( File.Exists( partialityModLocation ) )
                    File.Delete( partialityModLocation );
                //Copy mod to folder with assembly-chsharp.dll
                File.Copy( Directory.GetFiles( executableDirectory, "Partiality.dll", SearchOption.TopDirectoryOnly )[0], partialityModLocation );

                if( shouldPatch ) {

                    //Restore backup
                    File.Delete( engineDll );
                    File.Copy( backupFolder + "\\UnityEngine.dll", engineDll );

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
                    File.WriteAllBytes( hashesFolder + "\\ENGINEHASH.hash", newHash );
                }
            }


            //Install custom patches
            {

                string[] files = Directory.GetFiles( modDependencies );


                //Copy mod dependencies
                foreach( string dependency in files ) {
                    string fileName = Path.GetFileName( dependency );
                    //Delete the dependency if it already exists
                    if( File.Exists( managedFolder + "\\" + fileName ) )
                        File.Delete( managedFolder + "\\" + fileName );
                    //Copy the file
                    File.Copy( dependency, managedFolder + "\\" + fileName );
                }

                bool shouldPatch = false;
                string moddedDLL = Path.GetDirectoryName( engineDll ) + "\\patched_Assembly-CSharp.dll";
                string epListLocation = hashesFolder + "\\ENABLEDPATCHES.enp";


                //Check if we have the same enabled/disabled mods as last time, if we do, then 
                {
                    string totalEnabledPatches = "Partiality+";
                    foreach( ModMetadata md in GameManager.modMetas ) {
                        if( md.isPatch && md.isEnabled ) {
                            totalEnabledPatches += Path.GetFileNameWithoutExtension( md.modPath ) + "+";
                        }
                    }

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

                DebugLogger.Log( shouldPatch + " patching " );

                if( shouldPatch ) {

                    string backupDll = backupFolder + "\\Assembly-CSharp.dll";

                    //Restore backup
                    File.Delete( codeDll );
                    File.Copy( backupDll, codeDll );

                    foreach( ModMetadata md in GameManager.modMetas ) {
                        if( md.isPatch && md.isEnabled ) {

                            monomodProcess.StartInfo.Arguments = ( '"' + codeDll + '"' ) + " " + ( '"' + md.modPath + '"' ) + " " + ( '"' + moddedDLL + '"' );

                            monomodProcess.Start();
                            string mmoutput = monomodProcess.StandardOutput.ReadToEnd();
                            monomodProcess.WaitForExit();

                            int exitCode = monomodProcess.ExitCode;
                            DebugLogger.Log( "MMEC:" + exitCode );
                            DebugLogger.Log( mmoutput );


                            //Replace file
                            if( File.Exists( moddedDLL ) ) {
                                //Move modded .dll over original .dll
                                File.Delete( codeDll );
                                File.Copy( moddedDLL, codeDll );
                                File.Delete( moddedDLL );
                            }
                        }
                    }

                    //Set mods to all not be dirty, and save them.
                    foreach( ModMetadata md in GameManager.modMetas ) {
                        md.isDirty = false;
                    }

                    try {
                        GameManager.SaveAllMetadata();
                    } catch( System.Exception e ) {
                        DebugLogger.Log( e );
                    }

                    //HookGen stuff
                    {

                        //Delete files if they existed, so we can update them.
                        if( File.Exists( managedFolder + runtimeDetourDLL ) )
                            File.Delete( managedFolder + runtimeDetourDLL );
                        if( File.Exists( managedFolder + mmUtilsDLL ) )
                            File.Delete( managedFolder + mmUtilsDLL );

                        //Copy files
                        File.Copy( executableDirectory + runtimeDetourDLL, managedFolder + runtimeDetourDLL );
                        File.Copy( executableDirectory + mmUtilsDLL, managedFolder + mmUtilsDLL );

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
                                gen.Generate();
                                mOut.Write( pathOut );
                            }
                            mm.Log( "[HookGen] Done." );
                        }
                    }
                }
            }

            //HookGen stuff
            if( !File.Exists( hookGenDLL ) ) {

                //Delete files if they existed, so we can update them.
                if( File.Exists( managedFolder + runtimeDetourDLL ) )
                    File.Delete( managedFolder + runtimeDetourDLL );
                if( File.Exists( managedFolder + mmUtilsDLL ) )
                    File.Delete( managedFolder + mmUtilsDLL );

                //Copy files
                File.Copy( executableDirectory + runtimeDetourDLL, managedFolder + runtimeDetourDLL );
                File.Copy( executableDirectory + mmUtilsDLL, managedFolder + mmUtilsDLL );

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