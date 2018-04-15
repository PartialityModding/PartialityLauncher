using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using System.Security.Cryptography;
using System.Diagnostics;

namespace PartialityLauncher {
    public class ModMetadata {
        public readonly static HashAlgorithm ChecksumHasher = MD5.Create();

        public bool isEnabled;
        public bool isPatch;
        public bool isDirty;

        public string modPath;

        public Dictionary<string, HashSet<string>> modifiedClasses = new Dictionary<string, HashSet<string>>();

        public static ModMetadata GetForMod(string file) {
            string metaFilePath = Directory.GetParent( file ) + "\\" + Path.GetFileNameWithoutExtension( file ) + ".modMeta";
            string hashFilePath = Directory.GetParent( file ) + "\\" + Path.GetFileNameWithoutExtension( file ) + ".modHash";
            DebugLogger.Log( "Checking for modmeta at " + metaFilePath );
            if( !File.Exists( metaFilePath ) ) {
                GenerateHashForMod( file, hashFilePath );
                return GenerateForMod( file );
            } else {
                return ReadFromFile( metaFilePath, hashFilePath, file );
            }
        }
        public static void SaveMod(ModMetadata md) {
            string metaFilePath = Directory.GetParent( md.modPath ) + "\\" + Path.GetFileNameWithoutExtension( md.modPath ) + ".modMeta";
            string hashFilePath = Directory.GetParent( md.modPath ) + "\\" + Path.GetFileNameWithoutExtension( md.modPath ) + ".modHash";

            GenerateHashForMod( md.modPath, hashFilePath );
            WriteToFile( md, metaFilePath );
        }

        public static ModMetadata GenerateForMod(string modFile) {
            ModMetadata meta = new ModMetadata();

            meta.isEnabled = false;
            meta.modPath = modFile;
            ModuleDefinition modDef = ModuleDefinition.ReadModule( modFile );
            IEnumerable<TypeDefinition> getTypes = modDef.GetTypes();

            Type patchType = typeof( MonoMod.MonoModPatch );
            Type constructorType = typeof( MonoMod.MonoModConstructor );
            Type originalNameType = typeof( MonoMod.MonoModOriginalName );
            Type ignoreType = typeof( MonoMod.MonoModIgnore );
            string origPrefix = "orig_";

            DebugLogger.Log( string.Empty );
            DebugLogger.Log( "Generating metadata for " + Path.GetFileName( modFile ) );

            //Foreach type in the mod dll
            foreach( TypeDefinition checkType in getTypes ) {
                //If the type has a custom attribute
                if( checkType.HasCustomAttributes ) {
                    HashSet<CustomAttribute> attributes = new HashSet<CustomAttribute>( checkType.CustomAttributes );
                    //Foreach custom attribute
                    foreach( CustomAttribute ct in attributes ) {
                        try {
                            //If the attribute is [MonoModPatch]
                            if( ct.AttributeType.Name == patchType.Name ) {
                                string originalClassName = ct.ConstructorArguments[0].Value as string;
                                HashSet<string> changedFunctions = new HashSet<string>();

                                DebugLogger.Log( string.Empty );
                                DebugLogger.Log( "Adding " + originalClassName );

                                List<MethodDefinition> methods = new List<MethodDefinition>( checkType.Methods );
                                //Foreach method in this type
                                foreach( MethodDefinition methodDef in methods ) {

                                    if( methodDef.IsConstructor )
                                        continue;

                                    if( methodDef.Name.StartsWith( origPrefix ) ) {
                                        string methodEntry = originalClassName + "->" + methodDef.Name.Remove( 0, origPrefix.Length );
                                        DebugLogger.Log( "Adding method " + methodEntry );
                                        changedFunctions.Add( methodEntry );
                                    } else if( methodDef.HasCustomAttributes ) {
                                        HashSet<CustomAttribute> methodAttributes = new HashSet<CustomAttribute>( methodDef.CustomAttributes );
                                        //Foreach custom attribute on the method
                                        foreach( CustomAttribute c in methodAttributes ) {
                                            if( c.AttributeType.Name == ignoreType.Name ) {
                                                string methodEntry = originalClassName + "->" + methodDef.Name;
                                                DebugLogger.Log( "Ignoring Method " + methodEntry );
                                                break;
                                            } else if( c.AttributeType.Name == constructorType.Name ) {
                                                string methodEntry = originalClassName + "->" + "ctor_" + originalClassName;
                                                DebugLogger.Log( "Adding Constructor " + methodEntry );
                                                changedFunctions.Add( methodEntry );
                                                break;
                                            } else if( c.AttributeType.Name == originalNameType.Name ) {
                                                string methodEntry = originalClassName + "->" + methodDef.Name;
                                                DebugLogger.Log( "Adding Original Method " + methodEntry );
                                                changedFunctions.Add( methodEntry );
                                                break;
                                            }
                                        }
                                    }
                                }

                                meta.modifiedClasses.Add( originalClassName, changedFunctions );

                                //We're done looking through attributes of this class, break
                                break;
                            }
                        } catch( System.Exception e ) {
                            DebugLogger.Log( e );
                        }
                    }
                }
            }

            meta.isPatch = meta.modifiedClasses.Count > 0;
            if( meta.isPatch ) {
                meta.isDirty = true;
            }

            modDef.Dispose();

            return meta;
        }
        public static void GenerateHashForMod(string modFile, string hashFile) {
            byte[] fileData = File.ReadAllBytes( modFile );
            byte[] hash = ChecksumHasher.ComputeHash( fileData );
            File.WriteAllBytes( hashFile, hash );
        }

        public static ModMetadata ReadFromFile(string metaFile, string hashFile, string modFile) {

            //Check for hash mismatch
            bool isMatch = CompareHashes( modFile, hashFile );

            string[] text = File.ReadAllLines( metaFile );
            int index = 0;

            if( text.Length < 2 ) {
                throw new Exception( "Not enough strings in metadata file! " + metaFile );
            }

            ModMetadata meta = new ModMetadata();

            meta.isEnabled = bool.Parse( text[index++] );
            meta.isPatch = bool.Parse( text[index++] );
            meta.isDirty = bool.Parse( text[index++] );

            if( !isMatch ) {
                ModMetadata genMeta = GenerateForMod( modFile );
                genMeta.isEnabled = meta.isEnabled;
                genMeta.isPatch = meta.isPatch;
                genMeta.isDirty = true;
                meta = genMeta;

                WriteToFile( meta, metaFile );
                DebugLogger.Log( "Generated Data For " + modFile );
                return meta;
            }

            meta.modPath = modFile;

            string currentModClass = string.Empty;
            int modificationCount = -1;
            HashSet<string> currentModifications = new HashSet<string>();

            for( int i = index; i < text.Length; i++ ) {
                if( currentModClass == string.Empty ) {
                    currentModClass = text[i];
                    modificationCount = int.Parse( text[i + 1] );
                    i++;
                } else {
                    if( modificationCount <= 0 ) {
                        meta.modifiedClasses.Add( currentModClass, currentModifications );
                        currentModClass = string.Empty;
                        currentModifications = new HashSet<string>();
                        modificationCount = -1;
                        i--;
                    } else {
                        currentModifications.Add( text[i] );
                        modificationCount--;
                    }
                }
            }

            if( currentModifications.Count > 0 ) {
                meta.modifiedClasses.Add( currentModClass, currentModifications );
            }

            return meta;
        }

        public static void WriteToFile(ModMetadata meta, string path) {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine( meta.isEnabled.ToString() );
            sb.AppendLine( meta.isPatch.ToString() );
            sb.AppendLine( meta.isDirty.ToString() );

            foreach( KeyValuePair<string, HashSet<string>> modClass in meta.modifiedClasses ) {
                sb.AppendLine( modClass.Key );
                sb.AppendLine( modClass.Value.Count.ToString() );
                foreach( string s in modClass.Value ) {
                    sb.AppendLine( s );
                }
            }

            File.WriteAllText( path, sb.ToString() );
        }

        public static bool CompareHashes(string fileToHash, string hashToCompare) {
            byte[] fileData = File.ReadAllBytes( fileToHash );
            byte[] currentHash = File.ReadAllBytes( hashToCompare );
            byte[] fileDataHash = ChecksumHasher.ComputeHash( fileData );

            return SameHash( currentHash, fileDataHash );
        }
        public static bool SameHash(byte[] a, byte[] b) {
            if( a.Length != b.Length )
                return false;
            for( int i = 0; i < a.Length; i++ ) {
                if( a[i] != b[i] ) {
                    DebugLogger.Log( "Failed hash check at index " + i + " bytes where " + a[i] + " and " + b[i] );
                    return false;
                }
            }
            return true;
        }

    }
}
