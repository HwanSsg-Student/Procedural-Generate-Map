using UnityEngine;

public class TerrainChunk
{
    const float _colliderGenerationDistanceThreshold = 5;
    public event System.Action<TerrainChunk, bool> OnVisibilityChanged;
    public Vector2 _coord;

    GameObject _meshObject;
    Vector2 _sampleCentre;
    Bounds _bounds;

    MeshRenderer _meshRenderer;
    MeshFilter _meshFilter;
    MeshCollider _meshCollider;

    LODMesh[] _lodMeshes;
    LODInfo[] _detailLevels;

    HeightMap _heightMap;
    bool _heightMapReceived;
    bool _hasSetCollider;
    int _previousLODIndex = -1;
    int _colliderLODIndex;

    float _maxViewDst;


    HeightMapSettings _heightMapSettings;
    MeshSettings _meshSettings;

    Transform _viewer;

    public bool IsVisible => _meshObject.activeSelf;
    Vector2 ViewerPosition => new Vector2(_viewer.position.x, _viewer.position.z);

    public TerrainChunk(Vector2 coord, HeightMapSettings heightMapSettings, MeshSettings meshSettings,
        LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer ,Material material)
    {
        _coord = coord;
        _detailLevels = detailLevels;
        _colliderLODIndex = colliderLODIndex;
        _heightMapSettings = heightMapSettings;
        _meshSettings = meshSettings;
        _viewer = viewer;

        _sampleCentre = coord * meshSettings.meshWorldSize / meshSettings._meshScale;
        Vector2 position = coord * meshSettings.meshWorldSize;
        _bounds = new Bounds(position, Vector2.one * meshSettings.meshWorldSize);

        _meshObject = new GameObject("TerrainChunk");
        _meshRenderer = _meshObject.AddComponent<MeshRenderer>();
        _meshFilter = _meshObject.AddComponent<MeshFilter>();
        _meshCollider = _meshObject.AddComponent<MeshCollider>();
        _meshRenderer.material = material;

        _meshObject.transform.position = new Vector3(position.x, 0, position.y);
        _meshObject.transform.parent = parent;
        SetVisible(false);

        _lodMeshes = new LODMesh[_detailLevels.Length];
        for (int i = 0; i < _lodMeshes.Length; i++)
        {
            _lodMeshes[i] = new LODMesh(_detailLevels[i].lod);
            _lodMeshes[i]._updateCallback += UpdateTerrainChunk;

            if (i == _colliderLODIndex)
            {
                _lodMeshes[i]._updateCallback += UpdateCollisionMesh;
            }
        }
        _maxViewDst = _detailLevels[_detailLevels.Length - 1].visibleDstThreshold;
    }

    

    void OnHeightMapReceived(object heightMapObject)
    {
        // 바로 데이터를 가져오지 않는 이유는 
        // LOD 때문
        // 맵 데이터를 가져온 다음 필요한 세부 수준 메시를 생성하는 데 사용할 수 있다
        _heightMap = (HeightMap)heightMapObject;
        _heightMapReceived = true;
        UpdateTerrainChunk();
    }
    public void Load()
    {
        ThreadDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(_meshSettings.numVertsPerline, _meshSettings.numVertsPerline,
            _heightMapSettings, _sampleCentre)
        , OnHeightMapReceived);

    }
    public void UpdateTerrainChunk()
    {
        if (_heightMapReceived)
        {
            float viewerDstFromNearestEdge = Mathf.Sqrt(_bounds.SqrDistance(ViewerPosition));

            bool wasVisible = IsVisible;
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
                        if(lodIndex != _colliderLODIndex)
                        {
                            _meshCollider.sharedMesh = null;
                            _hasSetCollider = false;
                        }
                           
                        _previousLODIndex = lodIndex;
                        _meshFilter.mesh = lodMesh._mesh;

                    }
                    else if (!lodMesh._hasRequestedMesh)
                    {
                        lodMesh.RequestMesh(_heightMap,_meshSettings);
                    }
                }


            }

            if (wasVisible != visible)
            {
                SetVisible(visible);
                if(OnVisibilityChanged != null)
                {
                    OnVisibilityChanged(this, visible);
                }
            }

        }
    }
    public void UpdateCollisionMesh()
    {
        if (!_hasSetCollider)
        {
            float sqrDstFromViwerToEdge = _bounds.SqrDistance(ViewerPosition);

            if (sqrDstFromViwerToEdge < _detailLevels[_colliderLODIndex].sqrVisibleDstThreshold)
            {
                if (!_lodMeshes[_colliderLODIndex]._hasRequestedMesh)
                {
                    _lodMeshes[_colliderLODIndex].RequestMesh(_heightMap, _meshSettings);
                }
            }

            if (sqrDstFromViwerToEdge < _colliderGenerationDistanceThreshold * _colliderGenerationDistanceThreshold)
            {
                if (_lodMeshes[_colliderLODIndex]._hasMesh)
                {
                    _meshCollider.sharedMesh = _lodMeshes[_colliderLODIndex]._mesh;
                    _hasSetCollider = true;
                }
            }
        }
    }
    public void SetVisible(bool visible)
    {
        _meshObject.SetActive(visible);
    }

    class LODMesh
    {

        public Mesh _mesh;
        public bool _hasRequestedMesh;
        public bool _hasMesh;

        int _lod;

        public event System.Action _updateCallback;

        public LODMesh(int lod)
        {
            _lod = lod;
        }
        public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
        {
            _hasRequestedMesh = true;
            ThreadDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, _lod), OnMeshDataReceived);
        }
        void OnMeshDataReceived(object meshData)
        {
            _mesh = ((MeshData)meshData).CreateMesh();
            _hasMesh = true;

            _updateCallback();
        }

    }
}