using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class ComprasionInteractions : MonoBehaviour
{
    [Header("Настройки камеры")]
    public Camera playerCamera;
    public Transform puzzleCameraPosition;
    public Transform puzzleCameraLookAt;
    public float cameraMoveSpeed = 5f;

    [Header("Объекты и точки")]
    public GameObject[] draggableObjects;
    public Transform[] targetSpots;

    [Header("Цвета столбцов")]
    public Color[] colorOptions;

    [Header("UI")]
    public TextMeshProUGUI counterText;
    public TextMeshProUGUI successText;
    public GameObject hint;

    [Header("Звуки")]
    public AudioClip swapSound;
    public AudioClip completeSound;

    private AudioSource audioSource;
    private bool isPlayerNear = false;
    private bool isPuzzleActive = false;
    private bool isCompleted = false;
    private GameObject player;
    private CharacterController savedCharController;
    private MonoBehaviour savedMovementScript;
    public PuzzleHolder puzzleHolder; // Ссылка на менеджер пазлов

    private GameObject[] spotOccupants;
    private int[] objectSpotIndex;
    private int correctCount;
    private Color[] columnColors = new Color[5];
    private int[] blockTargetColumn = new int[15];
    private int[] blockTargetRow = new int[15];

    private int selectedBlockIndex = -1;
    private Vector3 selectedBlockOriginalPosition;
    private Coroutine currentMoveCoroutine;

    // Плавное движение камеры
    private bool isCameraMovingToPuzzle = false;
    private bool isCameraMovingBack = false;
    private Vector3 cameraTargetPos;
    private Quaternion cameraTargetRot;
    private Vector3 originalCameraPos;
    private Quaternion originalCameraRot;

    // Для отключения скриптов управления мышью
    private MonoBehaviour[] mouseControlScripts;
    private bool[] mouseControlScriptsEnabledState;

    void Start()
    {
        PlayerPrefs.DeleteKey("PuzzleCompleted");

        if (hint != null) hint.SetActive(false);
        if (counterText != null) counterText.gameObject.SetActive(false);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Полная проверка на наличие объектов в инспекторе
        if (targetSpots == null || targetSpots.Length == 0 || draggableObjects == null || draggableObjects.Length == 0)
        {
            Debug.LogError($"[{name}] КРИТИЧЕСКАЯ ОШИБКА: Забыли перетащить баночки или точки в Инспектор стенда!");
            return;
        }

        spotOccupants = new GameObject[targetSpots.Length];
        objectSpotIndex = new int[draggableObjects.Length];
        for (int i = 0; i < objectSpotIndex.Length; i++) objectSpotIndex[i] = -1;

        // Автоматически подстраиваем размер служебных массивов под 15 баночек
        blockTargetColumn = new int[draggableObjects.Length];
        blockTargetRow = new int[draggableObjects.Length];

        // Жестко фиксируем правильную сетку 3 ряда по 5 баночек
        for (int i = 0; i < draggableObjects.Length; i++)
        {
            blockTargetColumn[i] = i % 5;
            blockTargetRow[i] = i / 5;
        }

        if (PlayerPrefs.GetInt("PuzzleCompleted", 0) == 1)
        {
            isCompleted = true;
            if (counterText != null) counterText.text = "Пазл решён!";
        }
        else
        {
            // ИСПРАВЛЕНО: Запускаем только чистый опрос позиций банок без заигрываний с цветом материалов!
            GenerateRandomConfiguration();
        }

        if (successText != null) successText.gameObject.SetActive(false);
        Debug.Log($"[{name}] Скрипт успешно перезапущен без генерации цветов. Готов к работе.");
    }

    void GenerateRandomConfiguration()
    {
        // Очищаем массив занятых точек перед проверкой
        System.Array.Clear(spotOccupants, 0, spotOccupants.Length);

        for (int i = 0; i < draggableObjects.Length; i++)
        {
            // Если вдруг в массиве есть пустой слот (Missing/None), просто пропускаем его, чтобы игра не крашилась
            if (draggableObjects[i] == null) continue;

            float closestDistance = Mathf.Infinity;
            int closestSpotIndex = -1;

            // Находим, к какой именно точке на полке сейчас ближе всего стоит эта банка на сцене
            for (int j = 0; j < targetSpots.Length; j++)
            {
                if (targetSpots[j] == null) continue;
                float distance = Vector3.Distance(draggableObjects[i].transform.position, targetSpots[j].position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestSpotIndex = j;
                }
            }

            // Привязываем банку к этой точке
            if (closestSpotIndex != -1)
            {
                spotOccupants[closestSpotIndex] = draggableObjects[i];
                objectSpotIndex[i] = closestSpotIndex;

                // Фиксируем банку ровно на полке (повороты НЕ трогаем, чтобы банки не падали)
                draggableObjects[i].transform.position = targetSpots[closestSpotIndex].position;
            }
        }

        UpdateCorrectCount();
        UpdateCounterUI();
    }

    void UpdateCorrectCount()
    {
        correctCount = 0;

        if (draggableObjects == null || objectSpotIndex == null || blockTargetColumn == null || blockTargetRow == null)
            return;

        for (int i = 0; i < draggableObjects.Length; i++)
        {
            if (i >= objectSpotIndex.Length || i >= blockTargetColumn.Length || i >= blockTargetRow.Length)
                continue;

            int spotIdx = objectSpotIndex[i];
            if (spotIdx == -1) continue;

            // Рассчитываем текущие координаты банки по сетке 3х5
            int spotCol = spotIdx % 5;
            int spotRow = spotIdx / 5;

            // Сверяем с идеальными фабричными координатами
            if (spotCol == blockTargetColumn[i] && spotRow == blockTargetRow[i])
                correctCount++;
        }
    }


    void UpdateCounterUI()
    {
        if (counterText != null)
            counterText.text = $"Правильно: {correctCount}/{draggableObjects.Length}";
    }

    void Update()
    {
        // Плавное движение камеры к пазлу
        if (isCameraMovingToPuzzle)
        {
            MoveCameraTowardsTarget(cameraTargetPos, cameraTargetRot, ref isCameraMovingToPuzzle, OnPuzzleCameraArrived);
        }
        // Плавное возвращение камеры
        else if (isCameraMovingBack)
        {
            MoveCameraTowardsTarget(originalCameraPos, originalCameraRot, ref isCameraMovingBack, OnOriginalCameraArrived);
        }

        // Если пазл еще не решен, обрабатываем логику баночек
        if (!isCompleted)
        {
            if (isPuzzleActive && !isCameraMovingToPuzzle && !isCameraMovingBack)
                HandleSelectionAndSwap();

            if (Input.GetKeyDown(KeyCode.E) && !isPuzzleActive && !isCameraMovingToPuzzle && !isCameraMovingBack)
            {
                if (isPlayerNear) ActivatePuzzle();
            }
        }
        // Если пазл РЕШЕН и игрок нажимает E у стола
        else if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("[SYSTEM TEST] Зафиксировано нажатие кнопки E после конца игры с баночками!");

            if (playerCamera != null)
            {
                Ray testRay = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
                RaycastHit testHit;

                // Рисуем на сцене зеленую линию
                Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * 10f, Color.green, 3f);

                // ИСПРАВЛЕНИЕ: Пускаем луч, игнорируя триггеры и принудительно фильтруя попадания.
                // Мы делаем Raycast на 15 метров вперед.
                if (Physics.Raycast(testRay, out testHit, 15f))
                {
                    // Если луч ВСЁ ЕЩЁ по какой-то причине попал в игрока, мы принудительно пускаем ВТОРОЙ луч, 
                    // сместив его стартовую точку на 0.5 метра вперед — за пределы капсулы персонажа!
                    if (testHit.collider.gameObject.CompareTag("Player") || testHit.collider.gameObject.name == "Player")
                    {
                        Debug.Log("[SYSTEM TEST] Первый луч попал в игрока. Выталкиваем стартовую точку луча за пределы капсулы...");

                        Vector3 forwardStartPoint = playerCamera.transform.position + playerCamera.transform.forward * 0.6f;
                        Ray clearRay = new Ray(forwardStartPoint, playerCamera.transform.forward);

                        if (Physics.Raycast(clearRay, out testHit, 15f))
                        {
                            Debug.Log($"[SYSTEM TEST] УСПЕХ! Смещенный луч пробил капсулу и попал в: '{testHit.collider.gameObject.name}', Тег: '{testHit.collider.gameObject.tag}'");
                        }
                        else
                        {
                            Debug.LogWarning("[SYSTEM TEST] Смещенный луч улетел в пустоту.");
                        }
                    }
                    else
                    {
                        Debug.Log($"[SYSTEM TEST] Чистый луч сразу попал в объект: '{testHit.collider.gameObject.name}', Тег: '{testHit.collider.gameObject.tag}'");
                    }
                }
            }
        }
    }



    private void MoveCameraTowardsTarget(Vector3 targetPos, Quaternion targetRot, ref bool isMoving, System.Action onComplete)
    {
        playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, targetPos, cameraMoveSpeed * Time.deltaTime);
        playerCamera.transform.rotation = Quaternion.Lerp(playerCamera.transform.rotation, targetRot, cameraMoveSpeed * Time.deltaTime);

        if (Vector3.Distance(playerCamera.transform.position, targetPos) < 0.01f &&
            Quaternion.Angle(playerCamera.transform.rotation, targetRot) < 0.5f)
        {
            playerCamera.transform.position = targetPos;
            playerCamera.transform.rotation = targetRot;
            isMoving = false;
            onComplete?.Invoke();
        }
    }

    void ActivatePuzzle()
    {
        Debug.Log("[DEBUG] Метод ActivatePuzzle() начал работу.");

        if (hint != null) hint.SetActive(false);

        // 1. СНАЧАЛА ПОЛНОСТЬЮ ОСТАНАВЛИВАЕМ ИГРОКА И БЛОКИРУЕМ МЫШЬ
        DisableMouseControl();

        if (player != null)
        {
            savedCharController = player.GetComponent<CharacterController>();
            if (savedCharController != null) savedCharController.enabled = false;

            // Если у игрока есть физическое тело, гасим инерцию бега
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            var movement = player.GetComponent("PlayerMovement") as MonoBehaviour;
            if (movement != null)
            {
                savedMovementScript = movement;
                savedMovementScript.enabled = false; // Выключаем скрипт ходьбы
            }
        }

        // 2. И ТОЛЬКО ТЕПЕРЬ ЗАПОМИНАЕМ КООРДИНАТЫ КАМЕРЫ
        // Камера больше не дернется из-за инерции ходьбы в этот кадр
        originalCameraPos = playerCamera.transform.position;
        originalCameraRot = playerCamera.transform.rotation;

        // 3. ОТПРАВЛЯЕМ КАМЕРУ К СТЕНДУ
        if (counterText != null)
        {
            counterText.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[DEBUG] Внимание! Текст 'counterText' не назначен в инспекторе компонента.");
        }

        cameraTargetPos = puzzleCameraPosition.position;

        if (puzzleCameraLookAt != null)
            cameraTargetRot = Quaternion.LookRotation(puzzleCameraLookAt.position - puzzleCameraPosition.position);
        else
            cameraTargetRot = puzzleCameraPosition.rotation;

        isCameraMovingToPuzzle = true;
        isPuzzleActive = false;

        Debug.Log("[DEBUG] ActivatePuzzle успешно завершил работу. Камера зафиксирована и летит к стенду.");
    }



    private void OnOriginalCameraArrived()
    {
        // 1. Восстанавливаем управление мышью и скрипты обзора
        EnableMouseControl();

        // 2. Включаем физическое перемещение игрока обратно
        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;

            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            if (movement != null) movement.canMove = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (counterText != null)
        {
            counterText.gameObject.SetActive(false);
        }

        // Засчитываем задачу на планшетке
        ToggleClipboard clipboard = FindObjectOfType<ToggleClipboard>();
        if (clipboard != null)
        {
            clipboard.CompleteTask(5);
        }

        // ======================================================================
        // ЖЕСТКИЙ ЯВНЫЙ МЕТОД: ВОССТАНОВЛЕНИЕ ПОДБОРА ДЕТАЛЕЙ НА ВСЕМ ЭТАЖЕ
        // ======================================================================

        // 1. Находим вообще ВСЕ детали со скриптом ClickableDetailForSlots на сцене
        ClickableDetailForSlots[] allDetailsOnFloor = FindObjectsOfType<ClickableDetailForSlots>(true);

        foreach (var detail in allDetailsOnFloor)
        {
            if (detail != null)
            {
                // Принудительно включаем сам скрипт на детали, если он ушел в спячку
                detail.enabled = true;

                // Гарантируем, что у детали включен физический коллайдер для Рэйкаста
                Collider detailCol = detail.GetComponent<Collider>();
                if (detailCol != null) detailCol.enabled = true;

                // Восстанавливаем оригинальный слой и тег, если они сбились во время игры
                detail.gameObject.tag = "Consumable"; // Замените на ваш тег деталей, если он другой

                // Заставляем Unity принудительно обновить физическую матрицу этого объекта,
                // чтобы оригинальный скрипт подбора снова начал его видеть!
                detail.gameObject.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            }
        }

        // 2. Находим главный менеджер сборки платы и будим его
        AssemblySlotsManager slotsManager = FindObjectOfType<AssemblySlotsManager>();
        if (slotsManager != null)
        {
            slotsManager.enabled = true;
            // Даем сигнал менеджеру обновить интерфейс и проверить ввод
            slotsManager.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        }

        // 3. Находим игрока и принудительно возвращаем ему все кастомные скрипты взаимодействия
        GameObject mainPlayer = GameObject.FindGameObjectWithTag("Player");
        if (mainPlayer != null)
        {
            var playerScripts = mainPlayer.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var script in playerScripts)
            {
                if (script == null || script == this) continue;
                string name = script.GetType().Name;

                // Если ваш менеджер подбора называется как-то иначе, этот код принудительно его разбудит
                if (name.Contains("Interact") || name.Contains("Pick") || name.Contains("Raycast") || name.Contains("Input"))
                {
                    script.enabled = true;
                }
            }
        }

        // ======================================================================

        if (isCompleted)
        {
            PlayerPrefs.SetInt("PuzzleCompleted", 1);
            PlayerPrefs.Save();
            if (completeSound != null) audioSource.PlayOneShot(completeSound);
            if (successText != null)
            {
                successText.text = "Пазл решён! Получен номер телефона.";
                successText.gameObject.SetActive(true);
                Invoke(nameof(HideSuccessMessage), 3f);
            }

            // Полностью отключаем скрипт баночек, чтобы он не перехватывал кнопку E
            this.enabled = false;
            Debug.Log("[STAND FIXED] Скрипт баночек полностью отключен. Кнопка Е освобождена для подбора деталей.");
        }
        puzzleHolder.AddPuzzle(); // Прибавить один пазл и обновить экран!
        ClearSelectedHighlight();
    }



    private void OnPuzzleCameraArrived()
    {
        // Отключаем все скрипты, которые могут вращать камеру
        DisableMouseControl();

        // Включаем коллайдеры блоков для кликов
        foreach (var obj in draggableObjects)
        {
            if (obj != null)
            {
                Collider col = obj.GetComponent<Collider>();
                if (col != null) col.enabled = true;
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (player != null)
        {
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            if (movement != null) movement.canMove = false;
        }

        isPuzzleActive = true;
        UpdateCounterUI();
        Debug.Log("Пазл активирован, камера на месте, управление мышью отключено.");
    }


    void DeactivatePuzzle(bool completed)
    {
        if (isCameraMovingToPuzzle || isCameraMovingBack) return;

        isCameraMovingBack = true;
        isPuzzleActive = false;
        this.isCompleted = this.isCompleted || completed;
    }

    void HandleSelectionAndSwap()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject hitObj = hit.collider.gameObject;
                int hitIndex = System.Array.IndexOf(draggableObjects, hitObj);
                if (hitIndex != -1)
                {
                    if (selectedBlockIndex == -1)
                    {
                        selectedBlockIndex = hitIndex;
                        HighlightBlock(selectedBlockIndex, true);
                    }
                    else if (selectedBlockIndex == hitIndex)
                    {
                        ClearSelectedHighlight(true);
                        UpdateCounterUI();
                    }
                    else
                    {
                        int firstIndex = selectedBlockIndex;
                        int secondIndex = hitIndex;
                        ClearSelectedHighlight(false);
                        SwapBlocks(firstIndex, secondIndex);
                        UpdateCorrectCount();
                        UpdateCounterUI();
                        if (correctCount == draggableObjects.Length)
                            DeactivatePuzzle(true);
                    }
                }
                else
                {
                    if (selectedBlockIndex != -1) ClearSelectedHighlight(true);
                }
            }
            else
            {
                if (selectedBlockIndex != -1) ClearSelectedHighlight(true);
            }
        }
    }

    void SwapBlocks(int indexA, int indexB)
    {
        GameObject blockA = draggableObjects[indexA];
        GameObject blockB = draggableObjects[indexB];
        int spotA = objectSpotIndex[indexA];
        int spotB = objectSpotIndex[indexB];
        if (spotA == -1 || spotB == -1) return;

        Vector3 posA = blockA.transform.position;
        Quaternion rotA = blockA.transform.rotation;
        blockA.transform.position = blockB.transform.position;
        blockA.transform.rotation = blockB.transform.rotation;
        blockB.transform.position = posA;
        blockB.transform.rotation = rotA;

        spotOccupants[spotA] = blockB;
        spotOccupants[spotB] = blockA;
        objectSpotIndex[indexA] = spotB;
        objectSpotIndex[indexB] = spotA;

        if (swapSound != null) audioSource.PlayOneShot(swapSound);
    }

    void HighlightBlock(int index, bool highlight, bool animateReturn = true)
    {
        if (index < 0 || index >= draggableObjects.Length) return;
        GameObject block = draggableObjects[index];
        if (block == null) return;
        if (currentMoveCoroutine != null) StopCoroutine(currentMoveCoroutine);

        if (highlight)
        {
            selectedBlockOriginalPosition = block.transform.position;
            Vector3 targetPos = selectedBlockOriginalPosition + Vector3.right * 0.2f;
            currentMoveCoroutine = StartCoroutine(MoveBlockSmooth(block, selectedBlockOriginalPosition, targetPos));
        }
        else
        {
            if (animateReturn)
                currentMoveCoroutine = StartCoroutine(MoveBlockSmooth(block, block.transform.position, selectedBlockOriginalPosition));
            else
                block.transform.position = selectedBlockOriginalPosition;
        }
    }

    void ClearSelectedHighlight(bool animate = true)
    {
        if (selectedBlockIndex != -1)
        {
            HighlightBlock(selectedBlockIndex, false, animate);
            selectedBlockIndex = -1;
        }
    }

    System.Collections.IEnumerator MoveBlockSmooth(GameObject block, Vector3 from, Vector3 to)
    {
        float duration = 0.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            block.transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }
        block.transform.position = to;
        currentMoveCoroutine = null;
    }

    void HideSuccessMessage()
    {
        if (successText != null) successText.gameObject.SetActive(false);
    }

    private void DisableMouseControl()
    {
        List<MonoBehaviour> scriptsToDisable = new List<MonoBehaviour>();

        // 1. Отключаем пользовательские скрипты на камере
        var cameraScripts = playerCamera.GetComponents<MonoBehaviour>();
        foreach (var script in cameraScripts)
        {
            if (script == null) continue;
            if (script == this) continue;
            string typeName = script.GetType().Name;
            if (typeName == "Camera" || typeName == "Transform" || typeName == "RectTransform" || typeName == "AudioListener")
                continue;
            scriptsToDisable.Add(script);
        }

        // 2. Ищем скрипты вращения на игроке
        if (player != null)
        {
            var allPlayerScripts = player.GetComponentsInChildren<MonoBehaviour>();
            foreach (var script in allPlayerScripts)
            {
                if (script == null || script == this) continue;
                string typeName = script.GetType().Name;

                // ИСПРАВЛЕНО: Мы убрали "Input" и "Controller" из черного списка! 
                // Теперь блокируется ТОЛЬКО вращение мыши (Mouse и Look), а клики (Raycast) продолжают работать!
                if (typeName.Contains("Mouse") || typeName.Contains("Look") || typeName.Contains("Camera"))
                {
                    if (typeName != "Camera")
                    {
                        scriptsToDisable.Add(script);
                    }
                }
            }
        }

        if (scriptsToDisable.Count > 0)
        {
            mouseControlScripts = scriptsToDisable.ToArray();
            mouseControlScriptsEnabledState = new bool[mouseControlScripts.Length];
            for (int i = 0; i < mouseControlScripts.Length; i++)
            {
                mouseControlScriptsEnabledState[i] = mouseControlScripts[i].enabled;
                mouseControlScripts[i].enabled = false;
            }
            Debug.Log($"Отключено {mouseControlScripts.Length} скриптов обзора. Ввод мыши оставлен активным для Raycast.");
        }
    }


    private void EnableMouseControl()
    {
        // 1. Восстанавливаем сохраненные скрипты из массива
        if (mouseControlScripts != null)
        {
            for (int i = 0; i < mouseControlScripts.Length; i++)
            {
                if (mouseControlScripts[i] != null)
                    mouseControlScripts[i].enabled = mouseControlScriptsEnabledState[i];
            }
            mouseControlScripts = null;
        }

        // 2. Восстанавливаем физику и скрипт движения игрока
        if (savedCharController != null)
        {
            savedCharController.enabled = true;
            savedCharController = null;
        }

        if (savedMovementScript != null)
        {
            savedMovementScript.enabled = true;
            savedMovementScript = null;
        }

        // 3. ГАРАНТИРОВАННЫЙ ВОЗВРАТ ПОВОРОТОВ КАМЕРЫ:
        // Находим вообще ВСЕ скрипты на камере и включаем те, что отвечают за мышь/обзор
        var allCameraScripts = playerCamera.GetComponents<MonoBehaviour>();
        foreach (var script in allCameraScripts)
        {
            if (script == null || script == this) continue;
            string typeName = script.GetType().Name;

            // Включаем обратно MouseLook, CameraLook, FirstPersonLook и любые скрипты со словами Mouse/Look
            if (typeName.Contains("Mouse") || typeName.Contains("Look") || typeName.Contains("Camera") || typeName.Contains("Input"))
            {
                script.enabled = true;
            }
        }


        // 4. Если скрипт обзора висел на самом игроке или его детях — включаем и там
        if (player != null)
        {
            var allPlayerScripts = player.GetComponentsInChildren<MonoBehaviour>();
            foreach (var script in allPlayerScripts)
            {
                if (script == null || script == this) continue;
                string typeName = script.GetType().Name;
                if (typeName.Contains("Mouse") || typeName.Contains("Look") || typeName.Contains("Camera") || typeName.Contains("Input"))
                {
                    if (typeName != "Camera") // Не трогаем сам системный компонент Camera Unity
                    {
                        script.enabled = true;
                    }
                }
            }
        }

        Debug.Log("Повороты камеры и мышь принудительно разблокированы!");
    }

    // ========== ТРИГГЕРЫ ДЛЯ ВЗАИМОДЕЙСТВИЯ ==========
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[DEBUG] В триггер вошел объект: {other.name} с тегом: '{other.tag}'");

        if (other.CompareTag("Player"))
        {
            if (!isCompleted)
            {
                isPlayerNear = true;
                player = other.gameObject;
                if (hint != null) hint.SetActive(true);
                Debug.Log("[DEBUG] Игрок успешно распознан. Текст-подсказка включен. Теперь можно нажать E.");
            }
            else
            {
                Debug.Log("[DEBUG] Игрок вошел в триггер, но этот пазл уже имеет статус выполненного (isCompleted = true).");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[DEBUG] Игрок вышел из триггер-зоны стенда.");
            isPlayerNear = false;
            player = null;
            if (hint != null) hint.SetActive(false);
        }
    }
}