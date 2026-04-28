using UnityEngine;
using UnityEngine.UI;

public class ItemPickup : MonoBehaviour
{
    [Header("Настройки")]
    public float pickupRange = 2f;
    public float spawnDistance = 1.5f; // Расстояние появления предмета
    public KeyCode actionKey = KeyCode.E;

    [Header("Счетчик предметов")]
    public int itemsCollected = 0;

    [Header("Настройки предмета")]
    public GameObject itemPrefab; // Сюда перетащите префаб куба


    [Header("Отладка")]
    public bool showDebugRay = true;

    private Camera playerCamera;
    private GameObject currentTargetItem;

    void Start()
    {
        playerCamera = GetComponent<Camera>();
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }
    }

    void Update()
    {
        RaycastForItem();

        if (Input.GetKeyDown(actionKey))
        {
            // Если смотрим на предмет - подбираем
            if (currentTargetItem != null && currentTargetItem.CompareTag("Consumable"))
            {
                PickupItem();
            }
            // Если не смотрим на предмет И есть предметы в инвентаре - создаем новый
            else if (itemsCollected > 0)
            {
                SpawnItem();
            }
        }
    }

    void RaycastForItem()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * pickupRange, Color.green);
        }

        if (Physics.Raycast(ray, out hit, pickupRange))
        {
            if (hit.collider.CompareTag("Consumable"))
            {
                // Если нашли новый предмет
                if (currentTargetItem != hit.collider.gameObject)
                {
                    // Убираем подсветку со старого
                    if (currentTargetItem != null)
                    {
                        HighlightItem(currentTargetItem, false);
                    }
                    currentTargetItem = hit.collider.gameObject;
                    HighlightItem(currentTargetItem, true);
                }
            }
            else
            {
                if (currentTargetItem != null)
                {
                    HighlightItem(currentTargetItem, false);
                    currentTargetItem = null;
                }
            }
        }
        else
        {
            if (currentTargetItem != null)
            {
                HighlightItem(currentTargetItem, false);
                currentTargetItem = null;
            }
        }
    }

    void PickupItem()
    {
        if (currentTargetItem != null)
        {
            // Увеличиваем счетчик
            itemsCollected++;
            Debug.Log($"Предмет подобран! Всего: {itemsCollected}");

            // Уничтожаем предмет в мире
            Destroy(currentTargetItem);
            currentTargetItem = null;

        }
    }

    void SpawnItem()
    {
        if (itemPrefab == null)
        {
            Debug.LogError("Нет префаба предмета! Перетащите куб в поле Item Prefab");
            return;
        }

        if (itemsCollected > 0)
        {
            // Вычитаем из счетчика
            itemsCollected--;
            Debug.Log($"Предмет создан! Осталось: {itemsCollected}");

            // Рассчитываем позицию по направлению взгляда
            Vector3 spawnPosition = playerCamera.transform.position +
                                    playerCamera.transform.forward * spawnDistance;

            // Создаем новый предмет
            GameObject newItem = Instantiate(itemPrefab, spawnPosition, Quaternion.identity);

            // Убеждаемся, что у нового предмета правильный тег
            newItem.tag = "Consumable";

            // Добавляем компонент Rigidbody для физики (опционально)
            Rigidbody rb = newItem.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = newItem.AddComponent<Rigidbody>();
            }
            // Небольшой случайный импульс для красоты
            rb.AddForce(playerCamera.transform.forward * 2f + Vector3.up * 1f, ForceMode.Impulse);
        }
    }

    void HighlightItem(GameObject item, bool highlight)
    {
        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (highlight)
            {
                renderer.material.color = Color.yellow;
            }
            else
            {
                renderer.material.color = Color.white;
            }
        }
    }

}