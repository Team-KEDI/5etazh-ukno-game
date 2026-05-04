using UnityEngine;
using UnityEngine.UI;

public class PhoneSystem : MonoBehaviour
{
    [Header("Настройки телефона")]
    public string[] phoneNumbers; // Массив номеров (например "123456", "654321")
    public AudioClip[] phoneSounds; // Звуки для каждого номера
    public AudioClip errorSound; // Звук при ошибке
    public AudioClip clickSound; // Звук нажатия кнопки

    [Header("UI")]
    public TextMesh displayText; // 3D текст на телефоне
    public GameObject wrongMessage; // Сообщение об ошибке
    public GameObject successMessage; // Сообщение об успехе

    [Header("Позиции")]
    public Transform phoneViewPoint; // Точка куда встает игрок
    public Transform phoneCameraPoint; // Точка откуда смотрит камера на телефон

    [Header("Настройки")]
    public float moveSpeed = 5f;
    public int maxInputLength = 6; // 6 цифр

    [Header("Подсказка")]
    public GameObject interactionPrompt;

    private bool isGameActive = false;
    private string currentInput = "";
    private bool isWaitingForCompletion = false;

    private Vector3 originalPlayerPos;
    private Quaternion originalPlayerRot;
    private Vector3 originalCameraPos;
    private Quaternion originalCameraRot;

    private GameObject player;
    private PlayerMovement playerMovement;
    private Rigidbody playerRigidbody;
    private Camera mainCamera;
    private MonoBehaviour cameraController;

    private bool isPlayerNear = false;
    private bool isCompleted = false;

    void Start()
    {
        mainCamera = Camera.main;

        if (mainCamera != null)
        {
            cameraController = mainCamera.GetComponent<MonoBehaviour>();
            if (cameraController == null)
                cameraController = mainCamera.GetComponent("MouseLook") as MonoBehaviour;
        }

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
        if (wrongMessage != null)
            wrongMessage.SetActive(false);
        if (successMessage != null)
            successMessage.SetActive(false);

        UpdateDisplay();

        Debug.Log($"PhoneSystem инициализирован. Длина номера: {maxInputLength}");
        Debug.Log($"Доступные номера: {string.Join(", ", phoneNumbers)}");
    }

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.E) && !isGameActive && !isCompleted)
        {
            StartPhoneGame();
        }

        if (isGameActive && Input.GetKeyDown(KeyCode.Escape))
        {
            StopPhoneGame();
        }

        if (isWaitingForCompletion && !IsPlayingAudio())
        {
            isWaitingForCompletion = false;
        }

        if (isGameActive && player != null && phoneViewPoint != null)
        {
            player.transform.position = Vector3.Lerp(player.transform.position, phoneViewPoint.position, moveSpeed * Time.deltaTime);

            Vector3 direction = phoneCameraPoint.position - player.transform.position;
            direction.y = 0;
            Quaternion targetRot = Quaternion.LookRotation(direction);
            player.transform.rotation = Quaternion.Lerp(player.transform.rotation, targetRot, moveSpeed * Time.deltaTime);
        }

        if (isGameActive && mainCamera != null && phoneCameraPoint != null)
        {
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, phoneCameraPoint.position, moveSpeed * Time.deltaTime);
            mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, phoneCameraPoint.rotation, moveSpeed * Time.deltaTime);
        }
    }

    bool IsPlayingAudio()
    {
        AudioSource[] sources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource source in sources)
        {
            if (source.isPlaying)
                return true;
        }
        return false;
    }

    void StartPhoneGame()
    {
        isGameActive = true;
        currentInput = "";
        UpdateDisplay();

        Debug.Log("Телефонная игра начата");

        if (player != null)
        {
            originalPlayerPos = player.transform.position;
            originalPlayerRot = player.transform.rotation;
        }

        if (mainCamera != null)
        {
            originalCameraPos = mainCamera.transform.position;
            originalCameraRot = mainCamera.transform.rotation;
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = true;
            playerRigidbody.velocity = Vector3.zero;
        }

        if (playerMovement != null)
            playerMovement.canMove = false;

        if (cameraController != null)
            cameraController.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        HidePlayerModel(true);
    }

    void StopPhoneGame()
    {
        isGameActive = false;
        isWaitingForCompletion = false;

        Debug.Log("Телефонная игра остановлена");

        if (player != null)
        {
            player.transform.position = originalPlayerPos;
            player.transform.rotation = originalPlayerRot;

            if (playerRigidbody != null)
            {
                playerRigidbody.velocity = Vector3.zero;
                playerRigidbody.isKinematic = false;
            }
        }

        if (mainCamera != null)
        {
            mainCamera.transform.position = originalCameraPos;
            mainCamera.transform.rotation = originalCameraRot;
        }

        if (playerMovement != null)
            playerMovement.canMove = true;

        if (cameraController != null)
            cameraController.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        HidePlayerModel(false);

        if (wrongMessage != null)
            wrongMessage.SetActive(false);
        if (successMessage != null)
            successMessage.SetActive(false);

        currentInput = "";
        UpdateDisplay();
    }

    public void OnButtonPressed(string digit)
    {
        if (!isGameActive) return;
        if (isWaitingForCompletion) return;

        Debug.Log($"Нажата кнопка: {digit}");

        if (clickSound != null)
        {
            AudioSource.PlayClipAtPoint(clickSound, mainCamera.transform.position);
        }

        if (currentInput.Length < maxInputLength)
        {
            currentInput += digit;
            UpdateDisplay();
            Debug.Log($"Текущий ввод: \"{currentInput}\" (длина: {currentInput.Length})");

            if (currentInput.Length == maxInputLength)
            {
                Debug.Log($"Введено {maxInputLength} цифр: \"{currentInput}\", ищем звук...");
                PlaySoundForNumber();
            }
        }
    }

    void PlaySoundForNumber()
    {
        int foundIndex = -1;

        Debug.Log($"Поиск номера \"{currentInput}\" в списке: [{string.Join(", ", phoneNumbers)}]");

        // Ищем номер в списке
        for (int i = 0; i < phoneNumbers.Length; i++)
        {
            Debug.Log($"Сравниваем с phoneNumbers[{i}] = \"{phoneNumbers[i]}\"");
            if (currentInput == phoneNumbers[i])
            {
                foundIndex = i;
                Debug.Log($"СОВПАДЕНИЕ! Индекс {i}");
                break; // Выходим из цикла при первом совпадении
            }
        }

        // Проверяем результат поиска
        if (foundIndex != -1)
        {
            // Проверяем наличие звука
            if (phoneSounds != null && foundIndex < phoneSounds.Length && phoneSounds[foundIndex] != null)
            {
                Debug.Log($"Номер \"{currentInput}\" найден! Воспроизводим звук {foundIndex}");
                AudioSource.PlayClipAtPoint(phoneSounds[foundIndex], mainCamera.transform.position);
                isWaitingForCompletion = true;
                ShowSuccess();
            }
            else
            {
                Debug.LogWarning($"Номер \"{currentInput}\" найден, но звук для индекса {foundIndex} не назначен!");
                ShowWrong();
                if (errorSound != null)
                {
                    AudioSource.PlayClipAtPoint(errorSound, mainCamera.transform.position);
                }
            }
        }
        else
        {
            Debug.Log($"Номер \"{currentInput}\" НЕ НАЙДЕН в списке!");
            ShowWrong();
            if (errorSound != null)
            {
                AudioSource.PlayClipAtPoint(errorSound, mainCamera.transform.position);
            }
        }

        // Очищаем ввод после попытки
        ClearInput();
    }

    void ShowWrong()
    {
        if (wrongMessage != null)
        {
            wrongMessage.SetActive(true);
            Invoke("HideWrong", 1.5f);
        }
    }

    void HideWrong()
    {
        if (wrongMessage != null)
            wrongMessage.SetActive(false);
    }

    void ShowSuccess()
    {
        if (successMessage != null)
        {
            successMessage.SetActive(true);
            Invoke("HideSuccess", 1.5f);
        }
    }

    void HideSuccess()
    {
        if (successMessage != null)
            successMessage.SetActive(false);
    }

    void ClearInput()
    {
        currentInput = "";
        UpdateDisplay();
        Debug.Log("Ввод очищен");
    }

    void UpdateDisplay()
    {
        if (displayText != null)
        {
            string display = currentInput;
            for (int i = display.Length; i < maxInputLength; i++)
            {
                display += "_";
            }
            displayText.text = display;
        }
    }

    void CompleteGame()
    {
        isCompleted = true;
        isGameActive = false;

        Debug.Log("ИГРА ЗАВЕРШЕНА! Пазл получен!");

        PlayerPrefs.SetInt("PhoneGameCompleted", 1);
        PlayerPrefs.Save();

        if (successMessage != null)
        {
            TextMesh textMesh = successMessage.GetComponent<TextMesh>();
            if (textMesh != null)
                textMesh.text = "Задание выполнено!\nПазл получен!";
            successMessage.SetActive(true);
        }

        Invoke("StopPhoneGame", 3f);
    }

    public bool IsGameActive()
    {
        return isGameActive;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isCompleted)
        {
            isPlayerNear = true;
            player = other.gameObject;
            playerMovement = player.GetComponent<PlayerMovement>();
            playerRigidbody = player.GetComponent<Rigidbody>();

            if (interactionPrompt != null)
                interactionPrompt.SetActive(true);

            Debug.Log("Игрок подошел к телефону");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;

            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);

            if (isGameActive)
            {
                StopPhoneGame();
            }

            player = null;
            playerMovement = null;
            playerRigidbody = null;

            Debug.Log("Игрок отошел от телефона");
        }
    }

    void HidePlayerModel(bool hide)
    {
        if (player == null) return;

        Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer.GetComponent<Camera>() != null) continue;
            renderer.enabled = !hide;
        }

        Collider playerCollider = player.GetComponent<Collider>();
        if (playerCollider != null)
            playerCollider.enabled = !hide;
    }
}