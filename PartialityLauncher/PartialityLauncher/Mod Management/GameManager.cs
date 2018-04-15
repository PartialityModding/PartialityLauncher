using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using Eto.Forms;

namespace PartialityLauncher {
    public static class GameManager {

        public static string exePath;
        public static string appID = "PUT THE APPID HERE";
        public static List<ModMetadata> modMetas = new List<ModMetadata>();

        public static void LoadLastGame(MainForm form) {
            string executablePath = Assembly.GetEntryAssembly().Location;
            string executableDirectory = Directory.GetParent( executablePath ).FullName;

            if( !File.Exists( executableDirectory + "\\LASTGAMELOCATION.txt" ) )
                return;

            exePath = File.ReadAllText( executableDirectory + "\\LASTGAMELOCATION.txt" );
            GetAppID( form );

            if( form != null ) {
                form.appidBox.Text = appID;
                form.gameNameLabel.Text = Path.GetFileNameWithoutExtension( exePath );
                form.FillOutMods();
                MainForm.runGameButton.Enabled = true;
            }
        }
        public static void Reset() {
            exePath = string.Empty;
            modMetas.Clear();
            appID = "PUT THE APPID HERE";
        }

        public static void LoadModMetas() {
            modMetas.Clear();
            string gameDirectory = Directory.GetParent( exePath ).FullName;
            string managedPath = gameDirectory + "\\" + Path.GetFileNameWithoutExtension( exePath ) + "_Data\\Managed";

            //Load mods
            string modFolder = gameDirectory + "\\Mods";
            string dependenciesFolder = gameDirectory + "\\ModDependencies";
            string hashesFolder = gameDirectory + "\\PartialityHashes";

            if( !Directory.Exists( modFolder ) )
                File.Create( Directory.CreateDirectory( modFolder ).FullName + "\\mods go here" ).Dispose();
            if( !Directory.Exists( dependenciesFolder ) )
                File.Create( Directory.CreateDirectory( dependenciesFolder ).FullName + "\\mod dependencies go here" ).Dispose();
            if( !Directory.Exists( hashesFolder ) )
                File.Create( Directory.CreateDirectory( hashesFolder ).FullName + "\\IGNORE THIS FOLDER! It's just data for Partiality!" ).Dispose();

            string[] files = Directory.GetFiles( modFolder );
            foreach( string s in files ) {
                DebugLogger.Log( "Checking if file is mod " + s );
                if( Path.GetExtension( s ) == ".dll" ) {
                    try {
                        modMetas.Add( ModMetadata.GetForMod( s ) );
                    } catch( Exception e ) {
                        DebugLogger.Log( e );
                    }
                }
            }
        }
        public static void ClearMetas() {
            string gameDirectory = Directory.GetParent( exePath ).FullName;
            string modFolder = gameDirectory + "\\Mods";
            string hashesFolder = gameDirectory + "\\PartialityHashes";

            string[] modFiles = Directory.GetFiles( modFolder );
            string[] hashesFiles = Directory.GetFiles( hashesFolder );

            foreach( string s in modFiles ) {
                string extension = Path.GetExtension( s );
                if( extension == ".modMeta" || extension == ".modHash" ) {
                    DebugLogger.Log( "Deleting meta or hash " + s );
                    File.Delete( s );
                }

            }

            foreach( string s in hashesFiles ) {
                File.Delete( s );
            }
        }
        public static bool IsValidGamePath(string exePath) {
            string mainPath = Directory.GetParent( exePath ).FullName;
            string managedPath = mainPath + "\\" + Path.GetFileNameWithoutExtension( exePath ) + "_Data\\Managed";

            return Directory.Exists( managedPath );
        }

        public static void PatchGame() {
            try {
                PatchManager.PatchGame();
            } catch (System.Exception e ) {
                DebugLogger.Log( e );
            }
        }
        public static void StartGame() {
            string gameDirectory = Directory.GetParent( exePath ).FullName;

            if( !File.Exists( gameDirectory + "\\appid.txt" ) && !File.Exists( gameDirectory + "\\steam_appid.txt" ) )
                File.WriteAllText( gameDirectory + "\\appid.txt", appID );

            string executablePath = Assembly.GetEntryAssembly().Location;
            string executableDirectory = Directory.GetParent( executablePath ).FullName;

            File.WriteAllText( executableDirectory + "\\LASTGAMELOCATION.txt", exePath );

            Process p = new Process();
            p.StartInfo.FileName = "steam://rungameid/" + appID;
            p.Start();
        }

        public static void SaveAllMetadata() {
            string mainPath = Directory.GetParent( exePath ).FullName;
            string modFolder = mainPath + "\\Patches";
            foreach( ModMetadata md in modMetas ) {
                ModMetadata.SaveMod( md );
            }
        }
        public static void GetAppID(Control parent) {
            string mainPath = Directory.GetParent( exePath ).FullName;

            if( File.Exists( mainPath + "\\appid.txt" ) ) {
                appID = File.ReadAllText( mainPath + "\\appid.txt" );
            } else if( File.Exists( mainPath + "\\steam_appid.txt" ) ) {
                appID = File.ReadAllText( mainPath + "\\steam_appid.txt" );
            } else {
                if( parent != null )
                    MessageBox.Show( "No APPID found! Make sure to get one! There's a tutorial in your game's folder." );
                File.WriteAllText(
                    mainPath + "\\HOW TO GET APPID.txt", "1: Go to store.steampowered.com " +
                    "2: Search for the game " +
                    "3: The number after " + '"' + "app " + '"' + " in the URL for your game is the appID for the game. (For example, Slime Rancher's URL is " + '"' + "store.steampowered.com/app/433340/Slime_Rancher/" + '"' + ", the appid is, therefore, 433340.)"
                );
            }
        }

        public static ModIncompatibilityWarning CheckForModIncompatibilities(int modIndex) {
            ModIncompatibilityWarning warning = new ModIncompatibilityWarning();

            ModMetadata checkMod = modMetas[modIndex];

            StringBuilder warningBuilder = new StringBuilder();
            StringBuilder incompatibleBuilder = new StringBuilder();

            //If the mod's not a patch, we don't have to care what it messes with.
            if( !checkMod.isPatch )
                return warning;

            int warnType = 0;

            //Compare the mod to all other mods
            for( int i = 0; i < modMetas.Count; i++ ) {
                if( i == modIndex )
                    continue;

                //Get the other mod we're comparing to
                ModMetadata otherMod = modMetas[i];

                if( !otherMod.isPatch || !otherMod.isEnabled )
                    continue;

                //The warning type will be nothing by default

                //Check all the classes the other mod modifies
                foreach( KeyValuePair<string, HashSet<string>> otherModClass in otherMod.modifiedClasses ) {
                    //If the other mod modifies a class that this mod does
                    if( checkMod.modifiedClasses.ContainsKey( otherModClass.Key ) ) {
                        //Store this mod's compares for later
                        HashSet<string> classCompare = checkMod.modifiedClasses[otherModClass.Key];
                        bool isHighWarning = false;
                        //Set warning type to 1
                        if( warnType == 0 )
                            warnType = 1;

                        //Check all the functions the other mod changes
                        foreach( string s in otherModClass.Value ) {
                            //If both mods change the same function, it as incompatible.
                            if( classCompare.Contains( s ) ) {
                                warnType = 2;
                                isHighWarning = true;
                                break;
                            }
                        }

                        if( isHighWarning ) {
                            incompatibleBuilder.AppendLine( Path.GetFileName( otherMod.modPath ) );
                        } else {
                            warningBuilder.AppendLine( Path.GetFileName( otherMod.modPath ) );
                        }

                        break;
                    }
                }
            }

            warning.warningLevel = warnType;
            warning.sameClassMods = warningBuilder.ToString();
            warning.sameFunctionMods = incompatibleBuilder.ToString();

            return warning;
        }
    }

    public class ModIncompatibilityWarning {

        public int warningLevel;
        public string sameFunctionMods;
        public string sameClassMods;

    }
}