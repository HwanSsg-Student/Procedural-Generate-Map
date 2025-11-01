using UnityEngine;

[CreateAssetMenu()]
public class MeshSettings : UpdatableData
{
    public const int _numSupportedLODs = 5;
    public const int _numSupportedChunkSizes = 9;
    public const int _numSupportedFlatshadedChunkSizes = 3;
    public static readonly int[] _supportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };

    [Header("메시 크기")] public float _meshScale = 2.5f;
    [Header("플랫 쉐이딩")] public bool _useFlatShading;

    [Range(0, _numSupportedChunkSizes - 1)]
    public int _chunkSizeIndex;
    [Range(0, _numSupportedFlatshadedChunkSizes - 1)]
    public int _flatshadedChunkSizeIndex;



    // Unity에서 메시 하나당 최대 정점 수를 255개로 제한함.
    // 정점 증가폭은 (width - 1)의 인수
    // (width - 1) = 240일 때 인수가 1, 2, 3, 4, 6, ... 로 다양하기 때문에 청크 크기는 241이 적합함
    // +++ 원래 241이었는데 borderLine 때문에 - 2 계산한 값. = 239 였음
    // ++++ 이후 다시 borderLine을 포함한 값으로 만들기 위해 - 1 -> + 1 이 됨. (최종적으로 + 2 한 셈)
    // +++++ borderLine을 out of mesh vertices로 재정의함.
    // +++++ main vertices와 out of mesh vertices 사이에 mesh edge vertices를 추가 (다시 + 2).

    public int numVertsPerline
    {
        get
        {
            return _supportedChunkSizes[(_useFlatShading) ? _flatshadedChunkSizeIndex : _chunkSizeIndex] + 5;
        }
    }

    // (width - 1) - borderLine
    // = (width - 1) - 2
    // = (width - 3)
    public float meshWorldSize
    {
        get
        {
            return (numVertsPerline - 3) * _meshScale;
        }
    }    
}
