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

        //�� ��Ÿ�갡 �ٸ� ��ġ���� ���ø� �Ǳ⸦ ����
        Vector2[] octaveOffsets = new Vector2[octaves];


        // amplitude�� ��
        // �̷л� ������(perlinValue�� ��� 1) �ִ� ���̰�
        float maxPossibleHeight = 0f;
        // ����
        float amplitude = 1f;
        // �ֱ�
        float frequency = 1f;

        for (int i = 0; i < octaves; i++)
        {
            // ���� ����, ���� ���� ��Ģ���� ���� ������ ��ȯ
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        if(scale <= 0f) scale = 0.001f; //0���� ������ ������ ���� ������ ���� ó��

        //����ȭ�� ���� �ּҰ��� �ִ밪�� �����ϴ� ����
        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        // scale�� ������ �� ���� ������� Ȯ��Ǵ� ���� 
        // ����� Ȯ�� �Ǵ� ������ �����ϱ� ����
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {

                amplitude = 1f;

                frequency = 1f;

                // ���� ���� ��
                float noiseHeight = 0f;

                // octave ��ŭ �ݺ�
                for(int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    // ������ ���� ������ ���̰� ���� �Ǹ� ������
                    // 0 ~ 1 ������ ������ ���� ���� ��� ���� (perlinValue * 2 - 1) �Ͽ�  -> (-1 ~ 1) ���� ����
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                // �õ峪 �Ķ���Ͱ� �ٲ� ���� ��Ģ�� ���� �ǹ̷� �۵��� �� �ֵ��� ����ȭ�� �ʿ���.
                // ������ �ʿ��� ���� ���� ���� ���� ���� ���� �����ؾ��Ѵ�.
                if (noiseHeight > maxLocalNoiseHeight) maxLocalNoiseHeight = noiseHeight;
                else if(noiseHeight < minLocalNoiseHeight) minLocalNoiseHeight = noiseHeight;

                noiseMap[x, y] = noiseHeight;
            }
        }
        //����ȭ
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if(normalizeMode == NormalizeMode.Local)
                {
                    // ûũ���� �ּ� ���̿� �ִ� ���̰� �ٸ��� ������
                    // ����ȭ�� ���� �� ûũ�� ûũ ������ ������ ���̰� �ٸ�
                    // ûũ�� �ϳ��� �������� ���� ������ ���� ������
                    // ûũ�� ���� �� ������ ���� ������ ����


                    // ����[a, b] �ȿ��� value�� ����� ��ġ�� �ִ����� 0 ~ 1 ���� ������ ��ȯ�ϴ� �Լ�
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
                else if(normalizeMode == NormalizeMode.Global)
                {
                    // �� ûũ�� ������ �ּ�/ �ִ밪�� �ƴ�
                    // �̷������� �߻��� �� �ִ� �������� �ִ� ���̸� ����Ͽ� ����ȭ�� ���������� ����

                    // ����ȭ�� ����  -> 
                    // (noiseMap[x, y] + 1) / (2f * maxPossibleHeight);
                    // ������ ��κ� perlinValue�� �ִ밪�� �� ��ġ�� ������ 2f �� ����

                    float normalizeHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight);


                    // ���� [min, max] �ȿ��� value�� min ���� ������ min�� ��ȯ�ϰ� max���� ũ�� max�� ��ȯ
                    // �ּҰ��� 0���� ���� �ִ밪�� ������ ����
                    noiseMap[x, y] = Mathf.Clamp(normalizeHeight, 0, int.MaxValue);


                }
                
            }
        }
        return noiseMap;
    }
}
