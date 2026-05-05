using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

namespace Warkits.ToolBox.EasyProto
{
    internal static class EasyDownloader
    { 
        internal class DownloadJob
        {
            public string url;
            public string targetPath;
            public Func<string,string,Task<bool>> ExtractJob;
        } 
        static void WriteVersion(string targetPath)
        {
            string versionfile = Path.Combine(targetPath, ".version.txt");
            File.WriteAllText( versionfile, EasyProtoModule.Environment.Version );
        }
        public  static EasyFetchWindow m_Wnd; 
        static void ExtractAllFromZip( string zipPath, string targetPath )
        {  
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string entryFullName = entry.FullName.Replace('\\', '/'); 
                    if ( entryFullName.EndsWith('/') )
                        continue;  
                    string destFilePath = Path.Combine(targetPath, entryFullName); 
                    Directory.CreateDirectory( Path.GetDirectoryName( destFilePath ) );
                    entry.ExtractToFile( destFilePath, overwrite: true );
                }
            } 
        }
        static void ApplyChmod(string path)
        {
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
        try
        { 
            if (File.Exists(path))
            {
                Process p = new Process();
                p.StartInfo.FileName = "/bin/chmod";
                p.StartInfo.Arguments = $"+x \"{path}\"";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.WaitForExit();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
        }
#endif
        }
        
        static void ExtractNet2Only( string zipPath, string targetPath)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in archive.Entries)
                {
                    string name = entry.FullName.Replace("\\", "/");

                    if ( name == "lib/netstandard2.0/Google.Protobuf.dll")
                    {
                        string dest = Path.Combine( targetPath, "Google.Protobuf.dll" );
                        entry.ExtractToFile(dest, true);
                    }
                }
            }
        }
        public static void Fetch( string protocUrl, 
                                  string libUrl, 
                                  bool   downProtoc,
                                  bool   downloadLib )
        {

            m_Wnd = EasyFetchWindow.Create("Easy ProtoBuffer Downloader", new Vector2(480, 200), new Vector2(480, 300));
            if (downProtoc)
            {
                m_Wnd.AddJob( new  DownloadJob()
                {
                    url = protocUrl, 
                    targetPath = EasyProtoModule.Environment.ProtocPath,
                    ExtractJob = async (zipPath, targetPath) => 
                    {
                        // 调用提取 protoc 的逻辑
                        await Task.Run(() => {
                        {
                            ExtractAllFromZip(zipPath, targetPath);
                        } });
                        WriteVersion(targetPath);
                        ApplyChmod( targetPath+"/bin/protoc" );
                        File.Delete(zipPath);
                        return true;
                    }
                });   
            }
            if (downloadLib)
            {
                m_Wnd.AddJob( new  DownloadJob()
                {
                    url = libUrl,  
                    targetPath = EasyProtoModule.Environment.LibraryPath,
                    ExtractJob = async (zipPath, targetPath) => 
                    {
                        // 调用提取库的逻辑
                        await Task.Run(() =>
                        {
                            ExtractNet2Only( zipPath, targetPath );   
                        });
                        WriteVersion(targetPath);
                        File.Delete(zipPath);
                        return true;
                    }
                });  
            }
            m_Wnd.ConstructUI();
            m_Wnd.ShowUtility(); 
        } 
         
        
    }
    
    internal class EasyFetchWindow : EditorWindow
    {
        
        string platform;
        string libUrl, libTargetPath;
 
        VisualElement progressFill;
        Label  speedLabel;
        Button confirmBtn;
        Label  m_URLContent;
        List<EasyDownloader.DownloadJob> m_DownloadJobs = new(); 
        public static EasyFetchWindow Create(string title , in Vector2 minSize, in Vector2 maxSize)
        {
            EasyFetchWindow wnd   = CreateInstance<EasyFetchWindow>();
            wnd.titleContent      = new GUIContent(title); 
            wnd.minSize           = minSize;
            wnd.maxSize           = maxSize;  
            return wnd;
        }

        public void AddJob(EasyDownloader.DownloadJob job)
        {
            m_DownloadJobs.Add(job);
        }
       
         
        void OnEnable()
        {
            EditorApplication.LockReloadAssemblies();  
           
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDestroy()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.UnlockReloadAssemblies(); // 解锁编译
            EditorUtility.RequestScriptReload();
        }

        void OnEditorUpdate()
        { 
            Repaint();  
        } 
        internal void UpdateURL(string url)
        {
            m_URLContent.text = $"URL:  {url}";
        }
        
        internal void ConstructUI()
        {
            var root = rootVisualElement;
            root.Clear(); 
            root.style.paddingLeft  = 12;
            root.style.paddingRight = 12;
            root.style.paddingTop   = 12; 
            root.Add(new Label("Download <color=green>protoc</color> and <color=green>runtime</color> from official Github and Nuget link...")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize                = 14,
                    marginBottom            = 4
                }
            }); 
            root.Add(new Label($"Version: <color=yellow>{ EasyProtoModule.Environment.Version }</color>   Platform:  { EasyProtoModule.Environment.Plateform  }"));
            VisualElement space = new VisualElement();
            space.style.height = 20; 
            space.style.flexShrink = 0; 
            root.Add(space);
            m_URLContent = new Label($"URL:  https://github.com/protocolbuffers/protobuf/releases")
            {
                style = {  marginBottom = 10,unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11}  
            }; 
            root.Add(m_URLContent); 
            var progressBg                   = new VisualElement();
            progressBg.style.height          = 18;
            progressBg.style.backgroundColor = new Color(0.2f , 0.2f, 0.2f); 
            progressBg.style.overflow        = Overflow.Hidden;
            progressBg.style.flexGrow        = 1;

            progressFill              = new VisualElement();
            progressFill.style.height = 18;
            progressFill.style.width  = 0;
            progressFill.style.backgroundImage = new StyleBackground(CreateGradient());

            progressBg.Add(progressFill);

            var row                 = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems    = Align.Center;

            speedLabel                  = new Label("0 MB/s");
            speedLabel.style.marginLeft = 8;
            speedLabel.style.minWidth   = 80;

            row.Add(progressBg);
            row.Add(speedLabel);

            root.Add(row);

            // ===== Button =====
            var btnRow = new VisualElement();
            btnRow.style.flexDirection  = FlexDirection.Row;
            btnRow.style.justifyContent = Justify.FlexEnd;
            btnRow.style.marginTop      = 12;

            confirmBtn    = new Button(StartDownload) { text = "Download" };
            var cancelBtn = new Button(() =>
            { 
               Close();   
            }) { text = "Cancel" };

            btnRow.Add(confirmBtn);
            btnRow.Add(cancelBtn);

            root.Add(btnRow);
        }

       
        Texture2D CreateGradient()
        {
            int w = 256;
            Texture2D tex = new Texture2D(w, 1);

            for (int i = 0; i < w; i++)
            {
                float t = i / (float)(w - 1);
                Color c = Color.Lerp(
                    new Color(1f, 0.65f, 0.2f),
                    new Color(1f, 0.4f, 0f),
                    t
                );
                tex.SetPixel(i, 0, c);
            }

            tex.Apply();
            return tex;
        }

        private static string[] BuildUrlFallbacks(string originalUrl)
        {
            try
            { 
                if (originalUrl.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase))
                {
                    return new[]
                    {
                        originalUrl,
                        "https://ghproxy.com/" + originalUrl,
                        "https://mirror.ghproxy.com/" + originalUrl,
                        originalUrl.Replace("https://github.com/", "https://cdn.jsdelivr.net/gh/", StringComparison.OrdinalIgnoreCase)
                    };
                } 
                if (originalUrl.StartsWith("https://www.nuget.org/", StringComparison.OrdinalIgnoreCase))
                { 
                    return new[]
                    {
                        originalUrl,
                        originalUrl.Replace("https://www.nuget.org/", "https://mirror.baidu.com/nuget/", StringComparison.OrdinalIgnoreCase),
                        originalUrl.Replace("https://www.nuget.org/", "https://nuget.cdn.azure.cn/", StringComparison.OrdinalIgnoreCase)
                    };
                } 
                return new[] { originalUrl };
            }
            catch
            { 
                return new[] { originalUrl };
            }
        }
      
        async void StartDownload()
        { 
            confirmBtn.SetEnabled(false); 
            foreach (var job in m_DownloadJobs)
            {
                string zipPath = Path.Combine( job.targetPath, "temp.zip" );
                try
                {
                    if (!Directory.Exists(job.targetPath))
                        Directory.CreateDirectory(job.targetPath);
                    UpdateURL(job.url);
                    if ( await Download( job.url, zipPath ))
                    {
                        if (job.ExtractJob != null)
                        {
                            await job.ExtractJob( zipPath, job.targetPath );
                        }
                    } 
                    else
                    {
                       Debug.LogError("All downloads failed");
                        Close();
                        return;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    Close(); 
                }
            } 
            Close();  
            EasyProtoModule.ManualCompileAllProtos(); 
        }
        
        async Task<bool> Download( string url_, string path )
        {
            string[] urls   = BuildUrlFallbacks(url_); 
            try
            {
                foreach (var url in urls)
                {
                    using ( UnityWebRequest req = UnityWebRequest.Get(url) )
                    {
                        req.downloadHandler = new DownloadHandlerFile(path); 
                        var op              = req.SendWebRequest(); 
                        Stopwatch sw        = new Stopwatch();
                        sw.Start(); 
                        while (!op.isDone)
                        {
                            float p      = req.downloadProgress; 
                            double sec   = sw.Elapsed.TotalSeconds;
                            double mb    = req.downloadedBytes / (1024.0 * 1024.0);
                            double speed = sec > 0 ? mb / sec  : 0; 
                            progressFill.style.width = Length.Percent(p * 100f);
                            speedLabel.text = $"{speed:F2} MB/s"; 
                            await Task.Delay(50);
                        } 
                        if (req.result != UnityWebRequest.Result.Success)
                        {
                            continue;
                        } 
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        } 
    }
}