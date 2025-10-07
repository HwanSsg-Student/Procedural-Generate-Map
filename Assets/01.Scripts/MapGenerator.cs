using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;


public class MapGenerator : MonoBehaviour
{
    #region [Variable]
    public DrawMode _drawMode;
    public Noise.NormalizeMode _normalizeMode;
    // Unity���� �޽� �ϳ��� �ִ� ���� ���� 255���� ������.
    // ���� �������� (width - 1)�� �μ�
    // (width - 1) = 240�� �� �μ��� 1, 2, 3, 4, 6, ... �� �پ��ϱ� ������ ûũ ũ��� 241�� ������
    public const int _mapChunkSize = 239;

    [Range(0,6)]
    [Header("LOD")]public int _editorPreviewLOD;
    [Header("ũ��")] public float _noiseScale;
    [Header("������ ����")] public int _octaves;
    [Range(0f, 1f)]
    [Header("�������� ���� ����")] public float _persistance;
    [Header("�������� �ֱ� ����")] public float _lacunarity;
    [Header("�õ�")] public int _seed;
    [Header("������")] public Vector2 _offset;
    [Header("���� ����")] public TerrainType[] _regions;
    [Header("���� ����")] public float _meshHeightMultiplier;
    [Header("�� ���� �")] public AnimationCurve _meshHeightCurve;
    [Header("��")] public bool _useFalloff;
    [Header("�ڵ� ������Ʈ")] public bool _autoUpdate;

    float[,] _falloffMap;


    Queue<MapTheadInfo<MapData>> _mapDataThreadInfoQueue = new Queue<MapTheadInfo<MapData>>();
    Queue<MapTheadInfo<MeshData>> _meshDataThreadInfoQueue = new Queue<MapTheadInfo<MeshData>>();
    #endregion

    void Awake()
    {
        _falloffMap = FalloffGenerator.GenerateFalloffMap(_mapChunkSize);
    }

    void Update()
    {
        if(_mapDataThreadInfoQueue.Count > 0)
        {
            for(int i = 0; i < _mapDataThreadInfoQueue.Count; i++)
            {
                MapTheadInfo<MapData> threadInfo = _mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
        if(_meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < _meshDataThreadInfoQueue.Count; i++)
            {
                MapTheadInfo<MeshData> threadInfo = _meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }



    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();

        if (_drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (_drawMode == DrawMode.ColourMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, _mapChunkSize, _mapChunkSize));
        }
        else if (_drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, _meshHeightMultiplier, _meshHeightCurve, _editorPreviewLOD),
                TextureGenerator.TextureFromColourMap(mapData.colourMap, _mapChunkSize, _mapChunkSize));
        }
        else if (_drawMode == DrawMode.FalloffMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(_mapChunkSize)));
        }
    }
    public void RequestMapData(Action<MapData> callback, Vector2 centre)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(callback, centre);
        };

        new Thread(threadStart).Start();
    }
    void MapDataThread(Action<MapData> callback, Vector2 centre)
    {
        MapData mapData = GenerateMapData(centre);

        lock (_mapDataThreadInfoQueue)
        {
            _mapDataThreadInfoQueue.Enqueue(new MapTheadInfo<MapData>(callback, mapData));
        }
    }
    public void RequestMeshData(Action<MeshData> callback, MapData mapData, int lod)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(callback, mapData, lod);
        };

        new Thread(threadStart).Start();
    }
    void MeshDataThread(Action<MeshData> callback, MapData mapData, int lod)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, _meshHeightMultiplier, _meshHeightCurve, lod);
        lock (_meshDataThreadInfoQueue)
        {
            _meshDataThreadInfoQueue.Enqueue(new MapTheadInfo<MeshData>(callback, meshData));
        }
    }
    

    MapData GenerateMapData(Vector2 centre)
    {
        //borderLine���� ���� ���� (+ 2)
        float[,] noiseMap = Noise.GenerateNoiseMap(_mapChunkSize + 2, _mapChunkSize + 2, _seed,
            _noiseScale, _octaves, _persistance, _lacunarity, centre + _offset, _normalizeMode);

        Color[] colourMap = new Color[_mapChunkSize * _mapChunkSize];

        for (int y = 0; y < _mapChunkSize; y++)
        {
            for(int x = 0; x < _mapChunkSize; x++)
            {
                if(_useFalloff)
                {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - _falloffMap[x, y]);
                }


                float currentHeight = noiseMap[x, y];

                for(int i = 0; i < _regions.Length; i++)
                {
                    if(currentHeight >= _regions[i].height)
                    {
                        colourMap[y * _mapChunkSize + x] = _regions[i].colour;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colourMap);
    }

    void OnValidate()
    {
        if(_lacunarity < 1) _lacunarity = 1;
        if(_octaves < 0) _octaves = 0;

        if(_useFalloff) _falloffMap = FalloffGenerator.GenerateFalloffMap(_mapChunkSize);
    }

    struct MapTheadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapTheadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color colour;

}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colourMap;

    public MapData(float[,] heightMap, Color[] colourMap)
    {
        this.heightMap = heightMap;
        this.colourMap = colourMap;
    }
}
public enum DrawMode { NoiseMap, ColourMap, Mesh, FalloffMap };