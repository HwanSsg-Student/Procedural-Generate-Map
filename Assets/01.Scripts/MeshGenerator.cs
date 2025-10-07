using UnityEngine;

public static class MeshGenerator
{
    // 높이 값이 0 ~ 1이기 때문에 높이값에 대한 승수 값이 필요
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail)
    {
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);

        // 매시를 생성하는 정점 증가폭
        // 일관성을 위해 메시 증가폭을 짝수로 강제함
        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;

        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * meshSimplificationIncrement;
        int meshSizeUnsimplified = borderedSize - 2;

        // 매쉬의 중심을 (0, 0, 0) 좌표로 설정하기 위해서
        // 평행 이동량을 계산 해야함
        // 유니티는 XZ 좌표계를 사용함
        // 매쉬의 중심을 (0, 0, 0) 좌표로 설정하면 좌표가 +- 대칭이라 계산이 편해짐
        // API와 잘 맞음
        // 타일/청크 배치에 용이함
        float topLeftX = (meshSizeUnsimplified - 1) / -2f;
        float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

        // 인덱스는 0부터 시작하기 때문에 (meshSize - 1)
        // ; (width - 1) / 증가폭 + 1
        int verticesPerline = (meshSize - 1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerline);

        int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;

        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                bool isBorderVertex = (y == 0) || (y == borderedSize - 1) || (x == 0) || (x == borderedSize - 1);

                if(isBorderVertex)
                {
                    vertexIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                int vertexIndex = vertexIndicesMap[x, y];

                Vector3 percent = new Vector2((x - meshSimplificationIncrement) / (float)meshSize,
                                              (y - meshSimplificationIncrement) / (float)meshSize);

                float height = heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;

                Vector3 vertexPosition = new Vector3(topLeftX + percent.x * meshSizeUnsimplified, height, topLeftZ - percent.y * meshSizeUnsimplified);
                
                meshData.AddVertex(vertexPosition, percent, vertexIndex);

                //vertex의 맨 오른쪽과 아래쪽은 삼각형을 만들 때 접근하지 않아도 됨.
                if (x < borderedSize - 1 && y < borderedSize - 1)
                {
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + meshSimplificationIncrement, y];
                    int c = vertexIndicesMap[x, y + meshSimplificationIncrement];
                    int d = vertexIndicesMap[x + meshSimplificationIncrement, y + meshSimplificationIncrement];
                    
                    meshData.AddTriangle(a,d,c);
                    meshData.AddTriangle(d,a,b);
                }

                
            }
        }

        return meshData;
    }
}

/// <summary>
/// Mesh에 대한 정보를 담고 있는 Class. <br></br>
/// Mesh가 생성될 때 생기는 법선 문제를 해결하기 위해서 Bordered Vertex도 계산해야한다. <br></br>
/// 이 Bordered Vertex는 최종 메시에서 제외되지만 법선을 올바르게 계산하기 위해 쓰인다. <br></br>
/// Bordered Vertex는 -1부터 시작하여 음수로 표현된다. <br></br>
/// 메시가 생성될 때 음수가 포함된 메시는 최종 메시에서 제외된다.
/// </summary>
public class MeshData
{
    // Unity Mesh API가 1차원 배열을 요구하기 때문에
    // Vertex를 1차원 배열로 저장해야함
    // Vertex의 크기는 Width * Height
    // Triangle의 크기는 2 * 3 * (Width - 1) * (Height - 1)

    Vector3[] _vertices;
    int[] _triangles;
    Vector2[] _uvs;

    Vector3[] _borderVertices;
    int[] _borderTriagles;

    int _triangleIndex;
    int _borderTriangleIndex;

    public MeshData(int verticesPerLine)
    {
        _vertices = new Vector3[verticesPerLine * verticesPerLine];
        _uvs = new Vector2[verticesPerLine * verticesPerLine];
        _triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];

        _borderVertices = new Vector3[verticesPerLine * 4 + 4];

        // 2 * 3 * (4 * vertexPerLine) (= 24 * vertexPerLine)
        _borderTriagles = new int[24 * verticesPerLine];

        _triangleIndex = 0;
        _borderTriangleIndex = 0;
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {
        if(vertexIndex < 0)
        {
            _borderVertices[-vertexIndex - 1] = vertexPosition;
        }
        else
        {
            _vertices[vertexIndex] = vertexPosition;
            _uvs[vertexIndex] = uv;
        }
    }

    public void AddTriangle(int a, int b, int c)
    {
        if(a < 0 || b < 0 || c < 0)
        {
            _borderTriagles[_borderTriangleIndex] = a;
            _borderTriagles[_borderTriangleIndex + 1] = b;
            _borderTriagles[_borderTriangleIndex + 2] = c;
            _borderTriangleIndex += 3;
        }
        else
        {
            _triangles[_triangleIndex] = a;
            _triangles[_triangleIndex + 1] = b;
            _triangles[_triangleIndex + 2] = c;
            _triangleIndex += 3;
        }

        
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = _vertices;
        mesh.triangles = _triangles;
        mesh.uv = _uvs;
        mesh.normals = CalculateNormals();

        return mesh;
    }


    /// <summary>
    /// 노멀은 삼각형 표면에 수직인 방향 벡터로, 각 삼각형의 조명을 계산하는 데 사용됩니다. <br></br>
    /// Unity에서 메시를 생성할 때 정점(vertex)별로 노멀을 제공해야 하며, <br></br>
    /// 이는 해당 정점에 연결된 모든 삼각형의 노멀을 평균하여 계산됩니다.
    /// </summary>
    /// <returns></returns>
    Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[_vertices.Length];
        int triangleCount = _triangles.Length / 3;
        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = _triangles[normalTriangleIndex];
            int vertexIndexB = _triangles[normalTriangleIndex + 1];
            int vertexIndexC = _triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }
        
        int borderTriangleCount = _borderTriagles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = _borderTriagles[normalTriangleIndex];
            int vertexIndexB = _borderTriagles[normalTriangleIndex + 1];
            int vertexIndexC = _borderTriagles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);

            if(vertexIndexA >= 0)
            {
                vertexNormals[vertexIndexA] += triangleNormal;
            }
            if (vertexIndexB >= 0)
            {
                vertexNormals[vertexIndexB] += triangleNormal;
            }
            if (vertexIndexC >= 0)
            {
                vertexNormals[vertexIndexC] += triangleNormal;
            }
            
        }



        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
       

        Vector3 pointA = (indexA < 0) ? _borderVertices[-indexA - 1] : _vertices[indexA];
        Vector3 pointB = (indexB < 0) ? _borderVertices[-indexB - 1] : _vertices[indexB];
        Vector3 pointC = (indexC < 0) ? _borderVertices[-indexC - 1] : _vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        // 벡터 외적
        return Vector3.Cross(sideAB, sideAC).normalized;
    }
}