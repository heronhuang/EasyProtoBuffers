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
        
        public string m_TargetPath;
         
        
        public override void OnImportAsset(AssetImportContext ctx)
        {  
            string content = File.ReadAllText(ctx.assetPath);
            var assetText  = new TextAsset(content);
            ctx.AddObjectToAsset("main obj", assetText);
            ctx.SetMainObject(assetText);  
            EasyProtoCore.CompileProtoFile( ctx.assetPath, m_TargetPath ); 
        }
    }    
}
