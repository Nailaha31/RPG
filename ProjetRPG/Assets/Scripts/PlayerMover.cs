using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Netcode;
using UnityEngine.EventSystems;

public class PlayerMoverV2 : NetworkBehaviour
{
    public Tilemap tilemap;
    public Tilemap tilemapHighlight;
    public Tile highlightTile;
    public Pathfinding pathfinding;
    public float moveSpeed = 6f;

    private Vector3Int currentCell;
    private Coroutine currentPathCoroutine = null;
    private bool isInputBlocked = false;
    private Vector3Int lastTargetCell;
    private SpriteRenderer spriteRenderer;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log("🧍 Le joueur a bien spawn sur le réseau !");
    }

    void Start()
    {
        StartCoroutine(InitWhenReady());
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    IEnumerator InitWhenReady()
    {
        float timer = 0f;
        float timeout = 2f;

        while (tilemap == null && timer < timeout)
        {
            GameObject sol = GameObject.Find("Tilemap_Sol");
            if (sol != null)
                tilemap = sol.GetComponent<Tilemap>();

            timer += Time.deltaTime;
            yield return null;
        }

        if (highlightTile == null)
        {
            highlightTile = Resources.Load<Tile>("Tile_Highlight");
            if (highlightTile == null)
                Debug.LogWarning("⚠️ highlightTile introuvable dans Resources !");
            else
                Debug.Log("✅ highlightTile chargé dynamiquement !");
        }

        GameObject highlight = GameObject.Find("Tilemap_Highlight");
        if (highlight != null)
            tilemapHighlight = highlight.GetComponent<Tilemap>();

        if (pathfinding == null)
            pathfinding = Object.FindFirstObjectByType<Pathfinding>();

        if (tilemap == null)
        {
            Debug.LogError("Tilemap_Sol non trouvée après le chargement !");
            yield break;
        }

        SnapToGrid();
    }

    void Update()
    {
        if (!IsOwner) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0) && !isInputBlocked)
        {
            StartCoroutine(HandleClickCooldown(0.2f));

            Vector3Int targetCell = GetClickedCell();
            if (targetCell == currentCell)
                return;

            RequestMoveServerRpc(targetCell);
        }
    }

    [ServerRpc]
    void RequestMoveServerRpc(Vector3Int targetCell)
    {
        List<Node> path = pathfinding.FindPath(currentCell, targetCell);
        if (path != null && path.Count > 0)
        {
            ApplyMoveClientRpc(targetCell);
        }
        else
        {
            Debug.Log("❌ Aucun chemin trouvé");
        }
    }

    [ClientRpc]
    void ApplyMoveClientRpc(Vector3Int targetCell)
    {
        List<Node> path = pathfinding.FindPath(currentCell, targetCell);
        if (path != null && path.Count > 0)
        {
            if (currentPathCoroutine != null)
                StopCoroutine(currentPathCoroutine);

            currentPathCoroutine = StartCoroutine(FollowPath(path, targetCell));

            if (IsOwner)
            {
                UpdateHighlightIfOwner(targetCell);
                lastTargetCell = targetCell;
            }
        }
    }

    IEnumerator FollowPath(List<Node> path, Vector3Int targetCell)
    {
        foreach (Node node in path)
        {
            Vector3 targetPos = tilemap.GetCellCenterWorld(node.gridPos);
            while ((transform.position - targetPos).sqrMagnitude > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                UpdateSortingOrder();
                yield return null;
            }

            transform.position = targetPos;
            currentCell = node.gridPos;
            UpdateSortingOrder();
            yield return null;
        }

        if (currentCell == targetCell)
        {
            ClearHighlightIfOwner();
        }
    }

    IEnumerator HandleClickCooldown(float delay)
    {
        isInputBlocked = true;
        yield return new WaitForSeconds(delay);
        isInputBlocked = false;
    }

    void UpdateHighlightIfOwner(Vector3Int cell)
    {
        if (!IsOwner || tilemapHighlight == null || highlightTile == null)
            return;

        tilemapHighlight.ClearAllTiles();
        tilemapHighlight.SetTile(cell, highlightTile);
    }

    void ClearHighlightIfOwner()
    {
        if (!IsOwner || tilemapHighlight == null)
            return;

        tilemapHighlight.ClearAllTiles();
    }

    Vector3Int GetClickedCell()
    {
        Vector3 mouseScreen = Input.mousePosition;
        mouseScreen.z = 10f;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mouseScreen);
        Vector3Int cell = tilemap.WorldToCell(worldPos);
        cell.z = 0;
        return cell;
    }

    void SnapToGrid()
    {
        currentCell = tilemap.WorldToCell(transform.position);
        currentCell.z = 0;
        transform.position = tilemap.GetCellCenterWorld(currentCell);
    }

    void UpdateSortingOrder()
    {
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = -(int)(transform.position.y * 100);
    }
}
