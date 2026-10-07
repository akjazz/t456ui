using UnityEngine;

public class VoxelWorld : MonoBehaviour
{
    public GameObject chunkPrefab;
    public Vector3Int worldDimensions = new Vector3Int(3, 2, 3);

    private void Start()
    {
        GenerateWorld();
    }

    private void GenerateWorld()
    {
        for (int x = 0; x < worldDimensions.x; x++)
        {
            for (int y = 0; y < worldDimensions.y; y++)
            {
                for (int z = 0; z < worldDimensions.z; z++)
                {
                    Vector3Int chunkCoord = new Vector3Int(x, y, z);
                    Vector3 worldPos = new Vector3(
                        x * VoxelChunk.ChunkSize, 
                        y * VoxelChunk.ChunkSize, 
                        z * VoxelChunk.ChunkSize
                    );

                    GameObject chunkObj = Instantiate(chunkPrefab, worldPos, Quaternion.identity, transform);
                    chunkObj.name = $"Chunk_{x}_{y}_{z}";

                    VoxelChunk chunkScript = chunkObj.GetComponent<VoxelChunk>();
                    chunkScript.InitializeChunk(chunkCoord);
                }
            }
        }
    }
}