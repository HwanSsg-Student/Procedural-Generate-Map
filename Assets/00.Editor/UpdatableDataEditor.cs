using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//true 를 하면 파생 클래스에서도 가능

#if UNITY_EDITOR

[CustomEditor(typeof(UpdatableData), true)]
public class UpdatableDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        UpdatableData data = (UpdatableData)target;

        if (GUILayout.Button("Update"))
        {
            data.NotifyOfUpdateValues();
            EditorUtility.SetDirty(target);
        }
    }
}
#endif