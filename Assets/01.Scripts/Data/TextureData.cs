using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
[CreateAssetMenu()]
public class TextureData : UpdatableData
{
    const int _textureSize = 512;
    const TextureFormat _textureFormat = TextureFormat.RGB565;

    public Layer[] _layers;

    float _savedMinHeight;
    float _savedMaxHeight;

    public void ApplyToMaterial(Material material)
    {
        material.SetInt("layerCount", _layers.Length);
        material.SetColorArray("baseColors", _layers.Select(x => x._tint).ToArray());
        material.SetFloatArray("baseStartHeights", _layers.Select(x => x._startHeight).ToArray());
        material.SetFloatArray("baseBlends", _layers.Select(x => x._blendStrength).ToArray());
        material.SetFloatArray("baseColorStrength", _layers.Select(x => x._tColorStrength).ToArray());
        material.SetFloatArray("baseTextureScales", _layers.Select(x => x._textureScale).ToArray());
        Texture2DArray texturesArray = GenerateTextureArray(_layers.Select(x => x._texture).ToArray());
        material.SetTexture("baseTextures", texturesArray);

        UpdateMeshHeights(material, _savedMinHeight, _savedMaxHeight);
    }
    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
    {
        _savedMaxHeight = maxHeight;
        _savedMinHeight = minHeight;

        material.SetFloat("minHeight", minHeight);
        material.SetFloat("maxHeight", maxHeight);
    }

    Texture2DArray GenerateTextureArray(Texture2D[] textures)
    {
        // UnityEngine.Texture2DArray class
        // 여러 장의 같은 규격 텍스처를 한 묶음으로 만든 리소스 타입
        // 서로 다른 텍스처를 한 material로 그릴 때 사용
        Texture2DArray textureArray = new Texture2DArray(_textureSize, _textureSize, textures.Length, _textureFormat, true);
        for (int i = 0; i < textures.Length; i++)
        {
            textureArray.SetPixels(textures[i].GetPixels(), i);
        }

        textureArray.Apply();
        return textureArray;
    }

    [System.Serializable]
    public class Layer
    {
        public Texture2D _texture;
        public Color _tint;
        [Range(0,1)]
        public float _tColorStrength;
        [Range(0, 1)]
        public float _startHeight;
        [Range(0, 1)]
        public float _blendStrength;
        public float _textureScale;
    }
}
