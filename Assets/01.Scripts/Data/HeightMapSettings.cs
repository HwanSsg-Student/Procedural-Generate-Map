using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu()]
public class HeightMapSettings : UpdatableData
{
    public NoiseSettings _noiseSettings;

    [Header("높이 배율")] public float _heightMultiplier;
    [Header("고도 보정 곡선")] public AnimationCurve _heightCurve;
    [Header("섬")] public bool _useFalloff;

    public float MinHeight
    {
        get
        {
            return _heightMultiplier * _heightCurve.Evaluate(0);
        }
    }
    public float MaxHeight
    {
        get
        {
            return _heightMultiplier * _heightCurve.Evaluate(1.1f);
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        _noiseSettings.ValidateValues();
        base.OnValidate();
    }
#endif
}
