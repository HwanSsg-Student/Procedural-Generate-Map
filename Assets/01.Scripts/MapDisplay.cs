using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Renderer _textureRenderer;
    public MeshFilter _meshFilter;
    public MeshRenderer _meshRenderer;
    public void DrawTexture(Texture2D texture)
    {
        _textureRenderer.sharedMaterial.mainTexture = texture;
        _textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    internal void DrawMesh(MeshData meshData, Texture2D texture)
    {
        _meshFilter.sharedMesh = meshData.CreateMesh();
        _meshRenderer.sharedMaterial.mainTexture = texture;
    }
}
