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
            
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();
            GUILayout.Label("Specify one or more path directories to be added");
            GUILayout.Label("Separate multiple entries with a semicolon ';' ");
            GUILayout.Label("(e.g.: ./;./inc)" );
            EditorGUILayout.Space(); 
            EditorGUILayout.Space();  
                 EditorGUILayout.PropertyField(serializedObject.FindProperty("m_IncludePath")); 
            GUILayout.Label("(e.g.: ./ or ./netcode" );
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_TargetPath")); 
            EditorGUILayout.Separator();
            EditorGUILayout.Space(); 
            EditorGUILayout.EndVertical();
            
           // DrawDefaultInspector();
            
            EditorGUILayout.Space();
            if (GUILayout.Button("Compile"))
            {
                var proto_asset = serializedObject.targetObject as ProtoImporter;
                //EasyProtoCore.CompileProtoFile( proto_asset.assetPath ); 
            }
            
            // EditorGUILayout.Space();
            // EditorGUILayout.BeginVertical();
            // EditorGUILayout.Space();
            // GUILayout.Label("Specify one or more path directories to be added");
            // GUILayout.Label("Separate multiple entries with a semicolon ';' ");
            // GUILayout.Label("(e.g.: ./;./inc)" );
            // EditorGUILayout.BeginHorizontal();
            //     GUILayout.Label("Include:");
            //     EditorGUILayout.PropertyField(serializedObject.FindProperty("m_IncludePath"));
            // EditorGUILayout.EndHorizontal();
            // GUILayout.Label("(e.g.: ./ or ./netcode" );
            // EditorGUILayout.BeginHorizontal();
            //     GUILayout.Label(" Target:");
            //     EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OutPath"));
            // EditorGUILayout.EndHorizontal();
            // EditorGUILayout.Separator();
            // EditorGUILayout.Space(); 
            // EditorGUILayout.EndVertical();
            
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
