using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Warkits.ToolBox.EasyProto
{
    [CustomEditor(typeof(ProtoImporter))]
    [CanEditMultipleObjects]
    public class SomeImporterEditor : ScriptedImporterEditor
    {
        private string IncPath,TargetPath;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.Separator();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            if (GUILayout.Button("Compile"))
            {
                var proto_asset = serializedObject.targetObject as ProtoImporter;
                EasyProtoCore.CompileProtoFile( proto_asset.assetPath,proto_asset.m_TargetPath ); 
            }
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator(); 
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Separator();
            GUILayout.Label("Specify the target output directory");;
            GUILayout.Label("(e.g.: . or ./netcode, default empty means ./ )");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_TargetPath")); 
            EditorGUILayout.Separator();
            EditorGUILayout.EndVertical(); 
            EditorGUILayout.Space();
            
            serializedObject.ApplyModifiedProperties();
            
            ApplyRevertGUI();
        }


        protected override bool OnApplyRevertGUI()
        {
            return base.OnApplyRevertGUI();
        }

        protected override void Apply()
        {
            base.Apply();
        }
    }    
}
