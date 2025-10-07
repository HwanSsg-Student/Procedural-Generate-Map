using UnityEngine;

public static class MeshGenerator
{
    // ���� ���� 0 ~ 1�̱� ������ ���̰��� ���� �¼� ���� �ʿ�
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail)
    {
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);

        // �Žø� �����ϴ� ���� ������
        // �ϰ����� ���� �޽� �������� ¦���� ������
        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;

        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * meshSimplificationIncrement;
        int meshSizeUnsimplified = borderedSize - 2;

        // �Ž��� �߽��� (0, 0, 0) ��ǥ�� �����ϱ� ���ؼ�
        // ���� �̵����� ��� �ؾ���
        // ����Ƽ�� XZ ��ǥ�踦 �����
        // �Ž��� �߽��� (0, 0, 0) ��ǥ�� �����ϸ� ��ǥ�� +- ��Ī�̶� ����� ������
        // API�� �� ����
        // Ÿ��/ûũ ��ġ�� ������
        float topLeftX = (meshSizeUnsimplified - 1) / -2f;
        float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

        // �ε����� 0���� �����ϱ� ������ (meshSize - 1)
        // ; (width - 1) / ������ + 1
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

                //vertex�� �� �����ʰ� �Ʒ����� �ﰢ���� ���� �� �������� �ʾƵ� ��.
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
/// Mesh�� ���� ������ ��� �ִ� Class. <br></br>
/// Mesh�� ������ �� ����� ���� ������ �ذ��ϱ� ���ؼ� Bordered Vertex�� ����ؾ��Ѵ�. <br></br>
/// �� Bordered Vertex�� ���� �޽ÿ��� ���ܵ����� ������ �ùٸ��� ����ϱ� ���� ���δ�. <br></br>
/// Bordered Vertex�� -1���� �����Ͽ� ������ ǥ���ȴ�. <br></br>
/// �޽ð� ������ �� ������ ���Ե� �޽ô� ���� �޽ÿ��� ���ܵȴ�.
/// </summary>
public class MeshData
{
    // Unity Mesh API�� 1���� �迭�� �䱸�ϱ� ������
    // Vertex�� 1���� �迭�� �����ؾ���
    // Vertex�� ũ��� Width * Height
    // Triangle�� ũ��� 2 * 3 * (Width - 1) * (Height - 1)

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
    /// ����� �ﰢ�� ǥ�鿡 ������ ���� ���ͷ�, �� �ﰢ���� ������ ����ϴ� �� ���˴ϴ�. <br></br>
    /// Unity���� �޽ø� ������ �� ����(vertex)���� ����� �����ؾ� �ϸ�, <br></br>
    /// �̴� �ش� ������ ����� ��� �ﰢ���� ����� ����Ͽ� ���˴ϴ�.
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
        // ���� ����
        return Vector3.Cross(sideAB, sideAC).normalized;
    }
}