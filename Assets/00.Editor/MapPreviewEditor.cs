using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR

[CustomEditor (typeof(MapPreview))]
public class MapPreviewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        //target = 사용자 지정 편집기가 검사하는 객체
        MapPreview mapPreview = (MapPreview)target;
        if(DrawDefaultInspector())
        {
            if (mapPreview._autoUpdate)
            {
                mapPreview.DrawMapInEditor();
            }
        }
        if(GUILayout.Button("Generate"))
        {
            mapPreview.DrawMapInEditor();
        }    
    }
}

#endif