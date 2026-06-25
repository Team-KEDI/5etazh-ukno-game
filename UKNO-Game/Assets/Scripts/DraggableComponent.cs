using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableComponent : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("Настройки")]
    public string componentType; // Тип элемента (например "CPU", "RAM", "GPU")
    public bool isPlaced = false;

    private Vector3 originalPosition;
    private Transform originalParent;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPlaced) return;

        // Делаем элемент полупрозрачным при перетаскивании
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false; // Чтобы можно было бросать на слоты
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isPlaced) return;

        // Перемещаем элемент за мышкой
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isPlaced) return;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Если не разместили - возвращаем на место
        if (!isPlaced)
        {
            ReturnToOriginalPosition();
        }
    }

    public void PlaceOnSlot(ComponentSlot slot)
    {
        if (isPlaced) return;

        // Размещаем элемент на слоте
        transform.SetParent(slot.transform);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localScale = Vector3.one;

        isPlaced = true;
        slot.Occupy(this);
        canvasGroup.blocksRaycasts = false; // Нельзя двигать после размещения

        // Отмечаем задание
        FindObjectOfType<PuzzleBoard>().CheckCompletion();
    }

    public void ReturnToOriginalPosition()
    {
        if (isPlaced) return;

        transform.SetParent(originalParent);
        rectTransform.anchoredPosition = originalPosition;
    }
}