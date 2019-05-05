using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MTMeshCreator))]
public class MTMeshCreatorEditor : Editor
{
    private void DrawSetting(int idx, MTMeshLODSetting setting)
    {
        bool bFold = EditorGUILayout.Foldout(setting.bEditorUIFoldout, string.Format("LOD {0}", idx));
        if (!bFold)
        {
            int subdivision = EditorGUILayout.IntField("Subdivision(1 ~ 7)", setting.Subdivision);
            if (setting.Subdivision != subdivision)
            {
                setting.Subdivision = Mathf.Clamp(subdivision, 1, 7);
            }
            float slopeErr = EditorGUILayout.FloatField("Slope Tolerance(Max 45)", setting.SlopeAngleError);
            if (setting.SlopeAngleError != slopeErr)
            {
                setting.SlopeAngleError = Mathf.Clamp(slopeErr, 0, 45);
            }
        }
        if (setting.bEditorUIFoldout != bFold)
        {
            setting.bEditorUIFoldout = bFold;
        }
    }
    public override void OnInspectorGUI()
    {
        MTMeshCreator comp = (MTMeshCreator)target;
        // Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
        serializedObject.Update();
        base.OnInspectorGUI();
        int lodCount = comp.LOD.Length;
        int lod = EditorGUILayout.IntField("LOD (1 ~ 4)", lodCount);
        if (lod != lodCount)
        {
            lodCount = Mathf.Clamp(lod, 1, 4);
            MTMeshLODSetting[] old = comp.LOD;
            comp.LOD = new MTMeshLODSetting[lodCount];
            for(int i=0; i< lodCount; ++i)
            {
                comp.LOD[i] = new MTMeshLODSetting();
                if (i < old.Length)
                {
                    comp.LOD[i].Subdivision = old[i].Subdivision;
                    comp.LOD[i].SlopeAngleError = old[i].SlopeAngleError;
                }
            }
        }
        for (int i = 0; i < lodCount; ++i)
        {
            DrawSetting(i, comp.LOD[i]);
        }
        if (GUILayout.Button("CreateData"))
        {
            if (comp.DataName == "")
            {
                Debug.LogError("data should have a name");
                return;
            }
            comp.EditorCreateDataBegin();
            for(int i= 0; i<int.MaxValue; ++i)
            {
                comp.EditorCreateDataUpdate();
                EditorUtility.DisplayProgressBar("creating data", "scaning volumn", comp.EditorCreateDataProgress);
                if (comp.IsEditorCreateDataDone)
                    break;
            }
            comp.EditorCreateDataEnd();
            comp.EditorTessBegin();
            for (int i = 0; i < int.MaxValue; ++i)
            {
                comp.EditorTessUpdate();
                EditorUtility.DisplayProgressBar("creating data", "tessellation", comp.EditorTessProgress);
                if (comp.IsEditorTessDone)
                    break;
            }
            EditorUtility.DisplayProgressBar("saving data", "processing", 1f);
            comp.EditorTessEnd();
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }
        if (GUILayout.Button("CreatePreview"))
        {
            comp.EditorCreatePreview();
        }
        if (GUILayout.Button("ClearPreview"))
        {
            comp.EditorClearPreview();
        }
        // Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
        serializedObject.ApplyModifiedProperties();
    }
}
