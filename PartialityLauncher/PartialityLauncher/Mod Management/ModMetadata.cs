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
using YamlDotNet.Serialization;

namespace PartialityLauncher {
    public class ModMetadata {
        public readonly static HashAlgorithm ChecksumHasher = MD5.Create();

        public bool isEnabled;
        public bool isPatch;
        public bool isDirty;
        public bool isStandalone;

        public string modPath;

        public Dictionary<string, HashSet<string>> modifiedClasses = new Dictionary<string, HashSet<string>>();

        public static ModMetadata GetForMod(string file) {
            string metaFilePath = Path.Combine( Directory.GetParent( file ).FullName, Path.GetFileNameWithoutExtension( file ) + ".modMeta" );
            string hashFilePath = Path.Combine( Directory.GetParent( file ).FullName, Path.GetFileNameWithoutExtension( file ) + ".modHash" );
            DebugLogger.Log( "Checking for modmeta at " + metaFilePath );
            if( !File.Exists( metaFilePath ) ) {
                GenerateHashForMod( file, hashFilePath );
                return GenerateForMod( file );
            } else {
                return ReadFromFile( metaFilePath, hashFilePath, file );
            }
        }
        public static void SaveMod(ModMetadata md) {
            string metaFilePath = Path.Combine( Directory.GetParent( md.modPath ).FullName, Path.GetFileNameWithoutExtension( md.modPath ) + ".modMeta" );
            string hashFilePath = Path.Combine( Directory.GetParent( md.modPath ).FullName, Path.GetFileNameWithoutExtension( md.modPath ) + ".modHash" );

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
            if( !File.Exists( hashFile ) ) {
                GenerateHashForMod( modFile, hashFile );
            }
            bool isMatch = CompareHashes( modFile, hashFile );

            string text = File.ReadAllText( metaFile );
            int index = 0;

            ModMetadata meta = new ModMetadata();

            Deserializer ds = new Deserializer();
            ModJSONMetadata jsonMeta = ds.Deserialize<ModJSONMetadata>( text );

            meta.isEnabled = jsonMeta.isEnabled;
            meta.isPatch = jsonMeta.isPatch;
            meta.isDirty = isMatch;
            meta.isStandalone = jsonMeta.isStandalone;

            meta.modifiedClasses = new Dictionary<string, HashSet<string>>();
            foreach( KeyValuePair<string, List<string>> kvp in jsonMeta.modifiedClasses ) {
                meta.modifiedClasses[kvp.Key] = new HashSet<string>( kvp.Value );
            }

            meta.modPath = modFile;

            return meta;
        }
        public static ModMetadata ReadRawFromFile(string metaFile) {
            string text = File.ReadAllText( metaFile );
            int index = 0;

            ModMetadata meta = new ModMetadata();

            Deserializer ds = new Deserializer();
            ModJSONMetadata jsonMeta = ds.Deserialize<ModJSONMetadata>( text );

            meta.isEnabled = jsonMeta.isEnabled;
            meta.isPatch = jsonMeta.isPatch;
            meta.isDirty = jsonMeta.isDirty;
            meta.isStandalone = jsonMeta.isStandalone;

            meta.modifiedClasses = new Dictionary<string, HashSet<string>>();
            foreach( KeyValuePair<string, List<string>> kvp in jsonMeta.modifiedClasses ) {
                meta.modifiedClasses[kvp.Key] = new HashSet<string>( kvp.Value );
            }

            return meta;
        }

        public static void WriteToFile(ModMetadata meta, string path) {
            ModJSONMetadata JSON = new ModJSONMetadata() {
                isDirty = meta.isDirty,
                isEnabled = meta.isEnabled,
                isPatch = meta.isPatch,
                isStandalone = meta.isStandalone,
                modifiedClasses = new Dictionary<string, List<string>>()
            };

            foreach( KeyValuePair<string, HashSet<string>> kvp in meta.modifiedClasses ) {
                HashSet<string> modClassList = kvp.Value;
                JSON.modifiedClasses[kvp.Key] = new List<string>( modClassList );
            }

            Serializer s = new Serializer();
            string json = s.Serialize( JSON );

            File.WriteAllText( path, json );
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

        private class ModJSONMetadata {
            public bool isEnabled { get; set; }
            public bool isPatch { get; set; }
            public bool isDirty { get; set; }
            public bool isStandalone { get; set; }
            public Dictionary<string, List<string>> modifiedClasses { get; set; }
        }
    }
}
