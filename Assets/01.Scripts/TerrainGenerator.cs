using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


public class TerrainGenerator : MonoBehaviour
{
    
    const float _viewerMoveThreshholdForChunkUpdate = 25f;
    const float _sqrViewerMoveThreshholdForChunkUpdate = _viewerMoveThreshholdForChunkUpdate * _viewerMoveThreshholdForChunkUpdate;

    public int _colliderLODIndex;
    public LODInfo[] _detailLevels;
    
    public MeshSettings _meshSettings;
    public HeightMapSettings _heightMapSettings;
    public TextureData _textureSettings;

    public Transform _viewer;
    public Material _mapMaterial;

    Vector2 _viewerPosition;
    Vector2 _viewerPositionOld;
  
    float _meshWorldSize;
    int _chunksVisibleInViewDst;

    Dictionary<Vector2, TerrainChunk> _terrainChunkDic = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> _visibleTerrainChunks = new List<TerrainChunk>();

    void Start()
    {
        _textureSettings.ApplyToMaterial(_mapMaterial);
        _textureSettings.UpdateMeshHeights(_mapMaterial, _heightMapSettings.MinHeight, _heightMapSettings.MaxHeight);

        float maxViewDst = _detailLevels[_detailLevels.Length - 1].visibleDstThreshold;
        _meshWorldSize = _meshSettings.meshWorldSize;
        _chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / _meshWorldSize);

        UpdateVisibleChunks();
    }
    void Update()
    {
        _viewerPosition = new Vector2(_viewer.position.x, _viewer.position.z);
        if (_viewerPosition != _viewerPositionOld)
        {
            foreach (TerrainChunk chunk in _visibleTerrainChunks)
            {
                chunk.UpdateCollisionMesh();
            }
        }
        if((_viewerPositionOld - _viewerPosition).sqrMagnitude > _sqrViewerMoveThreshholdForChunkUpdate)
        {
            _viewerPositionOld = _viewerPosition;
            UpdateVisibleChunks();
        }
    }
    void UpdateVisibleChunks()
    {
        HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();

        for (int i = _visibleTerrainChunks.Count - 1; i >= 0; i--)
        {
            alreadyUpdatedChunkCoords.Add(_visibleTerrainChunks[i]._coord);
            _visibleTerrainChunks[i].UpdateTerrainChunk();
        }

        int currentChunkCoordX = Mathf.RoundToInt(_viewerPosition.x / _meshWorldSize);
        int currentChunkCoordY = Mathf.RoundToInt(_viewerPosition.y / _meshWorldSize);

        for (int yOffset = -_chunksVisibleInViewDst; yOffset <= _chunksVisibleInViewDst; yOffset++)
        {
            for(int xOffset = -_chunksVisibleInViewDst; xOffset <= _chunksVisibleInViewDst; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if(!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
                {
                    if (_terrainChunkDic.ContainsKey(viewedChunkCoord))
                    {
                        _terrainChunkDic[viewedChunkCoord].UpdateTerrainChunk();
                    }
                    else
                    {
                        TerrainChunk newChunk = new TerrainChunk(viewedChunkCoord, _heightMapSettings, _meshSettings, _detailLevels,
                            _colliderLODIndex, transform, _viewer, _mapMaterial);
                        _terrainChunkDic.Add(viewedChunkCoord, newChunk);
                        
                        newChunk.OnVisibilityChanged += OnTerrainChunkVisibilityChanged;
                        newChunk.Load();
                    }
                }
            }
        }
    }    

    void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible)
    {
        if(isVisible)
        {
            _visibleTerrainChunks.Add(chunk);
        }
        else
        {
            _visibleTerrainChunks.Remove(chunk);
        }
    }
}

[System.Serializable]
public struct LODInfo
{
    [Range(0, MeshSettings._numSupportedLODs - 1)]
    public int lod;

    // 가시 거리 임계값
    public float visibleDstThreshold;

    public float sqrVisibleDstThreshold
    {
        get
        {
            return visibleDstThreshold * visibleDstThreshold;
        }
    }
}