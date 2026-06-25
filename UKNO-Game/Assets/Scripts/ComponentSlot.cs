using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ComponentSlot : MonoBehaviour, IDropHandler
{
    [Header("Настройки слота")]
    public string requiredComponentType; // Какой элемент сюда нужен
    public bool isOccupied = false;

    [Header("Визуальные эффекты")]
    public Color availableColor = Color.green;
    public Color occupiedColor = Color.blue;
    public Color wrongColor = Color.red;

    private DraggableComponent currentComponent;
    private Image slotImage;
    private Color originalColor;

    void Start()
    {
        slotImage = GetComponent<Image>();
        if (slotImage != null)
            originalColor = slotImage.color;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (isOccupied) return;

        // Получаем перетаскиваемый объект
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null) return;

        DraggableComponent component = draggedObject.GetComponent<DraggableComponent>();
        if (component == null || component.isPlaced) return;

        // Проверяем подходит ли элемент
        if (component.componentType == requiredComponentType)
        {
            // Правильный элемент
            component.PlaceOnSlot(this);
            ShowFeedback(availableColor, true);
        }
        else
        {
            // Неправильный элемент - показываем ошибку
            ShowFeedback(wrongColor, false);
        }
    }

    public void Occupy(DraggableComponent component)
    {
        isOccupied = true;
        currentComponent = component;

        if (slotImage != null)
            slotImage.color = occupiedColor;
    }

    public void ClearSlot()
    {
        isOccupied = false;
        currentComponent = null;

        if (slotImage != null)
            slotImage.color = originalColor;
    }

    void ShowFeedback(Color color, bool success)
    {
        if (slotImage == null) return;

        slotImage.color = color;
        Invoke("ResetColor", 0.5f);

        if (success)
        {
            // Можно добавить звук успеха
            Debug.Log($"Элемент {requiredComponentType} установлен!");
        }
        else
        {
            Debug.Log($"Неверный элемент! Сюда нужен {requiredComponentType}");
        }
    }

    void ResetColor()
    {
        if (!isOccupied)
            slotImage.color = originalColor;
        else
            slotImage.color = occupiedColor;
    }
}