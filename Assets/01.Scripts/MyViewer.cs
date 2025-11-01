using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MyViewer : MonoBehaviour
{
    [Header("Gizmo Settings")]
    public float radius = 5f;                 // 반지름
    public Color color = new Color(0, 1, 1, .6f);
    public bool solid = false;                // 채움 여부
    public bool alwaysShow = false;           // 선택 안 해도 보이게

    void OnDrawGizmos()
    {
        if (!alwaysShow) return;
        DrawSphereGizmo();
    }

    void OnDrawGizmosSelected()
    {
        if (alwaysShow) return;
        DrawSphereGizmo();
    }

    void DrawSphereGizmo()
    {
        Gizmos.color = color;
        if (solid) Gizmos.DrawSphere(transform.position, radius);
        else Gizmos.DrawWireSphere(transform.position, radius);
    }
}
