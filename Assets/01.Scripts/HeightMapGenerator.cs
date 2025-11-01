using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using UnityEngine;

public static class HeightMapGenerator
{
    public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCentre)
    {
        float[,] values = Noise.GenerateNoiseMap(width, height, settings._noiseSettings, sampleCentre);

        AnimationCurve heightCurveThreadSafe = new AnimationCurve(settings._heightCurve.keys);

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                // 높이 값이 0 ~ 1이기 때문에 높이 배수가 필요함.
                values[i, j] *= heightCurveThreadSafe.Evaluate(values[i, j]) * settings._heightMultiplier;

                if(values[i, j] > maxValue) maxValue = values[i, j];
                if(values[i, j] < minValue) minValue = values[i, j];
            }
        }

        return new HeightMap(values, minValue, maxValue);
    }

    
}

public struct HeightMap
{
    public readonly float[,] values;
    public readonly float minValue;
    public readonly float maxValue;

    public HeightMap(float[,] values, float minValue, float maxValue)
    {
        this.values = values;
        this.minValue = minValue;
        this.maxValue = maxValue;
    }
}
