using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{ 

    public enum NormalizeMode { Local, Global };

    
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, 
        int octaves, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random prng = new System.Random(seed);

        //각 옥타브가 다른 위치에서 샘플링 되기를 원함
        Vector2[] octaveOffsets = new Vector2[octaves];


        // amplitude의 합
        // 이론상 가능한(perlinValue가 모두 1) 최대 높이값
        float maxPossibleHeight = 0f;
        // 진폭
        float amplitude = 1f;
        // 주기
        float frequency = 1f;

        for (int i = 0; i < octaves; i++)
        {
            // 하한 포함, 상한 제외 규칙으로 정수 난수를 반환
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        if(scale <= 0f) scale = 0.001f; //0으로 나누면 오류가 나기 때문에 예외 처리

        //정규화를 위한 최소값과 최대값을 저장하는 변수
        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        // scale을 변경할 때 우측 상단으로 확대되는 것을 
        // 가운데로 확대 되는 것으로 변경하기 위함
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {

                amplitude = 1f;

                frequency = 1f;

                // 현재 높이 값
                float noiseHeight = 0f;

                // octave 만큼 반복
                for(int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    // 때때로 음수 값으로 높이가 조절 되면 좋겠음
                    // 0 ~ 1 사이의 값에서 음수 값을 얻기 위해 (perlinValue * 2 - 1) 하여  -> (-1 ~ 1) 값을 얻음
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                // 시드나 파라미터가 바뀌어도 같은 규칙이 같은 의미로 작동할 수 있도록 정규화가 필요함.
                // 노이즈 맵에서 가장 낮은 값과 가장 높은 값을 추적해야한다.
                if (noiseHeight > maxLocalNoiseHeight) maxLocalNoiseHeight = noiseHeight;
                else if(noiseHeight < minLocalNoiseHeight) minLocalNoiseHeight = noiseHeight;

                noiseMap[x, y] = noiseHeight;
            }
        }
        //정규화
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if(normalizeMode == NormalizeMode.Local)
                {
                    // 청크마다 최소 높이와 최대 높이가 다르기 때문에
                    // 정규화를 했을 때 청크와 청크 사이의 이음세 높이가 다름
                    // 청크를 하나만 생성했을 때는 문제가 되지 않지만
                    // 청크를 여러 개 생성할 때는 문제가 생김


                    // 구간[a, b] 안에서 value가 어디쯤 위치해 있는지를 0 ~ 1 사이 비율로 반환하는 함수
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
                else if(normalizeMode == NormalizeMode.Global)
                {
                    // 각 청크의 고유한 최소/ 최대값이 아닌
                    // 이론적으로 발생할 수 있는 노이즈의 최대 높이를 계산하여 정규화의 기준점으로 삼음

                    // 정규화를 위해  -> 
                    // (noiseMap[x, y] + 1) / (2f * maxPossibleHeight);
                    // 하지만 대부분 perlinValue가 최대값에 못 미치기 때문에 2f 를 없앰

                    float normalizeHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight);


                    // 구간 [min, max] 안에서 value가 min 보다 작으면 min을 반환하고 max보다 크면 max를 반환
                    // 최소값을 0으로 막되 최대값은 제한이 없음
                    noiseMap[x, y] = Mathf.Clamp(normalizeHeight, 0, int.MaxValue);


                }
                
            }
        }
        return noiseMap;
    }
}
