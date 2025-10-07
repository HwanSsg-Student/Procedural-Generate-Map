using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


public class EndlessTerrain : MonoBehaviour
{
    const float _scale = 2f;
    const float _viewerMoveThreshholdForChunkUpdate = 25f;
    const float _sqrViewerMoveThreshholdForChunkUpdate = _viewerMoveThreshholdForChunkUpdate * _viewerMoveThreshholdForChunkUpdate - 25f;

    public LODInfo[] _detailLevels;
    // static 으로 선언하여 TerrainChunk class에서 사용할 수 있도록 함
    public static float _maxViewDst;
    public Transform _viewer;
    public Material _mapMaterial;
    public static Vector2 _viewerPosition;

    Vector2 _viewerPositionOld;

    // static 으로 선언하여 TerrainChunk class에서 사용할 수 있도록 함
    static MapGenerator _mapGenerator;

    int _chunkSize;
    int _chunksVisibleInViewDst;

    Dictionary<Vector2, TerrainChunk> _terrainChunkDic = new Dictionary<Vector2, TerrainChunk>();

    // static 으로 선언하여 TerrainChunk class에서 사용할 수 있도록 함
    static List<TerrainChunk> _terrainChunksVisibleLastUpdate = new List<TerrainChunk>();



    void Start()
    {
        _mapGenerator = FindObjectOfType<MapGenerator>();
        _maxViewDst = _detailLevels[_detailLevels.Length - 1].visibleDstThreshold;
        _chunkSize = MapGenerator._mapChunkSize - 1;
        _chunksVisibleInViewDst = Mathf.RoundToInt(_maxViewDst / _chunkSize);
        UpdateVisibleChunks();
    }
    void Update()
    {
        _viewerPosition = new Vector2(_viewer.position.x, _viewer.position.z) / _scale;

        if((_viewerPositionOld - _viewerPosition).sqrMagnitude > _sqrViewerMoveThreshholdForChunkUpdate)
        {
            _viewerPositionOld = _viewerPosition;
            UpdateVisibleChunks();
        }
    }
    void UpdateVisibleChunks()
    {
        for (int i = 0; i < _terrainChunksVisibleLastUpdate.Count; i++)
        {
            _terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        _terrainChunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(_viewerPosition.x / _chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(_viewerPosition.y / _chunkSize);

        for (int yOffset = -_chunksVisibleInViewDst; yOffset <= _chunksVisibleInViewDst; yOffset++)
        {
            for(int xOffset = -_chunksVisibleInViewDst; xOffset <= _chunksVisibleInViewDst; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);


                if(_terrainChunkDic.ContainsKey(viewedChunkCoord))
                {
                    _terrainChunkDic[viewedChunkCoord].UpdateTerrainChunk();

                }
                else
                {
                    _terrainChunkDic.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, _chunkSize, _detailLevels, transform, _mapMaterial));
                    
                }

            }
        }
    }

    public class TerrainChunk
    {
        GameObject _meshObject;
        Vector2 _position;
        Bounds _bounds;
        
        MeshRenderer _meshRenderer;
        MeshFilter _meshFilter;
        MeshCollider _meshCollider;

        LODMesh[] _lodMeshes;
        LODInfo[] _detailLevels;

        MapData _mapData;
        bool _mapDataReceived;
        int _previousLODIndex = -1;
        public bool IsVisible => _meshObject.activeSelf;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels , Transform parent, Material material)
        {
            _detailLevels = detailLevels;

            _position = coord * size;
            _bounds = new Bounds(_position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(_position.x, 0, _position.y);

            _meshObject = new GameObject("TerrainChunk");
            _meshRenderer = _meshObject.AddComponent<MeshRenderer>();
            _meshFilter = _meshObject.AddComponent<MeshFilter>();
            _meshCollider = _meshObject.AddComponent<MeshCollider>();
            _meshRenderer.material = material;
            
            _meshObject.transform.position = positionV3 * _scale;
            _meshObject.transform.localScale = Vector3.one * _scale;
            _meshObject.transform.parent = parent;
            SetVisible(false);

            _lodMeshes = new LODMesh[_detailLevels.Length];
            for (int i = 0; i < _lodMeshes.Length; i++)
            {
                _lodMeshes[i] = new LODMesh(_detailLevels[i].lod, UpdateTerrainChunk);
            }

            _mapGenerator.RequestMapData(OnMapDataReceived, _position);
        }

        // 바로 데이터를 가져오지 않는 이유는 
        // LOD 때문
        // 맵 데이터를 가져온 다음 필요한 세부 수준 메시를 생성하는 데 사용할 수 있다.
        void OnMapDataReceived(MapData mapData)
        {
            _mapData = mapData;
            _mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColourMap(_mapData.colourMap, MapGenerator._mapChunkSize, MapGenerator._mapChunkSize);
            _meshRenderer.material.mainTexture = texture;


            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk()
        {
            if(_mapDataReceived)
            {
                float viewerDstFromNearestEdge = Mathf.Sqrt(_bounds.SqrDistance(_viewerPosition));
                bool visible = viewerDstFromNearestEdge <= _maxViewDst;

                if (visible)
                {
                    int lodIndex = 0;

                    for (int i = 0; i < _detailLevels.Length - 1; i++)
                    {
                        if (viewerDstFromNearestEdge > _detailLevels[i].visibleDstThreshold)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    // 플레이어와의 거리가 달라져 LOD를 갱신해야할 때만 업데이트 하기 위함.
                    if (lodIndex != _previousLODIndex)
                    {
                        LODMesh lodMesh = _lodMeshes[lodIndex];
                        if (lodMesh._hasMesh)
                        {
                            _previousLODIndex = lodIndex;   
                            _meshFilter.mesh = lodMesh._mesh;
                            _meshCollider.sharedMesh = lodMesh._mesh;
                        }
                        else if (!lodMesh._hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(_mapData);
                        }
                    }
                    _terrainChunksVisibleLastUpdate.Add(this);
                }

                SetVisible(visible);
            }
        }
        public void SetVisible(bool visible)
        {
            _meshObject.SetActive(visible);
        }

        
        /// <summary>
        /// MapGenerator에서 자체 메시를 가져오는 것만 담당
        /// </summary>
        class LODMesh
        {
            public Mesh _mesh;
            public bool _hasRequestedMesh;
            public bool _hasMesh;

            int _lod;

            System.Action _updateCallback;

            public LODMesh(int lod, System.Action updateCallback)
            {
                _lod = lod;
                _updateCallback = updateCallback;
            }
            public void RequestMesh(MapData mapData)
            {
                _hasRequestedMesh = true;
                _mapGenerator.RequestMeshData(OnMeshDataReceived, mapData, _lod);
            }
            void OnMeshDataReceived(MeshData meshData)
            {
                _mesh = meshData.CreateMesh();
                _hasMesh = true;

                _updateCallback();
            }

        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;

        // 가시 거리 임계값
        public float visibleDstThreshold;
    }
}
