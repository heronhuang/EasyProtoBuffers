using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;
using Warkits.ToolBox.EasyProto;

[ScriptedImporter(0, ".gconf")]
public class ConfigImporter : ScriptedImporter
{
    static string GetCurrentScriptPath()
    {
        var stackTrace = new System.Diagnostics.StackTrace(true);
        var frame      = stackTrace.GetFrame(0);
        var filePath   = frame.GetFileName();
        return filePath;
    }
    static bool IsSameDirectory(string path1, string path2)
    { 
        string fullPath1 = Path.GetFullPath(path1).TrimEnd('\\', '/');
        string fullPath2 = Path.GetFullPath(path2).TrimEnd('\\', '/'); 
        return fullPath1.Equals(fullPath2, System.StringComparison.OrdinalIgnoreCase);
    } 

    public override void OnImportAsset(AssetImportContext ctx)
    {
        string content = File.ReadAllText(ctx.assetPath);
        var assetText  = new TextAsset(content);
        ctx.AddObjectToAsset("main obj", assetText);
        ctx.SetMainObject(assetText);
        string currentBDirectory = GetCurrentScriptPath(); 
        currentBDirectory        = Path.Combine(currentBDirectory, "../../../Config"); 
        if(IsSameDirectory( currentBDirectory, Path.GetDirectoryName(ctx.assetPath)))
        {
            string version_ = content.Trim();
            version_ = version_.Length > 0?version_:"34.1";
            EasyProtoModule.Environment.SetVersion( version_ ); 
        }
        //EasyProtoModule.CompileProtoFile( ctx.assetPath );
    }
}