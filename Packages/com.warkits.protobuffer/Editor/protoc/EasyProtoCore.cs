using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Warkits.ToolBox.EasyProto
{ 
    public class EasyProtoCore
    { 
        //auto execute on plugin loaded
        [InitializeOnLoadMethod]
        private static void OnPluginLoaded()
        {
            if (!SessionState.GetBool("ProtocDetected", false))
            {
                (_,string protocPath, string protocLocate) = GetProtoCompilerFolder();
                
                if ( !File.Exists(protocLocate) )
                {
                    string url  = "https://github.com/protocolbuffers/protobuf/releases/download/v34.1/protoc-34.1-"; 
                    EasyDownloader.Fetch( url, protocPath );  
                }
                SessionState.SetBool("ProtocDetected", true);
            }
        }

        //protoc folder,protoc full path
        static (string,string,string) GetProtoCompilerFolder()
        {
            string projectRoot     = Application.dataPath + "/../";
            string scriptPath      = GetCurrentScriptPath();
            string scriptDirectory = projectRoot + Path.GetDirectoryName(scriptPath);
            string platformFolder  = DetectPlatform();
            string includePath     = scriptDirectory;
            string protocPath      = Path.Combine( includePath     , ".bin" );
            protocPath             = Path.Combine( protocPath       , platformFolder );
#if UNITY_EDITOR_WIN
            string protocLocate    = Path.Combine( protocPath,     "protoc.exe");
#else
           string protocLocate    = Path.Combine( protocPath,     "protoc");
#endif
            return (includePath, protocPath, protocLocate);
        }
        
        
        
        [MenuItem("Tools/EasyProto/Compile All Protos")]
        public static void ManualCompileAllProtos()
        {
            string[] allProtoFiles = Directory.GetFiles(Application.dataPath, "*.proto", SearchOption.AllDirectories);
            List<string> protoPaths = new List<string>();
            
            foreach (string file in allProtoFiles)
            {
                // 转换为相对路径
                string relativePath = "Assets" + file.Substring(Application.dataPath.Length);
                protoPaths.Add(relativePath);
            }
            
            if (protoPaths.Count > 0)
            {
                foreach (string assetPath in protoPaths)
                {
                      // 延迟处理，避免阻塞
                      EditorApplication.delayCall += () => ProcessProtoFileAsync(assetPath); 
                }
                //EditorUtility.DisplayDialog("EasyProto", $"Compiled {protoPaths.Count} proto files", "OK");
            }
            else
            {
                //EditorUtility.DisplayDialog("EasyProto", "No .proto files found", "OK");
            }
        }
        
        private static void ProcessProtoFileAsync(string protPath)
        {
            // 使用delayCall确保不在AssetPostprocessor中直接执行
            EditorApplication.delayCall += () =>
            {
                try
                {
                    CompileProtoFile(protPath);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ProtoImporter] 处理文件 {protPath} 时出错: {ex.Message}");
                }
            };
        }

        public  static void CompileProtoFile(string protPath)
        {
            // 检查是否已被忽略
            string itemFolderPath = Path.GetDirectoryName(Path.GetFullPath(Application.dataPath + "/../" + protPath));
            string ignoreTag      = Path.Combine( itemFolderPath  , ".ignore" );
            
            if (File.Exists(ignoreTag)) 
            {
                return;
            } 
  
            (string includePath,string protocPath, string protocLocate) = GetProtoCompilerFolder();

            // 验证protoc是否存在
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
                // 验证.proto文件是否存在
                if (!File.Exists(fullPath))
                {
                    Debug.LogWarning($"[ProtoImporter] .proto文件不存在: {fullPath}");
                    return;
                } 
                // 执行protoc命令
                System.Diagnostics.ProcessStartInfo procInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName               = protocLocate,
                    Arguments              = $"--proto_path=\"{includePath}\" --proto_path=\"{workingPath}\" --csharp_out=\"{workingPath}\" \"{fullPath}\"",
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
                        Debug.LogFormat("-><color=green>[ProtoImporter] {0}.proto 解析成功</color>", fileName);
                    
                        // 刷新Asset数据库，但批量执行以提高性能
                        EditorApplication.delayCall += () => AssetDatabase.Refresh(ImportAssetOptions.Default);
                    }
                    else
                    {
                        string errorOutput = process.StandardError.ReadToEnd();
                        Debug.LogErrorFormat("-><color=#800000ff>[ProtoImporter] {0}.proto 解析失败: {1}</color>", fileName, errorOutput);
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
