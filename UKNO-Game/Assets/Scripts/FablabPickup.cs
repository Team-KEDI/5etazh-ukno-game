using UnityEngine;
using TMPro;

public class FablabPickup : MonoBehaviour
{
    [Header("Íàñòðîéêè ïîäáîðà")]
    public float pickupRange = 3f;
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
        if (r) r.material.color = highlight ? GameColors.Primary : originalItemColor;
    }

    void PickupItem()
    {
        itemsCollected++;

        if (itemsCollected == 1 && exitBlocker != null) exitBlocker.SetActive(true);

        if (counterPanel) counterPanel.SetActive(true);
        if (counterText) counterText.text = "Собрано деталей: " + itemsCollected + "/5";

        PrepareItemForTable(currentTargetItem);

        currentTargetItem = null;

        if (itemsCollected >= itemsRequired)
        {
            questCompleted = true;
            if (counterText) counterText.text = "Все детали собраны!";
        }
    }

    void PrepareItemForTable(GameObject item)
    {
        Renderer r = item.GetComponent<Renderer>();
        if (r) r.material.color = originalItemColor;

        item.transform.position = assemblyTablePoint.position + new Vector3(itemsCollected * 0.4f - 0.8f, 0.1f, 0.4f);
        item.transform.rotation = Quaternion.identity;

        item.tag = "Untagged";

        var clickable = item.GetComponent<ClickableDetailForSlots>();
        if (clickable == null) clickable = item.AddComponent<ClickableDetailForSlots>();

        var manager = Object.FindAnyObjectByType<AssemblySlotsManager>();
        clickable.Initialize(itemsCollected);
    }

    public bool IsQuestCompleted() => questCompleted;
    public int GetCollectedCount() => itemsCollected;
    public void HideCounter() { if (counterPanel) counterPanel.SetActive(false); }
}
