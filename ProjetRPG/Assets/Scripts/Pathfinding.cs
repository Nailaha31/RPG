using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


public class Node
{
    public Vector3Int gridPos; // Position dans la grille
    public bool walkable;      // Accessible ou non
    public int gCost;          // Coût du départ jusqu'ici
    public int hCost;          // Coût estimé jusqu'à la cible
    public Node parent;        // Pour retracer le chemin

    public int fCost
    {
        get { return gCost + hCost; }
    }

    public Node(Vector3Int pos, bool walkable)
    {
        this.gridPos = pos;
        this.walkable = walkable;
    }
}

public class Pathfinding : MonoBehaviour
{
    public Tilemap tilemap;          // La Tilemap de sol
    public Tilemap tilemapObstacles; // La Tilemap des obstacles

    private Dictionary<Vector3Int, Node> grid = new Dictionary<Vector3Int, Node>();

    // Crée une grille pour une zone définie, par exemple pour toutes les cellules affichées sur la Tilemap
    public void CreateGrid()
    {
        grid.Clear();
        // Obtenir les bornes de la Tilemap (ici, on part du principe que toutes les cellules sont utiles)
        BoundsInt bounds = tilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                // Une case est walkable si elle n'a pas de tile d'obstacle dans la Tilemap des obstacles
                bool walkable = !tilemapObstacles.HasTile(cell);
                Node node = new Node(cell, walkable);
                grid[cell] = node;
            }
        }
    }

    public List<Node> FindPath(Vector3Int startPos, Vector3Int targetPos)
    {
        CreateGrid();

        // Vérifie si la cible est accessible
        if (!grid.ContainsKey(targetPos) || !grid[targetPos].walkable)
            return null;

        Node startNode = grid[startPos];
        Node targetNode = grid[targetPos];

        List<Node> openList = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        openList.Add(startNode);
        startNode.gCost = 0;
        startNode.hCost = GetDistance(startNode.gridPos, targetNode.gridPos);

        while (openList.Count > 0)
        {
            Node currentNode = openList[0];
            // Sélectionne le nœud avec le plus faible fCost
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].fCost < currentNode.fCost ||
                   (openList[i].fCost == currentNode.fCost && openList[i].hCost < currentNode.hCost))
                {
                    currentNode = openList[i];
                }
            }

            openList.Remove(currentNode);
            closedSet.Add(currentNode);

            // Si on a atteint la cible, on reconstitue le chemin
            if (currentNode.gridPos == targetNode.gridPos)
            {
                return RetracePath(startNode, targetNode);
            }

            foreach (Node neighbor in GetNeighbors(currentNode))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor))
                    continue;

                int tentativeGCost = currentNode.gCost + GetDistance(currentNode.gridPos, neighbor.gridPos);
                if (tentativeGCost < neighbor.gCost || !openList.Contains(neighbor))
                {
                    neighbor.gCost = tentativeGCost;
                    neighbor.hCost = GetDistance(neighbor.gridPos, targetNode.gridPos);
                    neighbor.parent = currentNode;

                    if (!openList.Contains(neighbor))
                        openList.Add(neighbor);
                }
            }
        }

        // Pas de chemin trouvé
        return null;
    }

    List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse();
        return path;
    }

    int GetDistance(Vector3Int a, Vector3Int b)
{
    int dx = Mathf.Abs(a.x - b.x);
    int dy = Mathf.Abs(a.y - b.y);

    if (dx > dy)
        return 14 * dy + 10 * (dx - dy);
    else
        return 14 * dx + 10 * (dy - dx);
}

    List<Node> GetNeighbors(Node node)
{
    List<Node> neighbors = new List<Node>();
    Vector3Int[] directions = new Vector3Int[]
    {
        new Vector3Int(1, 0, 0),     // droite
        new Vector3Int(-1, 0, 0),    // gauche
        new Vector3Int(0, 1, 0),     // haut
        new Vector3Int(0, -1, 0),    // bas
        new Vector3Int(1, 1, 0),     // haut droite
        new Vector3Int(-1, 1, 0),    // haut gauche
        new Vector3Int(1, -1, 0),    // bas droite
        new Vector3Int(-1, -1, 0)    // bas gauche
    };

    foreach (Vector3Int dir in directions)
    {
        Vector3Int neighborPos = node.gridPos + dir;

        if (!grid.ContainsKey(neighborPos))
            continue;

        // Si c’est une direction diagonale
        if (Mathf.Abs(dir.x) == 1 && Mathf.Abs(dir.y) == 1)
        {
            Vector3Int check1 = new Vector3Int(node.gridPos.x + dir.x, node.gridPos.y, 0);
            Vector3Int check2 = new Vector3Int(node.gridPos.x, node.gridPos.y + dir.y, 0);

            // Vérifie que les deux cellules adjacentes sont walkable
            if (grid.ContainsKey(check1) && grid.ContainsKey(check2) &&
                grid[check1].walkable && grid[check2].walkable)
            {
                neighbors.Add(grid[neighborPos]);
            }
        }
        else
        {
            neighbors.Add(grid[neighborPos]);
        }
    }

    return neighbors;
}

}
