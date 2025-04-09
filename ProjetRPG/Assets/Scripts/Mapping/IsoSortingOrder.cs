using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class IsoSortingOrder : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    // Facteur de précision (tu peux ajuster selon ton unité de map)
    [SerializeField] private int sortingPrecision = 100;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateSortingOrder();
    }

    void LateUpdate()
    {
        UpdateSortingOrder();
    }

    void UpdateSortingOrder()
    {
        spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * sortingPrecision);
    }
}
