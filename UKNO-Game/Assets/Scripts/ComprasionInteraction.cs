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

        hint.SetActive(false);
        counterText.gameObject.SetActive(false);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        spotOccupants = new GameObject[targetSpots.Length];
        objectSpotIndex = new int[draggableObjects.Length];
        for (int i = 0; i < objectSpotIndex.Length; i++) objectSpotIndex[i] = -1;

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
            GenerateRandomConfiguration();
        }

        if (successText != null) successText.gameObject.SetActive(false);
        Debug.Log("ComprasionInteractions Start - готов");
    }

    void GenerateRandomConfiguration()
    {
        List<Color> sourceColors = new List<Color>(colorOptions);
        while (sourceColors.Count < 5)
            sourceColors.Add(Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.8f, 1f));

        List<Color> shuffledColors = new List<Color>(sourceColors);
        for (int i = 0; i < shuffledColors.Count; i++)
        {
            Color temp = shuffledColors[i];
            int randomIndex = Random.Range(i, shuffledColors.Count);
            shuffledColors[i] = shuffledColors[randomIndex];
            shuffledColors[randomIndex] = temp;
        }
        for (int i = 0; i < 5; i++)
            columnColors[i] = shuffledColors[i];

        for (int i = 0; i < draggableObjects.Length; i++)
        {
            if (draggableObjects[i] == null) continue;
            Renderer rend = draggableObjects[i].GetComponent<Renderer>();
            if (rend != null)
                rend.material.color = columnColors[blockTargetColumn[i]];
        }

        List<int> freeSpots = new List<int>();
        for (int i = 0; i < targetSpots.Length; i++) freeSpots.Add(i);

        for (int i = 0; i < draggableObjects.Length; i++)
        {
            if (draggableObjects[i] == null) continue;
            int rand = Random.Range(0, freeSpots.Count);
            int spotIdx = freeSpots[rand];
            freeSpots.RemoveAt(rand);

            spotOccupants[spotIdx] = draggableObjects[i];
            objectSpotIndex[i] = spotIdx;
            draggableObjects[i].transform.position = targetSpots[spotIdx].position;
            draggableObjects[i].transform.rotation = targetSpots[spotIdx].rotation;
        }

        UpdateCorrectCount();
        UpdateCounterUI();
    }

    void UpdateCorrectCount()
    {
        correctCount = 0;
        for (int i = 0; i < draggableObjects.Length; i++)
        {
            int spotIdx = objectSpotIndex[i];
            if (spotIdx == -1) continue;
            int spotCol = spotIdx % 5;
            int spotRow = spotIdx / 5;
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

        if (!isCompleted && isPuzzleActive)
            HandleSelectionAndSwap();

        if (!isCompleted && isPlayerNear && Input.GetKeyDown(KeyCode.E) && !isPuzzleActive && !isCameraMovingToPuzzle && !isCameraMovingBack)
        {
            ActivatePuzzle();
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
        if (hint != null) hint.SetActive(false);
        counterText.gameObject.SetActive(true);

        originalCameraPos = playerCamera.transform.position;
        originalCameraRot = playerCamera.transform.rotation;
        cameraTargetPos = puzzleCameraPosition.position;

        if (puzzleCameraLookAt != null)
            cameraTargetRot = Quaternion.LookRotation(puzzleCameraLookAt.position - puzzleCameraPosition.position);
        else
            cameraTargetRot = puzzleCameraPosition.rotation;

        // ЗАПОМИНАЕМ ССЫЛКИ НА КОМПОНЕНТЫ ИГРОКА ДО ТОГО, КАК ОНИ ВЫКЛЮЧАТСЯ
        if (player != null)
        {
            savedCharController = player.GetComponent<CharacterController>();
            if (savedCharController != null) savedCharController.enabled = false;

            // Находим кастомный скрипт движения (пытаемся найти PlayerMovement)
            var movement = player.GetComponent("PlayerMovement") as MonoBehaviour;
            if (movement != null)
            {
                savedMovementScript = movement;
                savedMovementScript.enabled = false; // Выключаем скрипт ходьбы
            }
        }

        DisableMouseControl();

        isCameraMovingToPuzzle = true;
        isPuzzleActive = false;
    }

    private void OnOriginalCameraArrived()
    {
        // Восстанавливаем управление мышью
        EnableMouseControl();

        // ТОЧЕЧНОЕ ИСПРАВЛЕНИЕ: Включаем физическое перемещение обратно
        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;

            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            if (movement != null) movement.canMove = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        counterText.gameObject.SetActive(false);

        ToggleClipboard clipboard = FindObjectOfType<ToggleClipboard>();
        if (clipboard != null)
        {
            clipboard.CompleteTask(5);
        }

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
        }

        ClearSelectedHighlight();
        Debug.Log("Пазл деактивирован, управление мышью восстановлено.");
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
        if (other.CompareTag("Player") && !isCompleted)
        {
            isPlayerNear = true;
            player = other.gameObject;
            if (hint != null) hint.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            player = null;
            if (hint != null) hint.SetActive(false);
        }
    }
}