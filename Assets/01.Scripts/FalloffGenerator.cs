using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FalloffGenerator
{
    public static float[,] GenerateFalloffMap(int size)
    {
        float[,] map = new float[size, size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                // 중앙으로 옮기기 위해서 * 2 - 1 연산
                // -1 ~ 1 사이의 값을 얻음.
                float x = j / (float)size * 2 - 1; 
                float y = i / (float)size * 2 - 1;

                // 절대값이 1에 가까울수록 가장자리에 가깝고, 0에 가까울수록 중앙에 가깝다.

                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                map[j, i] = Evaluate(value);
            }
        }
        return map;
    }
    static float Evaluate(float value)
    {
        float a = 3;
        float b = 2.2f;

        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }

}