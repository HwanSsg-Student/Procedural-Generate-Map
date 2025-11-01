using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPreview : MonoBehaviour
{
    public enum DrawMode { NoiseMap, Mesh, FalloffMap }
    public DrawMode _drawMode;
    public MeshSettings _meshSettings;
    public HeightMapSettings _heightMapSettings;
    public TextureData _textureData;
    public Material _terrainMaterial;

    [Range(0, MeshSettings._numSupportedLODs - 1)]
    [Header("LOD")] public int _editorPreviewLOD;
    [Header("자동 업데이트")] public bool _autoUpdate;

    public Renderer _textureRenderer;
    public MeshFilter _meshFilter;
    public MeshRenderer _meshRenderer;
    public void DrawMapInEditor()
    {
        _textureData.ApplyToMaterial(_terrainMaterial);
        _textureData.UpdateMeshHeights(_terrainMaterial, _heightMapSettings.MinHeight, _heightMapSettings.MaxHeight);
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(_meshSettings.numVertsPerline, _meshSettings.numVertsPerline, _heightMapSettings, Vector2.zero);

        if (_drawMode == DrawMode.NoiseMap)
        {
            DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
        }
        else if (_drawMode == DrawMode.Mesh)
        {
            DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, _meshSettings, _editorPreviewLOD));
        }
        else if (_drawMode == DrawMode.FalloffMap)
        {
            DrawTexture(TextureGenerator.TextureFromHeightMap(new HeightMap(FalloffGenerator.GenerateFalloffMap(_meshSettings.numVertsPerline), 0, 1)));
        }
    }


    public void DrawTexture(Texture2D texture)
    {
        _textureRenderer.sharedMaterial.mainTexture = texture;
        _textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10f;
        _textureRenderer.gameObject.SetActive(true);
        _meshFilter.gameObject.SetActive(false);
    }

    internal void DrawMesh(MeshData meshData)
    {
        _meshFilter.sharedMesh = meshData.CreateMesh();
        _textureRenderer.gameObject.SetActive(false);
        _meshFilter.gameObject.SetActive(true);
    }

    void OnValidate()
    {
        if (_meshSettings != null)
        {
            _meshSettings.OnValuesUpdated -= OnValuesUpdated;
            _meshSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (_heightMapSettings != null)
        {
            _heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            _heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (_textureData != null)
        {
            _textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            _textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }

    }
    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }
    void OnTextureValuesUpdated()
    {
        _textureData.ApplyToMaterial(_terrainMaterial);
    }
}
