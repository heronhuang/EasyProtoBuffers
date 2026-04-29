using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Threading.Tasks;
using System; 
using Debug = UnityEngine.Debug;

namespace Warkits.ToolBox.EasyProto
{
    public static class EasyDownloader
    { 
        public static void Fetch(string url, string targetPath)
        {
            string platform = EasyProtoCore.DetectPlatform();
            string finalTarget = targetPath;
            //string finalTarget = Path.Combine(targetPath, platform);
            string baseUrl = url + platform + ".zip";

            string[] urls = BuildUrlFallbacks(baseUrl);

            EasyFetchWindow.Open(urls, finalTarget, platform);
        }
 

        static string[] BuildUrlFallbacks(string githubUrl)
        {
            try
            {
                return new string[]
                {
                    githubUrl,
                    "https://ghproxy.com/" + githubUrl,
                    "https://mirror.ghproxy.com/" + githubUrl,
                    githubUrl.Replace("https://github.com/", "https://cdn.jsdelivr.net/gh/")
                };
            }
            catch
            {
                return new string[] { githubUrl };
            }
        }
    }

    public class EasyFetchWindow : EditorWindow
    {
        string[] urls;
        string targetPath;
        string platform;

        VisualElement progressFill;
        Label speedLabel;
        Button confirmBtn;

        bool isDownloading = false;
        private static bool opened = false;
        public static void Open(string[] urls, string targetPath, string platform)
        {
            if (opened) return;
            opened = true;
            var wnd = CreateInstance<EasyFetchWindow>();
            wnd.titleContent = new GUIContent("Easy Proto Downloader");
            wnd.urls = urls;
            wnd.targetPath = targetPath;
            wnd.platform = platform;
            wnd.minSize = new Vector2(480, 200);
            wnd.maxSize = new Vector2(480, 300); // 👉 固定高度
            wnd.ShowUtility();
            wnd.Init();
           
        }
        void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        void OnEditorUpdate()
        {
            if (isDownloading)
            {
                Repaint(); // 👉 强制刷新UI（关键）
            }
        }
        void Init()
        {
            var root = rootVisualElement;
            root.Clear();

            root.style.paddingLeft = 12;
            root.style.paddingRight = 12;
            root.style.paddingTop = 12;

            root.Add(new Label("Easy Proto需要从Github下载")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 14,
                    marginBottom = 4
                }
            });

            root.Add(new Label($"Platform: {platform}"));

            root.Add(new Label(urls[0])
            {
                style = { fontSize = 10, marginBottom = 10 }
            });

            // ===== 渐变进度条 =====
            var progressBg = new VisualElement();
            progressBg.style.height = 18;
            progressBg.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            //progressBg.style.borderr = 6;
            progressBg.style.overflow = Overflow.Hidden;
            progressBg.style.flexGrow = 1;

            progressFill = new VisualElement();
            progressFill.style.height = 18;
            progressFill.style.width = 0;
            progressFill.style.backgroundImage = new StyleBackground(CreateGradient());

            progressBg.Add(progressFill);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            speedLabel = new Label("0 MB/s");
            speedLabel.style.marginLeft = 8;
            speedLabel.style.minWidth = 80;

            row.Add(progressBg);
            row.Add(speedLabel);

            root.Add(row);

            // ===== 按钮 =====
            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.justifyContent = Justify.FlexEnd;
            btnRow.style.marginTop = 12;

            confirmBtn = new Button(StartDownload) { text = "确认" };
            var cancelBtn = new Button(() =>
            {
                if (!isDownloading)
                {
                    Close();    opened = false;
                }
            }) { text = "取消" };

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

        async void StartDownload()
        {
            isDownloading = true;
            confirmBtn.SetEnabled(false);

            string zipPath = Path.Combine(targetPath, "temp.zip");

            try
            {
                if (!Directory.Exists(targetPath))
                    Directory.CreateDirectory(targetPath);

                bool success = false;

                foreach (var url in urls)
                {
                    Debug.Log("Try: " + url);
                    if (await Download(url, zipPath))
                    {
                        success = true;
                        break;
                    }
                }

                if (!success)
                {
                    Debug.LogError("All downloads failed");
                    Close();
                    opened = false;
                    return;
                }

                ExtractProtocOnly(zipPath, targetPath);

                ApplyChmod(targetPath);

                File.Delete(zipPath);

                Debug.Log("EasyFetch Done");
                Close(); opened = false;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Close();opened = false;
            }
        }

        async Task<bool> Download(string url, string path)
        {
            try
            {
                using (UnityWebRequest req = UnityWebRequest.Get(url))
                {
                    req.downloadHandler = new DownloadHandlerFile(path);

                    var op = req.SendWebRequest();

                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    while (!op.isDone)
                    {
                        float p = req.downloadProgress;

                        double sec = sw.Elapsed.TotalSeconds;
                        double mb = req.downloadedBytes / (1024.0 * 1024.0);
                        double speed = sec > 0 ? mb / sec : 0;

                        progressFill.style.width = Length.Percent(p * 100f);
                        speedLabel.text = $"{speed:F2} MB/s";

                        await Task.Delay(50);
                    }

                    if (req.result != UnityWebRequest.Result.Success)
                        return false;

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        void ExtractProtocOnly(string zipPath, string targetPath)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in archive.Entries)
                {
                    string name = entry.FullName.Replace("\\", "/");

                    if (name == "bin/protoc" || name == "bin/protoc.exe")
                    {
                        string dest = Path.Combine(targetPath, Path.GetFileName(name));
                        entry.ExtractToFile(dest, true);
                    }
                }
            }
        }

        void ApplyChmod(string path)
        {
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
        try
        {
            string exe = Path.Combine(path, "protoc");
            if (File.Exists(exe))
            {
                Process p = new Process();
                p.StartInfo.FileName = "/bin/chmod";
                p.StartInfo.Arguments = $"+x \"{exe}\"";
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
    }
}