using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Warkits.ToolBox.EasyProto
{
    [ScriptedImporter(0, ".proto")]
    public class ProtoImporter : ScriptedImporter
    {
        public string m_IncludePath;
        public string m_TargetPath;
        
        [CanBeNull] string GetTarget(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }
        
     
        
        public override void OnImportAsset(AssetImportContext ctx)
        {  
            string content = File.ReadAllText(ctx.assetPath);
            var assetText  = new TextAsset(content);
            ctx.AddObjectToAsset("main obj", assetText);
            ctx.SetMainObject(assetText); 
            List<string> includes = new List<string>();
            string target = GetTarget(m_TargetPath);
            EasyProtoCore.CompileProtoFile(ctx.assetPath,includes,target); 
        }
    }    
}
