using System; 
using System.IO;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;

namespace Warkits.ToolBox.EasyProto
{ 
    public static class EasyProtoModule
    { 
        internal static class Environment
        { 
            static string m_LibraryPath;
            static string m_Version; 
            static string m_IncludePath;
            static string m_BinPath;
            static string m_ProtocPath;
            static string m_Platform;
            static string m_ProtocFilename;
            static string m_ProtocLaunchPath;
            internal static string ProtocPath    => m_ProtocPath;
            internal static string BinPath       => m_BinPath;
            internal static string IncludePath   => m_IncludePath;

            internal static string LibraryPath   => m_LibraryPath;
            internal static string Version       => m_Version;
            internal static string Plateform    => m_Platform;
            internal static string ProtocFilename => m_ProtocFilename;
            internal static string ProtocLaunchPath => m_ProtocLaunchPath;

            internal static void SetVersion(string ver)
            {
                m_Version = ver;
            }
            internal static void Init()
            { 
                string currentPath = GetCurrentScriptPath();
                currentPath        = Path.GetDirectoryName( currentPath );
                string version_    = GetProtoVersion( Path.Combine( currentPath  , "../../Config" ), "version.gconf" );
                m_Version          = version_ ?? "34.1";
                m_LibraryPath      = Path.GetFullPath(Path.Combine( currentPath    , "../../Runtime/Plugins"));
                m_Platform         = DetectPlatform();
                string protocPath  = Path.GetFullPath(Path.Combine( currentPath    , ".protoc" ));
                if (!Directory.Exists(protocPath))
                {
                    Directory.CreateDirectory(protocPath);
                }
                //./.protoc/win64
                m_ProtocPath       = Path.Combine( protocPath     , m_Platform );
                //./.protoc/win64/include
                m_IncludePath      = Path.Combine ( m_ProtocPath  , "include");
                //./.protoc/win64/bin
                m_BinPath          = Path.Combine ( m_ProtocPath  , "bin" ); 
#if UNITY_EDITOR_WIN
                m_ProtocFilename = "protoc.exe";
#else
                m_ProtocFilename = "protoc";   
#endif
                m_ProtocLaunchPath = Path.Combine( m_BinPath, m_ProtocFilename);
            } 
        }
        //path without filename
        [CanBeNull]
        static string GetProtoVersion( string folder,string versionFilename )
        {
            string versionFile  = Path.Combine( folder, versionFilename );
            if ( File.Exists(versionFile) )
            {
                foreach (var line in File.ReadLines(versionFile))
                {
                    var trimLine = line.Trim();
                    //skip commends and empty line
                    if (string.IsNullOrEmpty(trimLine) || trimLine.StartsWith("#")) continue;
                    return trimLine;
                }
            } 
            return null; 
        } 

        [InitializeOnLoadMethod]
        private static void OnPluginLoaded()
        {
            Environment.Init();
        }
          
        [MenuItem("Tools/EasyProto/Compile All Protos")]
        public static void ManualCompileAllProtos()
        {
            // Get the Assets path of the entire Unity project
            string assetsPath = UnityEngine.Application.dataPath; 
            // Search for all .proto files (including subfolders)
            string[] protoFiles = Directory.GetFiles( assetsPath, 
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

        static bool Protoc_NeedDownload()
        {
            string curVersion  =  GetProtoVersion(Environment.ProtocPath,".version.txt");
            return curVersion != Environment.Version; 
        }

        static bool Library_NeedDownload()
        {
            string curVersion  = GetProtoVersion(Environment.LibraryPath,".version.txt");
            return curVersion != Environment.Version; 
        }
        
        
        public  static void CompileProtoFile( string protoFilePath,  string target )
        {
            bool needDownloadProtoc  = Protoc_NeedDownload();
            bool needDownloadLibrary = Library_NeedDownload();
            // Verify if protoc exists
            if ( needDownloadProtoc || needDownloadLibrary )
            {
                string version     = Environment.Version;
                string protoc_url  = $"https://github.com/protocolbuffers/protobuf/releases/download/v{version}/protoc-{version}-{Environment.Plateform}.zip";
                string lib_url     = $"https://www.nuget.org/api/v2/package/Google.Protobuf/3.{version}";
                EasyDownloader.Fetch( protoc_url, lib_url,needDownloadProtoc,needDownloadLibrary );  
            } 
            else
            {
                string projectRoot     = Application.dataPath + "/../"; 
                string fullPath        = Path.GetFullPath( Path.Combine(projectRoot, protoFilePath) );
                string protoPath       = Path.GetDirectoryName( fullPath )??projectRoot;
                string fileName        = Path.GetFileNameWithoutExtension( protoFilePath );
                string outputPath      = Path.Combine( protoPath, target);
                // Check if .proto exists
                if (!File.Exists(fullPath))
                {
                    Debug.LogWarning( $"[Easy ProtoBuffer] .proto does not exist: {fullPath}");
                    return;
                }
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }
                // Execute protoc command
                System.Diagnostics.ProcessStartInfo procInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName               =  Environment.ProtocLaunchPath,
                    Arguments              =  $"--proto_path=\"{Environment.IncludePath}\" --proto_path=\"{protoPath}\" --csharp_out=\"{outputPath}\" \"{fullPath}\"",
                    WorkingDirectory       =  Environment.BinPath,
                    UseShellExecute        =  false,
                    CreateNoWindow         =  true,
                    RedirectStandardOutput =  true,
                    RedirectStandardError  =  true
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

        public static string GetCurrentScriptPath()
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
