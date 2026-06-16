using UnityEngine;

public class ToggleClipboard : MonoBehaviour
{
    [Header("Точка осмотра перед глазами")]
    public Transform inspectPosition; // Сюда перетащить InspectPos_Anchor из Камеры

    [Header("Точка скрытого хранения")]
    public Transform hiddenPosition;  // Сюда перетащить HiddenPos_Anchor из Камеры

    [Header("Массив объектов-галочек")]
    public GameObject[] taskCheckmarks; // Сюда перетащите ваши объекты Square по порядку!

    [Header("Настройки скорости")]
    public float moveSpeed = 12f;
    public float rotateSpeed = 12f;

    private bool isPlayerNear = false;
    private bool isPickedUp = false;
    private bool isInspecting = false;
    private Collider objectCollider;

    void Start()
    {
        objectCollider = GetComponent<Collider>();
    }

    void Update()
    {
        // 1. Подбираем контейнер со стола на кнопку E
        if (isPlayerNear && !isPickedUp && Input.GetKeyDown(KeyCode.E))
        {
            PickUp();
        }

        // 2. Если подобрали, плавно перемещаем контейнер по ПКМ
        if (isPickedUp)
        {
            if (Input.GetMouseButtonDown(1))
            {
                isInspecting = !isInspecting;
            }

            // Выбираем целевой якорь в камере
            Transform targetAnchor = isInspecting ? inspectPosition : hiddenPosition;

            // Двигаем и крутим внешний контейнер
            transform.position = Vector3.Lerp(transform.position, targetAnchor.position, Time.deltaTime * moveSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetAnchor.rotation, Time.deltaTime * rotateSpeed);
        }
    }

    void PickUp()
    {
        isPickedUp = true;
        if (objectCollider != null) objectCollider.enabled = false;

        // Находим саму 3D-модель планшетки (первый дочерний объект)
        Transform modelTransform = transform.GetChild(0);

        // Привязываем внешний контейнер к камере игрока
        transform.SetParent(inspectPosition.parent);

        // Сбрасываем координаты контейнера
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // Задаем ваши идеальные локальные координаты для 3D-модели
        if (modelTransform != null)
        {
            modelTransform.localPosition = new Vector3(0.9f, 0.2f, 0.8f);
            modelTransform.localRotation = Quaternion.Euler(90f, 25f, 180f);
        }
    }

    // Эту функцию будут вызывать ваши мини-игры при победе
    public void CompleteTask(int taskIndex)
    {
        if (taskIndex >= 0 && taskIndex < taskCheckmarks.Length)
        {
            taskCheckmarks[taskIndex].SetActive(true); // Включаем нужную галочку
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) isPlayerNear = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) isPlayerNear = false;
    }
}
