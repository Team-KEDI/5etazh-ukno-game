using UnityEngine;
using TMPro;

public class FablabPickup : MonoBehaviour
{
    [Header("Настройки подбора")]
    public float pickupRange = 3f; // Немного увеличил для удобства
    public KeyCode actionKey = KeyCode.E;
    public int itemsCollected = 0;
    public int itemsRequired = 5;
    public Transform assemblyTablePoint;
    public GameObject counterPanel;
    public TMP_Text counterText;
    public GameObject exitBlocker;

    private Camera playerCamera;
    private GameObject currentTargetItem;
    private Color originalItemColor;
    private bool questCompleted = false;

    void Start()
    {
        playerCamera = GetComponent<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;
    }

    void Update()
    {
        if (questCompleted) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange))
        {
            if (hit.collider.CompareTag("Consumable"))
            {
                if (currentTargetItem != hit.collider.gameObject)
                {
                    if (currentTargetItem != null) HighlightItem(currentTargetItem, false);
                    currentTargetItem = hit.collider.gameObject;

                    Renderer r = currentTargetItem.GetComponent<Renderer>();
                    if (r) originalItemColor = r.material.color;

                    HighlightItem(currentTargetItem, true);
                }
            }
            else if (currentTargetItem != null) { HighlightItem(currentTargetItem, false); currentTargetItem = null; }
        }
        else if (currentTargetItem != null) { HighlightItem(currentTargetItem, false); currentTargetItem = null; }

        if (Input.GetKeyDown(actionKey) && currentTargetItem != null) PickupItem();
    }

    void HighlightItem(GameObject item, bool highlight)
    {
        Renderer r = item.GetComponent<Renderer>();
        if (r) r.material.color = highlight ? Color.yellow : originalItemColor;
    }

    void PickupItem()
    {
        itemsCollected++;

        // Активируем стену при первой детали
        if (itemsCollected == 1 && exitBlocker != null) exitBlocker.SetActive(true);

        if (counterPanel) counterPanel.SetActive(true);
        if (counterText) counterText.text = "Детали: " + itemsCollected + "/5";

        // ПЕРЕМЕЩАЕМ ОБЪЕКТ НА СТОЛ
        PrepareItemForTable(currentTargetItem);

        currentTargetItem = null;

        if (itemsCollected >= itemsRequired)
        {
            questCompleted = true;
            if (counterText) counterText.text = "Иди к столу сборки";
        }
    }

    void PrepareItemForTable(GameObject item)
    {
        // 1. Убираем подсветку (желтый цвет) перед перемещением
        Renderer r = item.GetComponent<Renderer>();
        if (r) r.material.color = originalItemColor;

        // 2. Перемещаем на стол в ряд
        item.transform.position = assemblyTablePoint.position + new Vector3(itemsCollected * 0.4f - 0.8f, 0.1f, 0.4f);
        item.transform.rotation = Quaternion.identity;

        // 3. Меняем тег, чтобы его больше нельзя было «подобрать» через Raycast игрока
        item.tag = "Untagged";

        // 4. Добавляем скрипт клика для сборки и инициализируем ID
        var clickable = item.GetComponent<ClickableDetailForSlots>();
        if (clickable == null) clickable = item.AddComponent<ClickableDetailForSlots>();

        var manager = Object.FindAnyObjectByType<AssemblySlotsManager>();
        // ID детали будет равен порядку сбора (1, 2, 3, 4, 5)
        clickable.Initialize(itemsCollected);
    }

    public bool IsQuestCompleted() => questCompleted;
    public int GetCollectedCount() => itemsCollected;
    public void HideCounter() { if (counterPanel) counterPanel.SetActive(false); }
}
