using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        //target = 사용자 지정 편집기가 검사하는 객체
        MapGenerator mapGen = (MapGenerator)target;
        if(DrawDefaultInspector())
        {
            if (mapGen._autoUpdate)
            {
                mapGen.DrawMapInEditor();
            }
        }
        if(GUILayout.Button("Generate"))
        {
            mapGen.DrawMapInEditor();
        }    
    }
}
