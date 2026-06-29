using UnityEngine;

public class PathNode
{
    public bool walkable;
    public Vector3 worldPosition;
    public int gridX;
    public int gridY;

    public int gCost; 
    public int hCost; 
    public PathNode parent;

    public int FCost => gCost + hCost;

    public PathNode(bool walkable, Vector3 worldPosition, int gridX, int gridY)
    {
        this.walkable = walkable;
        this.worldPosition = worldPosition;
        this.gridX = gridX;
        this.gridY = gridY;
    }
}
