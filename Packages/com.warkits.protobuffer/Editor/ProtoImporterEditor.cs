using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Warkits.ToolBox.EasyProto
{
    [CustomEditor(typeof(ProtoImporter))]
    [CanEditMultipleObjects]
    public class SomeImporterEditor : ScriptedImporterEditor
    {
    
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Compile"))
            {
                var proto_asset = serializedObject.targetObject as ProtoImporter;
                EasyProtoCore.CompileProtoFile( proto_asset.assetPath ); 
            }
            ApplyRevertGUI(); 
        }
    }    
}
