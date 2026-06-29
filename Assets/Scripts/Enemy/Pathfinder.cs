using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    public static Pathfinder Instance { get; private set; }

    [SerializeField] private GridManager grid;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (grid == null) grid = GridManager.Instance;
    }

    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        if (grid == null) grid = GridManager.Instance;
        if (grid == null) return null;

        PathNode startNode = grid.NodeFromWorldPoint(startPos);
        PathNode targetNode = grid.NodeFromWorldPoint(targetPos);

        if (!startNode.walkable) startNode = ClosestWalkable(startNode);
        if (startNode == null) return null;
        if (!targetNode.walkable) targetNode = ClosestWalkable(targetNode);
        if (targetNode == null) return null;


        List<PathNode> openSet = new List<PathNode>();
        HashSet<PathNode> closedSet = new HashSet<PathNode>();

        startNode.gCost = 0;
        startNode.hCost = GetDistance(startNode, targetNode);
        startNode.parent = null;
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            PathNode current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < current.FCost ||
                    (openSet[i].FCost == current.FCost && openSet[i].hCost < current.hCost))
                    current = openSet[i];
            }

            openSet.Remove(current);
            closedSet.Add(current);

            if (current == targetNode)
                return RetracePath(startNode, targetNode);

            foreach (PathNode neighbour in grid.GetNeighbours(current))
            {
                if (!neighbour.walkable || closedSet.Contains(neighbour)) continue;

                int newCost = current.gCost + GetDistance(current, neighbour);
                bool inOpen = openSet.Contains(neighbour);

                if (newCost < neighbour.gCost || !inOpen)
                {
                    neighbour.gCost = newCost;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = current;
                    if (!inOpen) openSet.Add(neighbour);
                }
            }
        }

        return null; 
    }

    private PathNode ClosestWalkable(PathNode node)
    {
        Queue<PathNode> frontier = new Queue<PathNode>();
        HashSet<PathNode> visited = new HashSet<PathNode>();
        frontier.Enqueue(node);
        visited.Add(node);

        while (frontier.Count > 0)
        {
            PathNode n = frontier.Dequeue();
            if (n.walkable) return n;
            foreach (PathNode nb in grid.GetNeighbours(n))
                if (!visited.Contains(nb)) { visited.Add(nb); frontier.Enqueue(nb); }
        }
        return null;
    }

    private List<Vector3> RetracePath(PathNode startNode, PathNode endNode)
    {
        List<PathNode> path = new List<PathNode>();
        PathNode current = endNode;
        while (current != startNode) { path.Add(current); current = current.parent; }
        path.Reverse();
        return SimplifyPath(path);
    }

    private List<Vector3> SimplifyPath(List<PathNode> path)
    {
        List<Vector3> pts = new List<Vector3>();
        if (path.Count == 0) return pts;

        pts.Add(path[0].worldPosition);
        int anchor = 0;

        for (int i = 2; i < path.Count; i++)
        {
            if (!grid.HasClearLine(path[anchor].worldPosition, path[i].worldPosition))
            {
                pts.Add(path[i - 1].worldPosition);
                anchor = i - 1;
            }
        }

        pts.Add(path[path.Count - 1].worldPosition);
        return pts;
    }

    private int GetDistance(PathNode a, PathNode b)
    {
        int dstX = Mathf.Abs(a.gridX - b.gridX);
        int dstY = Mathf.Abs(a.gridY - b.gridY);
        return dstX > dstY ? 14 * dstY + 10 * (dstX - dstY)
                           : 14 * dstX + 10 * (dstY - dstX);
    }
}
