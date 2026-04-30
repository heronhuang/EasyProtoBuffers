using System; 
using System.IO; 
using UnityEngine;
using UnityEditor;

namespace Warkits.ToolBox.EasyProto
{ 
    public class EasyProtoModule
    { 
        // Auto execute on plugin loaded
        [InitializeOnLoadMethod]
        private static void OnPluginLoaded()
        {
           // if (!SessionState.GetBool("ProtocDetected", false))
            {
                (_,string protocPath, string protocLocate) = GetProtoCompilerFolder();
                
                if ( !File.Exists(protocLocate) )
                {
                    string url  = "https://github.com/protocolbuffers/protobuf/releases/download/v34.1/protoc-34.1-"; 
                    EasyDownloader.Fetch( url, protocPath );  
                }
            //    SessionState.SetBool("ProtocDetected", true);
            }
        }

        // Include folder, protoc folder, protoc full path
        static (string,string,string) GetProtoCompilerFolder()
        {
            string projectRoot     = Application.dataPath + "/../";
            string scriptPath      = GetCurrentScriptPath();
            string scriptDirectory = projectRoot + Path.GetDirectoryName(scriptPath);
            string platformFolder  = DetectPlatform();
            string includePath     = Path.Combine( scriptDirectory ,".inc");
            string protocPath      = Path.Combine( scriptDirectory ,".bin" );
            protocPath             = Path.Combine( protocPath  ,platformFolder );
#if UNITY_EDITOR_WIN
            string protocLocate    = Path.Combine( protocPath  ,"protoc.exe");
#else
           string protocLocate    = Path.Combine( protocPath   ,"protoc");
#endif
            return (includePath, protocPath, protocLocate);
        }
        
        [MenuItem("Tools/EasyProto/Compile All Protos")]
        public static void ManualCompileAllProtos()
        {
            // Get the Assets path of the entire Unity project
            string assetsPath = UnityEngine.Application.dataPath; 
            // Search for all .proto files (including subfolders)
            string[] protoFiles = Directory.GetFiles(
                assetsPath, 
                "*.proto", 
                SearchOption.AllDirectories
            );

            int count = 0;

            // Traverse and re-import
            foreach (string filePath in protoFiles)
            {
                // Convert system path to Unity recognizable Asset path (key!)
                string assetPath = filePath.Replace(assetsPath, "Assets"); 
                // Force re-import → automatically trigger your CustomImporter
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate); 
                count++;
            }  
            Debug.Log($"[East ProtoBuffer] Compile completed! Total: {count} .proto files processed.");
        }
         
        public  static void CompileProtoFile( string protPath,  string target )
        {
            string itemFolderPath = Path.GetDirectoryName(Path.GetFullPath(Application.dataPath + "/../" + protPath));
            if (target == null) target = "";
            // .ignore exists in current directory, all files in this folder will be ignored
            string ignoreTag      = Path.Combine( itemFolderPath  , ".ignore" );
            if (File.Exists(ignoreTag)) 
            {
                return;
            } 
            (string includePath,string protocPath, string protocLocate) = GetProtoCompilerFolder();

            // Verify if protoc exists
            if ( !File.Exists(protocLocate) )
            {
                string url  = "https://github.com/protocolbuffers/protobuf/releases/download/v34.1/protoc-34.1-"; 
                EasyDownloader.Fetch( url, protocPath );  
            }
            else
            {
                string projectRoot     = Application.dataPath + "/../"; 
                string fullPath        = Path.GetFullPath(Path.Combine(projectRoot  , protPath));
                string workingPath     = Path.GetDirectoryName(fullPath);
                string fileName        = Path.GetFileNameWithoutExtension(protPath);
                string outputPath      = Path.Combine( workingPath, target);
                // Check if .proto exists
                if (!File.Exists(fullPath))
                {
                    Debug.LogWarning( $"[Easy protoBuffer] .proto does not exist: {fullPath}");
                    return;
                }

                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }
                // Execute protoc command
                System.Diagnostics.ProcessStartInfo procInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName               = protocLocate,
                    Arguments              = $"--proto_path=\"{includePath}\" --proto_path=\"{workingPath}\" --csharp_out=\"{outputPath}\" \"{fullPath}\"",
                    WorkingDirectory       = workingPath,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true
                };

                using (var process = System.Diagnostics.Process.Start(procInfo))
                {
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        Debug.LogFormat("-><color=green>[East ProtoBuffer] {0}.proto Compile Successes!</color>", fileName);
                    
                        // Refresh Asset database, but batch execute to improve performance
                        EditorApplication.delayCall += () => AssetDatabase.Refresh(ImportAssetOptions.Default);
                    }
                    else
                    {
                        string errorOutput = process.StandardError.ReadToEnd();
                        Debug.LogErrorFormat("-><color=#800000ff>[East ProtoBuffer] {0}.proto Failed to Compile !: {1}</color>", fileName, errorOutput);
                    }
                }
            } 
        }

        private static string GetCurrentScriptPath()
        {
            var stackTrace = new System.Diagnostics.StackTrace(true);
            var frame      = stackTrace.GetFrame(0);
            var filePath   = frame.GetFileName();
            return filePath;
        }
        public  static string DetectPlatform()
        {
#if UNITY_EDITOR_WIN
            return IntPtr.Size == 8 ? "win64" : "win32";
#elif UNITY_EDITOR_OSX
        return "osx-universal_binary";
#elif UNITY_EDITOR_LINUX
        if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture ==
            System.Runtime.InteropServices.Architecture.Arm64)
            return "linux-aarch_64";

        return IntPtr.Size == 8 ? "linux-x86_64" : "linux-x86_32";
#else
        return "win64";
#endif
        }
     
    }
}
