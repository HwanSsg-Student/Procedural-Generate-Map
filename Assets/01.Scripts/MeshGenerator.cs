using System;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings, int levelOfDetail)
    {
        // 매시를 생성하는 정점 증가폭
        // 일관성을 위해 메시 증가폭을 짝수로 강제함
        int skipIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;

        int numVertsPerline = meshSettings.numVertsPerline;


        // 매쉬의 중심을 (0, 0, 0) 좌표로 설정하기 위해서
        // 평행 이동량을 계산 해야함
        // 유니티는 XZ 좌표계를 사용함
        // 매쉬의 중심을 (0, 0, 0) 좌표로 설정하면 좌표가 +- 대칭이라 계산이 편해짐
        // API와 잘 맞음
        // 타일/청크 배치에 용이함
        // (-1, 1) * (meshWorldSize/2f) = (- meshWorldSize / 2f , meshWorldSize / 2f)
        Vector2 topLeftX = new Vector2(-1, 1) * meshSettings.meshWorldSize / 2f;

        MeshData meshData = new MeshData(numVertsPerline, skipIncrement, meshSettings._useFlatShading);

        int[,] vertexIndicesMap = new int[numVertsPerline, numVertsPerline];
        int meshVertexIndex = 0;
        int outOfMeshVertexIndex = -1;

        for (int y = 0; y < numVertsPerline; y ++)
        {
            for (int x = 0; x < numVertsPerline; x ++)
            {
                bool isOutOfMeshVertex = (y == 0) || (y == numVertsPerline - 1) || (x == 0) || (x == numVertsPerline - 1);
                bool isSkippedVertex = (x > 2) && x < (numVertsPerline - 3) && (y > 2) && (y < numVertsPerline - 3)
                    && ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);


                if (isOutOfMeshVertex)
                {
                    vertexIndicesMap[x, y] = outOfMeshVertexIndex;
                    outOfMeshVertexIndex--;
                }
                else if(!isSkippedVertex)
                {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int y = 0; y < numVertsPerline; y++)
        {
            for (int x = 0; x < numVertsPerline; x++)
            {
                // skipped vertices라 정의.
                // 테두리에 있는 vertices를 edge connection vertices라 정의.
                bool isSkippedVertex = (x > 2) && x < (numVertsPerline - 3) && (y > 2) && (y < numVertsPerline - 3) 
                    && ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);

                if (!isSkippedVertex)
                {
                    bool isOutOfMeshVertex = (y == 0) || (y == numVertsPerline - 1) || (x == 0) || (x == numVertsPerline - 1);
                    bool isMeshEdgeVertex = ((y == 1) || (y == numVertsPerline - 2) || (x == 1) || (x == numVertsPerline - 2)) && !isOutOfMeshVertex;
                    bool isMainVertex = (x - 2) % skipIncrement == 0 && (y - 2) % skipIncrement == 0 && !isOutOfMeshVertex && !isMeshEdgeVertex;
                    bool isEdgeConnectionVertex = (y == 2 || y == numVertsPerline - 3 || x == 2 || x == numVertsPerline - 3) && !isOutOfMeshVertex && !isMeshEdgeVertex && !isMainVertex;

                    int vertexIndex = vertexIndicesMap[x, y];

                    Vector2 percent = new Vector2(x - 1, y - 1) / (numVertsPerline - 3);
                    Vector2 vertexPosition2D = topLeftX + new Vector2(percent.x, -percent.y) * meshSettings.meshWorldSize;

                   

                    float height = heightMap[x, y];

                    if (isEdgeConnectionVertex)
                    {
                        bool isVertical = x == 2 || x == numVertsPerline - 3;
                        int dstToMainVertexA = ((isVertical) ? (y - 2 ) : (x - 2)) % skipIncrement;
                        int dstToMainVertexB = skipIncrement - dstToMainVertexA;
                        float dstPercentFromAToB = dstToMainVertexA / (float)skipIncrement;

                        float heightMainVertexA = heightMap[(isVertical) ? x : x - dstToMainVertexA, (isVertical) ? y - dstToMainVertexA : y];
                        float heightMainVertexB = heightMap[(isVertical) ? x : x + dstToMainVertexB, (isVertical) ? y + dstToMainVertexB : y];

                        height = heightMainVertexA * (1 - dstPercentFromAToB) + heightMainVertexB * dstPercentFromAToB;
                    }

                    meshData.AddVertex(new Vector3(vertexPosition2D.x, height, vertexPosition2D.y), percent, vertexIndex);

                    bool createTriangle = x < numVertsPerline - 1 && y < numVertsPerline - 1 && (!isEdgeConnectionVertex || (x != 2 && y != 2));

                    //vertex의 맨 오른쪽과 아래쪽은 삼각형을 만들 때 접근하지 않아도 됨.
                    if (createTriangle)
                    {
                        int currentIncrement = (isMainVertex && x != numVertsPerline - 3 && y != numVertsPerline - 3) ? skipIncrement : 1;

                        int a = vertexIndicesMap[x, y];
                        int b = vertexIndicesMap[x + currentIncrement, y];
                        int c = vertexIndicesMap[x, y + currentIncrement];
                        int d = vertexIndicesMap[x + currentIncrement, y + currentIncrement];

                        meshData.AddTriangle(a, d, c);
                        meshData.AddTriangle(d, a, b);
                    }
                }

                
            }
        }

        meshData.ProcessMesh();
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
    Vector3[] _bakedNormals;

    Vector3[] _outOfMeshVertices;
    int[] _outOfMeshTriangles;

    int _triangleIndex;
    int _outOfMeshTriangleIndex;

    bool _useFlatShading;
    public MeshData(int numVertsPerLine,int skipIncrement, bool useFlatShading)
    {
        _useFlatShading = useFlatShading;

        // 정사각형이기 때문에 * 4, 중복 계산는 꼭짓점을 빼기 위해 - 4
        int numMeshEdgeVertices = (numVertsPerLine - 2) * 4 - 4;

        //(폴리 하나에 스킵된 vertex가) * (한 모서리 안에 반복되는 횟수) * (정사각형이니까 * 4)
        int numEdgeConnectionVertices = (skipIncrement - 1) * ((numVertsPerLine - 5) / skipIncrement) * 4; 

        int numMainVerticesPerLine = (numVertsPerLine - 5) / skipIncrement + 1;

        int numMainVertices = numMainVerticesPerLine * numMainVerticesPerLine;

        _vertices = new Vector3[numMeshEdgeVertices + numEdgeConnectionVertices + numMainVertices];
        _uvs = new Vector2[_vertices.Length];


        // (((numVertsPerline - 2) - 1) * 4 - 4) * 2
        int numMeshEdgeTriangles = 8 * (numVertsPerLine - 4); 
        int numMainTriangles = (numMainVerticesPerLine - 1) * (numMainVerticesPerLine - 1) * 2;

        _triangles = new int[(numMeshEdgeTriangles + numMainTriangles) * 3];
        _outOfMeshVertices = new Vector3[numVertsPerLine * 4 - 4];

        // 2 * 3 * (4 * (vertexPerLine - 1) - 4) (= 24 * (vertexPerLine - 2))
        _outOfMeshTriangles = new int[24 * (numVertsPerLine - 2)];

        _triangleIndex = 0;
        _outOfMeshTriangleIndex = 0;

        
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {
        if(vertexIndex < 0)
        {
            _outOfMeshVertices[-vertexIndex - 1] = vertexPosition;
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
            _outOfMeshTriangles[_outOfMeshTriangleIndex] = a;
            _outOfMeshTriangles[_outOfMeshTriangleIndex + 1] = b;
            _outOfMeshTriangles[_outOfMeshTriangleIndex + 2] = c;
            _outOfMeshTriangleIndex += 3;
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

        if(_useFlatShading)
        {
            mesh.RecalculateNormals();
        }
        else
        {
            mesh.normals = _bakedNormals;
        }
        return mesh;
    }
    public void ProcessMesh()
    {
        if(_useFlatShading)
        {
            // 노말을 베이킹 하지 않는 이유는
            // 인접한 청크 간에 부드러운 조명 계산을 위함인데
            // flatShading은 각 메시 고유의 정점으로 normal을 계산하기 때문임.
            FlatShading();
        }
        else
        {
            BakeNormals();
        }
    }
    void BakeNormals()
    {
        _bakedNormals = CalculateNormals();
    }
    void FlatShading()
    {
        Vector3[] flatShadedVertices = new Vector3[_triangles.Length];
        Vector2[] flatShadedUvs = new Vector2[_triangles.Length];
        for (int i = 0; i < _triangles.Length; i++)
        {
            flatShadedVertices[i] = _vertices[_triangles[i]];
            flatShadedUvs[i] = _uvs[_triangles[i]];
            _triangles[i] = i;
        }

        _vertices = flatShadedVertices;
        _uvs = flatShadedUvs;
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
        
        int borderTriangleCount = _outOfMeshTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = _outOfMeshTriangles[normalTriangleIndex];
            int vertexIndexB = _outOfMeshTriangles[normalTriangleIndex + 1];
            int vertexIndexC = _outOfMeshTriangles[normalTriangleIndex + 2];

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
       

        Vector3 pointA = (indexA < 0) ? _outOfMeshVertices[-indexA - 1] : _vertices[indexA];
        Vector3 pointB = (indexB < 0) ? _outOfMeshVertices[-indexB - 1] : _vertices[indexB];
        Vector3 pointC = (indexC < 0) ? _outOfMeshVertices[-indexC - 1] : _vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        // 벡터 외적
        return Vector3.Cross(sideAB, sideAC).normalized;
    }
}