using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Área de la grilla (plano XZ)")]
    [SerializeField] private Vector2 gridWorldSize = new Vector2(50, 50);
    [SerializeField] private float nodeRadius = 0.5f;
    [Tooltip("Centro de la grilla. Si está vacío usa la posición de este objeto.")]
    [SerializeField] private Transform gridOrigin;

    [Header("Obstáculos")]
    [SerializeField] private LayerMask obstacleMask;
    [Range(0.1f, 1f)]
    [SerializeField] private float obstacleCheckRadiusFactor = 0.9f;
    [Tooltip("Marca también las celdas pegadas a un obstáculo. Da 'aire' para que el agente no roce las paredes.")]
    [SerializeField] private bool inflateObstacles = true;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool drawOnlyBlocked = true;

    private PathNode[,] grid;
    private float nodeDiameter;
    private int gridSizeX, gridSizeY;

    public int MaxSize => gridSizeX * gridSizeY;
    public Bounds WorldBounds => new Bounds(Origin, new Vector3(gridWorldSize.x, 1f, gridWorldSize.y));
    private Vector3 Origin => gridOrigin != null ? gridOrigin.position : transform.position;
    public float NodeRadius => nodeRadius;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        nodeDiameter = nodeRadius * 2f;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        CreateGrid();
    }

    private void CreateGrid()
    {
        grid = new PathNode[gridSizeX, gridSizeY];
        Vector3 bottomLeft = Origin
            - Vector3.right * gridWorldSize.x / 2f
            - Vector3.forward * gridWorldSize.y / 2f;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = bottomLeft
                    + Vector3.right * (x * nodeDiameter + nodeRadius)
                    + Vector3.forward * (y * nodeDiameter + nodeRadius);

                bool walkable = !Physics.CheckSphere(worldPoint, nodeRadius * obstacleCheckRadiusFactor, obstacleMask);
                grid[x, y] = new PathNode(walkable, worldPoint, x, y);
            }
        }

        if (inflateObstacles) InflateObstacles();
    }

    private void InflateObstacles()
    {
        bool[,] toBlock = new bool[gridSizeX, gridSizeY];

        for (int x = 0; x < gridSizeX; x++)
            for (int y = 0; y < gridSizeY; y++)
            {
                if (!grid[x, y].walkable) continue;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= gridSizeX || ny < 0 || ny >= gridSizeY) continue;
                        if (!grid[nx, ny].walkable) toBlock[x, y] = true;
                    }
            }

        for (int x = 0; x < gridSizeX; x++)
            for (int y = 0; y < gridSizeY; y++)
                if (toBlock[x, y]) grid[x, y].walkable = false;
    }

    public PathNode NodeFromWorldPoint(Vector3 worldPosition)
    {
        Vector3 local = worldPosition - Origin;
        float percentX = Mathf.Clamp01((local.x + gridWorldSize.x / 2f) / gridWorldSize.x);
        float percentY = Mathf.Clamp01((local.z + gridWorldSize.y / 2f) / gridWorldSize.y);

        int x = Mathf.Clamp(Mathf.RoundToInt((gridSizeX - 1) * percentX), 0, gridSizeX - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt((gridSizeY - 1) * percentY), 0, gridSizeY - 1);
        return grid[x, y];
    }

    public bool IsWalkable(Vector3 worldPosition)
    {
        if (grid == null) return true;
        return NodeFromWorldPoint(worldPosition).walkable;
    }
    public Vector3 ClosestWalkablePoint(Vector3 worldPosition)
    {
        if (grid == null) return worldPosition;
        PathNode start = NodeFromWorldPoint(worldPosition);
        if (start.walkable) return worldPosition;

        Queue<PathNode> frontier = new Queue<PathNode>();
        HashSet<PathNode> visited = new HashSet<PathNode>();
        frontier.Enqueue(start);
        visited.Add(start);

        while (frontier.Count > 0)
        {
            PathNode n = frontier.Dequeue();
            if (n.walkable) return n.worldPosition;
            foreach (PathNode nb in GetNeighbours(n))
                if (!visited.Contains(nb)) { visited.Add(nb); frontier.Enqueue(nb); }
        }
        return worldPosition;
    }

    public List<PathNode> BlockCircle(Vector3 center, float radius)
    {
        List<PathNode> changed = new List<PathNode>();
        if (grid == null) return changed;

        PathNode c = NodeFromWorldPoint(center);
        int r = Mathf.CeilToInt(radius / nodeDiameter);
        float radSqr = radius * radius;
        center.y = 0f;

        for (int x = c.gridX - r; x <= c.gridX + r; x++)
            for (int y = c.gridY - r; y <= c.gridY + r; y++)
            {
                if (x < 0 || x >= gridSizeX || y < 0 || y >= gridSizeY) continue;
                PathNode n = grid[x, y];
                if (!n.walkable) continue; 

                Vector3 p = n.worldPosition; p.y = 0f;
                if ((p - center).sqrMagnitude <= radSqr)
                {
                    n.walkable = false;
                    changed.Add(n);
                }
            }
        return changed;
    }

    public void UnblockNodes(List<PathNode> nodes)
    {
        if (nodes == null) return;
        foreach (PathNode n in nodes) n.walkable = true;
    }
    public List<PathNode> GetNeighbours(PathNode node)
    {
        List<PathNode> neighbours = new List<PathNode>();

        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                int cx = node.gridX + x;
                int cy = node.gridY + y;
                if (cx < 0 || cx >= gridSizeX || cy < 0 || cy >= gridSizeY) continue;

                if (x != 0 && y != 0)
                {
                    if (!grid[node.gridX + x, node.gridY].walkable ||
                        !grid[node.gridX, node.gridY + y].walkable)
                        continue;
                }
                neighbours.Add(grid[cx, cy]);
            }

        return neighbours;
    }
    public bool HasClearLine(Vector3 a, Vector3 b)
    {
        a.y = b.y;
        float dist = Vector3.Distance(a, b);
        int steps = Mathf.CeilToInt(dist / Mathf.Max(nodeRadius, 0.01f));
        if (steps <= 0) return IsWalkable(a);

        for (int i = 0; i <= steps; i++)
        {
            Vector3 p = Vector3.Lerp(a, b, i / (float)steps);
            if (!IsWalkable(p)) return false;
        }
        return true;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
        Gizmos.DrawWireCube(Origin, new Vector3(gridWorldSize.x, 0.1f, gridWorldSize.y));

        if (grid == null) return;
        foreach (PathNode n in grid)
        {
            if (n.walkable && drawOnlyBlocked) continue;
            Gizmos.color = n.walkable ? new Color(0f, 1f, 0f, 0.12f) : new Color(1f, 0f, 0f, 0.45f);
            float s = nodeDiameter * 0.9f;
            Gizmos.DrawCube(n.worldPosition, new Vector3(s, 0.05f, s));
        }
    }
}
