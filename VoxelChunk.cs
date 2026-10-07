using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class VoxelChunk : MonoBehaviour
{
    public const int ChunkSize = 16;
    public float isoLevel = 0.0f;

    private float[,,] mapData = new float[ChunkSize + 1, ChunkSize + 1, ChunkSize + 1];
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
    }

    public void InitializeChunk(Vector3Int chunkOffset)
    {
        GenerateDensityMap();
        GenerateMesh();
    }

    // Preenche a matriz 3D com dados de densidade cruzando o IsoLevel (0.0f)
    private void GenerateDensityMap()
    {
        for (int x = 0; x <= ChunkSize; x++)
        {
            for (int y = 0; y <= ChunkSize; y++)
            {
                for (int z = 0; z <= ChunkSize; z++)
                {
                    // Posição global no mundo considerando o deslocamento do Chunk
                    Vector3 worldPos = transform.position + new Vector3(x, y, z);

                    // Ponto de altura base (superfície ao redor de Y = 8)
                    // Valores positivos = Terra (abaixo do nível da superfície)
                    // Valores negativos = Ar (acima do nível da superfície)
                    float heightDensity = 8.0f - worldPos.y;

                    // Onde o valor é maior que 0 gera terra, menor gera ar
                    float noise = Mathf.Sin(worldPos.x * 0.2f) * Mathf.Cos(worldPos.z * 0.2f) * 3.0f;

                    mapData[x, y, z] = heightDensity + noise;
                }
            }
        }
    }

    // Executa o algoritmo do Marching Cubes em cada Voxel
    private void GenerateMesh()
    {
        vertices.Clear();
        triangles.Clear();

        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    ProcessVoxel(new Vector3Int(x, y, z));
                }
            }
        }

        BuildMesh();
    }

    private void ProcessVoxel(Vector3Int pos)
    {
        float[] cubeValues = new float[8];
        Vector3[] corners = new Vector3[8];

        for (int i = 0; i < 8; i++)
        {
            Vector3Int cornerPos = pos + GetCornerOffset(i);
            cubeValues[i] = mapData[cornerPos.x, cornerPos.y, cornerPos.z];
            corners[i] = cornerPos;
        }

        // Calcula o índice de configuração de cantos (0 a 255)
        int cubeIndex = 0;
        for (int i = 0; i < 8; i++)
        {
            if (cubeValues[i] < isoLevel) cubeIndex |= (1 << i);
        }

        // Checagem de segurança para evitar ultrapassar os limites da tabela carregada
        if (cubeIndex >= MarchingCubesTables.TriTable.GetLength(0)) return;

        // Constrói os triângulos com base na tabela de pesquisa do Marching Cubes
        for (int i = 0; MarchingCubesTables.TriTable[cubeIndex, i] != -1; i += 3)
        {
            int a0 = MarchingCubesTables.EdgeNodes[MarchingCubesTables.TriTable[cubeIndex, i], 0];
            int b0 = MarchingCubesTables.EdgeNodes[MarchingCubesTables.TriTable[cubeIndex, i], 1];

            int a1 = MarchingCubesTables.EdgeNodes[MarchingCubesTables.TriTable[cubeIndex, i + 1], 0];
            int b1 = MarchingCubesTables.EdgeNodes[MarchingCubesTables.TriTable[cubeIndex, i + 1], 1];

            int a2 = MarchingCubesTables.EdgeNodes[MarchingCubesTables.TriTable[cubeIndex, i + 2], 0];
            int b2 = MarchingCubesTables.EdgeNodes[MarchingCubesTables.TriTable[cubeIndex, i + 2], 1];

            Vector3 v1 = VertexInterpolate(corners[a0], corners[b0], cubeValues[a0], cubeValues[b0]);
            Vector3 v2 = VertexInterpolate(corners[a1], corners[b1], cubeValues[a1], cubeValues[b1]);
            Vector3 v3 = VertexInterpolate(corners[a2], corners[b2], cubeValues[a2], cubeValues[b2]);

            int startIndex = vertices.Count;
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);

            triangles.Add(startIndex);
            triangles.Add(startIndex + 1);
            triangles.Add(startIndex + 2);
        }
    }

    // Interpolação linear dos vértices entre as arestas
    private Vector3 VertexInterpolate(Vector3 p1, Vector3 p2, float val1, float val2)
    {
        if (Mathf.Abs(isoLevel - val1) < 0.00001f) return p1;
        if (Mathf.Abs(isoLevel - val2) < 0.00001f) return p2;
        if (Mathf.Abs(val1 - val2) < 0.00001f) return p1;

        float mu = (isoLevel - val1) / (val2 - val1);
        return p1 + mu * (p2 - p1);
    }

    private Vector3Int GetCornerOffset(int index)
    {
        switch (index)
        {
            case 0: return new Vector3Int(0, 0, 0);
            case 1: return new Vector3Int(1, 0, 0);
            case 2: return new Vector3Int(1, 0, 1);
            case 3: return new Vector3Int(0, 0, 1);
            case 4: return new Vector3Int(0, 1, 0);
            case 5: return new Vector3Int(1, 1, 0);
            case 6: return new Vector3Int(1, 1, 1);
            case 7: return new Vector3Int(0, 1, 1);
            default: return Vector3Int.zero;
        }
    }

    // Atualiza a malha e os seletores de colisão da Unity
    private void BuildMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;

        // Atualização da malha de colisão
        if (vertices.Count > 0)
        {
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
        }
    }

    // Método para escavar ou adicionar terreno dinamicamente
    public void ModifyTerrain(Vector3Int localPos, float amount)
    {
        if (localPos.x >= 0 && localPos.x <= ChunkSize &&
            localPos.y >= 0 && localPos.y <= ChunkSize &&
            localPos.z >= 0 && localPos.z <= ChunkSize)
        {
            mapData[localPos.x, localPos.y, localPos.z] += amount;
            GenerateMesh();
        }
    }
}