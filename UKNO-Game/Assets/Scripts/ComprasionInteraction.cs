using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class ComprasionInteractions : MonoBehaviour
{
    [Header("Настройки камеры")]
    public Camera playerCamera;
    public Transform puzzleCameraPosition;
    public Transform puzzleCameraLookAt;

    [Header("Объекты и точки")]
    public GameObject[] draggableObjects;   // 9 блоков
    public Transform[] targetSpots;         // 9 точек (столбцы × строки)
    public int[] spotToColumn;              // для каждой точки (0..8): номер столбца (0,1,2)
    public int[] spotToRow;                 // для каждой точки (0..8): номер строки (0,1,2)

    [Header("Цвета столбцов (основные)")]
    public Color[] colorOptions = new Color[] { Color.red, Color.green, Color.blue };

    [Header("UI")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI counterText;
    public TextMeshProUGUI successText;

    [Header("Звуки")]
    public AudioClip swapSound;
    public AudioClip completeSound;

    private AudioSource audioSource;
    private bool isPlayerNear = false;
    private bool isPuzzleActive = false;
    private bool isCompleted = false;
    private GameObject player;

    // Состояние блоков
    private GameObject[] spotOccupants;
    private int[] objectSpotIndex;           // для каждого блока – индекс точки (-1 если не на точке)
    private int correctCount;

    // Цвета столбцов (генерируются случайно)
    private Color[] columnColors = new Color[3];

    // Целевые параметры блоков (не меняются в игре)
    private int[] blockTargetColumn = new int[9];
    private int[] blockTargetRow = new int[9];

    // Выделение блока
    private int selectedBlockIndex = -1;
    private Vector3 selectedBlockOriginalPosition;
    private Coroutine currentMoveCoroutine;

    private Vector3 originalCameraPos;
    private Quaternion originalCameraRot;

    void Start()
    {
        PlayerPrefs.DeleteKey("PuzzleCompleted"); // для теста

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Инициализация массивов
        spotOccupants = new GameObject[targetSpots.Length];
        objectSpotIndex = new int[draggableObjects.Length];
        for (int i = 0; i < objectSpotIndex.Length; i++) objectSpotIndex[i] = -1;

        // Задаём каждому блоку его целевой столбец и строку
        // Первые 3 блока: столбец 0, строки 0,1,2
        // Следующие 3: столбец 1, строки 0,1,2
        // Последние 3: столбец 2, строки 0,1,2
        for (int i = 0; i < draggableObjects.Length; i++)
        {
            int col = i / 3;   // 0,0,0,1,1,1,2,2,2
            int row = i % 3;   // 0,1,2,0,1,2,0,1,2
            blockTargetColumn[i] = col;
            blockTargetRow[i] = row;
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
        if (instructionText != null) instructionText.text = "";
    }

    void GenerateRandomConfiguration()
    {
        // 1. Случайный порядок цветов для столбцов
        List<Color> shuffledColors = new List<Color>(colorOptions);
        for (int i = 0; i < shuffledColors.Count; i++)
        {
            Color temp = shuffledColors[i];
            int randomIndex = Random.Range(i, shuffledColors.Count);
            shuffledColors[i] = shuffledColors[randomIndex];
            shuffledColors[randomIndex] = temp;
        }
        for (int i = 0; i < 3; i++)
            columnColors[i] = shuffledColors[i];

        // 2. Перекрашиваем блоки в цвет их целевого столбца (используя текущие columnColors)
        for (int i = 0; i < draggableObjects.Length; i++)
        {
            if (draggableObjects[i] == null) continue;
            Renderer rend = draggableObjects[i].GetComponent<Renderer>();
            if (rend != null)
            {
                int targetCol = blockTargetColumn[i];
                rend.material.color = columnColors[targetCol];
            }
        }

        // 3. Случайное размещение блоков по всем точкам
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
            if (spotIdx != -1)
            {
                int spotCol = spotToColumn[spotIdx];
                int spotRow = spotToRow[spotIdx];
                if (spotCol == blockTargetColumn[i] && spotRow == blockTargetRow[i])
                    correctCount++;
            }
        }
    }


    bool IsColorMatch(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.1f &&
               Mathf.Abs(a.g - b.g) < 0.1f &&
               Mathf.Abs(a.b - b.b) < 0.1f;
    }

    void UpdateCounterUI()
    {
        if (counterText != null)
            counterText.text = $"Правильно: {correctCount}/{draggableObjects.Length}";
    }

    void Update()
    {
        if (!isCompleted && isPlayerNear && Input.GetKeyDown(KeyCode.E) && !isPuzzleActive)
            ActivatePuzzle();

        if (isPuzzleActive && !isCompleted)
            HandleSelectionAndSwap();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isCompleted)
        {
            isPlayerNear = true;
            player = other.gameObject;
            if (instructionText != null)
                instructionText.text = "Нажмите E, чтобы начать пазл";
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            player = null;
            if (instructionText != null)
                instructionText.text = "";
        }
    }

    void ActivatePuzzle()
    {
        isPuzzleActive = true;

        if (player != null)
        {
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            if (movement != null) movement.canMove = false;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Камера пазла
        originalCameraPos = playerCamera.transform.position;
        originalCameraRot = playerCamera.transform.rotation;
        if (puzzleCameraPosition != null)
        {
            playerCamera.transform.position = puzzleCameraPosition.position;
            if (puzzleCameraLookAt != null)
                playerCamera.transform.LookAt(puzzleCameraLookAt);
            else
                playerCamera.transform.rotation = puzzleCameraPosition.rotation;
        }

        // Включаем коллайдеры для кликов
        foreach (var obj in draggableObjects)
        {
            if (obj != null)
            {
                Collider col = obj.GetComponent<Collider>();
                if (col != null) col.enabled = true;
            }
        }

        if (instructionText != null)
            instructionText.text = $"Кликните на блок, затем на другой, чтобы поменять их местами. (Правильно: {correctCount}/{draggableObjects.Length})";
    }

    void DeactivatePuzzle(bool completed)
    {
        isPuzzleActive = false;

        // Возвращаем камеру
        playerCamera.transform.position = originalCameraPos;
        playerCamera.transform.rotation = originalCameraRot;

        if (player != null)
        {
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            if (movement != null) movement.canMove = true;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (completed)
        {
            isCompleted = true;
            PlayerPrefs.SetInt("PuzzleCompleted", 1);
            PlayerPrefs.Save();

            if (completeSound != null) audioSource.PlayOneShot(completeSound);
            if (successText != null)
            {
                successText.text = "Пазл решён! Получен фрагмент.";
                successText.gameObject.SetActive(true);
                Invoke(nameof(HideSuccessMessage), 3f);
            }
        }

        // Сбрасываем подсветку, если была
        ClearSelectedHighlight();

        if (instructionText != null) instructionText.text = "";
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
                    if (instructionText != null)
                        instructionText.text = "Выберите второй блок для обмена.";
                }
                else if (selectedBlockIndex == hitIndex)
                {
                    ClearSelectedHighlight(true); // плавный возврат
                    if (instructionText != null)
                        instructionText.text = $"Выбор сброшен. (Правильно: {correctCount}/{draggableObjects.Length})";
                }
                else
                {
                    // Сохраняем индексы до сброса выделения
                    int firstIndex = selectedBlockIndex;
                    int secondIndex = hitIndex;
                    
                    ClearSelectedHighlight(false); // сброс без анимации
                    SwapBlocks(firstIndex, secondIndex);
                    
                    UpdateCorrectCount();
                    UpdateCounterUI();
                    
                    if (correctCount == draggableObjects.Length)
                    {
                        if (instructionText != null) instructionText.text = "Отлично! Пазл завершён!";
                        DeactivatePuzzle(true);
                    }
                    else
                    {
                        if (instructionText != null)
                            instructionText.text = $"Обмен выполнен. Правильно: {correctCount}/{draggableObjects.Length}";
                    }
                }
            }
            else
            {
                if (selectedBlockIndex != -1)
                {
                    ClearSelectedHighlight(true);
                    if (instructionText != null)
                        instructionText.text = $"Выбор сброшен. (Правильно: {correctCount}/{draggableObjects.Length})";
                }
            }
        }
        else
        {
            if (selectedBlockIndex != -1)
            {
                ClearSelectedHighlight(true);
                if (instructionText != null)
                    instructionText.text = $"Выбор сброшен. (Правильно: {correctCount}/{draggableObjects.Length})";
            }
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

        // Меняем позиции в мире
        Vector3 posA = blockA.transform.position;
        Quaternion rotA = blockA.transform.rotation;
        blockA.transform.position = blockB.transform.position;
        blockA.transform.rotation = blockB.transform.rotation;
        blockB.transform.position = posA;
        blockB.transform.rotation = rotA;

        // Обновляем массивы занятости точек
        spotOccupants[spotA] = blockB;
        spotOccupants[spotB] = blockA;

        // Обновляем индексы точек для блоков
        objectSpotIndex[indexA] = spotB;
        objectSpotIndex[indexB] = spotA;

        if (swapSound != null) audioSource.PlayOneShot(swapSound);
    }

void HighlightBlock(int index, bool highlight, bool animateReturn = true)
{
    if (index < 0 || index >= draggableObjects.Length) return;
    GameObject block = draggableObjects[index];
    if (block == null) return;

    if (currentMoveCoroutine != null)
        StopCoroutine(currentMoveCoroutine);

    if (highlight)
    {
        selectedBlockOriginalPosition = block.transform.position;
        Vector3 targetPos = selectedBlockOriginalPosition + Vector3.right * 0.2f; // смещение на камеру
        currentMoveCoroutine = StartCoroutine(MoveBlockSmooth(block, selectedBlockOriginalPosition, targetPos));
    }
    else
    {
        if (animateReturn)
        {
            currentMoveCoroutine = StartCoroutine(MoveBlockSmooth(block, block.transform.position, selectedBlockOriginalPosition));
        }
        else
        {
            block.transform.position = selectedBlockOriginalPosition;
            currentMoveCoroutine = null;
        }
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
}