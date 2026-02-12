using UnityEngine;

public class TrafficWorldPartitionBridge : MonoBehaviour
{
    [SerializeField] private Vector2 cellSize = new Vector2(256f, 256f);
    [SerializeField] private int seed = 12345;

    public Vector2Int GetCell(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / Mathf.Max(1f, cellSize.x));
        int z = Mathf.FloorToInt(worldPosition.z / Mathf.Max(1f, cellSize.y));
        return new Vector2Int(x, z);
    }

    public bool IsCellLoaded(Vector2Int cell)
    {
        return true;
    }

    public int DeterministicHash(Vector2Int cell)
    {
        unchecked
        {
            int hash = seed;
            hash = (hash * 397) ^ cell.x;
            hash = (hash * 397) ^ cell.y;
            return hash;
        }
    }
}
