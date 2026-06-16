using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class PendantSystem : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject gamePanel;
    public GameObject rolePanel;
    public GameObject connectionPanel;
    public GameObject phrasePanel;
    public GameObject resultPanel;

    [Header("Role Selection (Dropdown)")]
    public TMP_Dropdown roleDropdown; // Ссылка на выпадающий список ролей
    public Button confirmRoleButton;  // Кнопка "Далее" на панели ролей

    [Header("Connection Selection (Dropdown)")]
    public TMP_Dropdown connectionDropdown; // Ссылка на выпадающий список связей
    public Button confirmConnectionButton;  // Кнопка "Далее" на панели связей

    [Header("Phrase Input")]
    public TMP_InputField phraseInput; // TMP поле ввода
    public TextMeshProUGUI errorText; // TMP текст ошибки
    public TextMeshProUGUI titleText; // TMP заголовок

    [Header("Result Display")]
    public TextMeshProUGUI resultRoleText;
    public TextMeshProUGUI resultConnectionText;
    public TextMeshProUGUI resultPhraseText;

    [Header("Finish")]
    public Transform pendantTarget;
    public GameObject pendantPrefab;
    public GameObject completionMessage;
    public TextMeshProUGUI completionMessageText;

    [Header("Positions")]
    public Transform playerViewPoint;
    public Transform cameraViewPoint;

    [Header("Settings")]
    public float moveSpeed = 5f;

    [Header("Prompt")]
    public GameObject interactionPrompt;
    public TextMeshProUGUI promptText;
    public PuzzleHolder puzzleHolder; // Ссылка на менеджер пазлов

    // Списки данных, которые автоматически заполнят Dropdown-компоненты
    private readonly string[] roles = {
        "работник хлебзавода",
        "организатор мероприятий",
        "любитель креативных кластеров",
        "фанат заводов",
        "ученик",
        "посетитель кластера",
        "пытливый ум",
        "случайный прохожий",
        "любопытный житель",
        "мастер дела",
        "творец"
    };

    private readonly string[] connections = {
        "не имею прямого отношения к району",
        "живу здесь более 5 лет",
        "живу здесь менее 5 лет",
        "провожу свободное время здесь",
        "работаю в Чкаловском районе",
        "родился(-ась) в Чкаловском районе"
    };

    private string selectedRole = "";
    private string selectedConnection = "";
    private string userPhrase = "";

    private bool isGameActive = false;
    private bool isCompleted = false;

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

        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (errorText != null)
            errorText.gameObject.SetActive(false);

        if (completionMessage != null)
            completionMessage.SetActive(false);

        // --- НАСТРОЙКА DROPDOWN РОЛЕЙ ---
        if (roleDropdown != null)
        {
            roleDropdown.ClearOptions();
            List<TMP_Dropdown.OptionData> roleOptions = new List<TMP_Dropdown.OptionData>();
            foreach (string role in roles)
            {
                roleOptions.Add(new TMP_Dropdown.OptionData(role));
            }
            roleDropdown.AddOptions(roleOptions);

            if (roles.Length > 0) selectedRole = roles[0];
            roleDropdown.onValueChanged.AddListener(OnRoleDropdownChanged);
        }

        if (confirmRoleButton != null)
        {
            confirmRoleButton.onClick.AddListener(ConfirmRoleAndContinue);
        }

        // --- НАСТРОЙКА DROPDOWN СВЯЗЕЙ ---
        if (connectionDropdown != null)
        {
            connectionDropdown.ClearOptions();
            List<TMP_Dropdown.OptionData> connectionOptions = new List<TMP_Dropdown.OptionData>();
            foreach (string conn in connections)
            {
                connectionOptions.Add(new TMP_Dropdown.OptionData(conn));
            }
            connectionDropdown.AddOptions(connectionOptions);

            if (connections.Length > 0) selectedConnection = connections[0];
            connectionDropdown.onValueChanged.AddListener(OnConnectionDropdownChanged);
        }

        if (confirmConnectionButton != null)
        {
            confirmConnectionButton.onClick.AddListener(ConfirmConnectionAndContinue);
        }
    }

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.E) && !isGameActive && !isCompleted)
        {
            StartGame();
        }

        if (isGameActive && Input.GetKeyDown(KeyCode.Escape))
        {
            StopGame();
        }

        if (isGameActive && player != null && playerViewPoint != null)
        {
            player.transform.position = Vector3.Lerp(player.transform.position, playerViewPoint.position, moveSpeed * Time.deltaTime);

            Vector3 direction = cameraViewPoint.position - player.transform.position;
            direction.y = 0;
            Quaternion targetRot = Quaternion.LookRotation(direction);
            player.transform.rotation = Quaternion.Lerp(player.transform.rotation, targetRot, moveSpeed * Time.deltaTime);
        }

        if (isGameActive && mainCamera != null && cameraViewPoint != null)
        {
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, cameraViewPoint.position, moveSpeed * Time.deltaTime);
            mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, cameraViewPoint.rotation, moveSpeed * Time.deltaTime);
        }
    }

    void StartGame()
    {
        isGameActive = true;

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
        interactionPrompt.SetActive(false);

        HidePlayerModel(true);

        ShowRolePanel();
    }

    void StopGame()
    {
        isGameActive = false;

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

        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (completionMessage != null)
            completionMessage.SetActive(false);

        interactionPrompt.SetActive(true);
    }

    void ShowRolePanel()
    {
        if (gamePanel != null) gamePanel.SetActive(true);
        if (rolePanel != null) rolePanel.SetActive(true);
        if (connectionPanel != null) connectionPanel.SetActive(false);
        if (phrasePanel != null) phrasePanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    void ShowConnectionPanel()
    {
        if (rolePanel != null) rolePanel.SetActive(false);
        if (connectionPanel != null) connectionPanel.SetActive(true);
    }

    void ShowPhrasePanel()
    {
        if (connectionPanel != null) connectionPanel.SetActive(false);
        if (phrasePanel != null) phrasePanel.SetActive(true);
        if (phraseInput != null) phraseInput.text = "";
        if (errorText != null) errorText.gameObject.SetActive(false);
    }

    void ShowResultPanel()
    {
        if (phrasePanel != null) phrasePanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(true);

        if (resultRoleText != null) resultRoleText.text = selectedRole;
        if (resultConnectionText != null) resultConnectionText.text = selectedConnection;
        if (resultPhraseText != null) resultPhraseText.text = userPhrase;
    }

    // Слушатели изменения выбора в выпадающих списках
    void OnRoleDropdownChanged(int index)
    {
        if (index >= 0 && index < roles.Length)
        {
            selectedRole = roles[index];
        }
    }

    void OnConnectionDropdownChanged(int index)
    {
        if (index >= 0 && index < connections.Length)
        {
            selectedConnection = connections[index];
        }
    }

    // Методы для кнопок "Далее" под списками
    void ConfirmRoleAndContinue()
    {
        Debug.Log($"Роль окончательно выбрана: {selectedRole}");

        ShowConnectionPanel();
    }

    void ConfirmConnectionAndContinue()
    {
        Debug.Log($"Связь окончательно выбрана: {selectedConnection}");
        ShowPhrasePanel();
    }

    public void SavePhrase()
    {
        string phrase = phraseInput.text.Trim();
        if (string.IsNullOrEmpty(phrase))
        {
            if (errorText != null)
            {
                errorText.text = "Введите фразу!";
                errorText.gameObject.SetActive(true);
                Invoke("HideError", 2f);
            }
            return;
        }
        if (phrase.Length > 100)
        {
            if (errorText != null)
            {
                errorText.text = "Фраза слишком длинная!";
                errorText.gameObject.SetActive(true);
                Invoke("HideError", 2f);
            }
            return;
        }
        userPhrase = phrase;
        Debug.Log($"Введена фраза: {userPhrase}");
        ShowResultPanel();
    }
    void HideError()
    {
        if (errorText != null)
            errorText.gameObject.SetActive(false);
    }
    public void CompletePendant()
    {
        ToggleClipboard clipboard = FindObjectOfType<ToggleClipboard>();
        if (clipboard != null)
        {
            clipboard.CompleteTask(3);
        }

        PlayerPrefs.SetString("PendantRole", selectedRole);
        PlayerPrefs.SetString("PendantConnection", selectedConnection);
        PlayerPrefs.SetString("PendantPhrase", userPhrase);
        puzzleHolder.AddPuzzle(); // Прибавить один пазл и обновить экран!
        if (pendantPrefab != null && pendantTarget != null)
        {
            GameObject pendant = Instantiate(pendantPrefab, pendantTarget.position, pendantTarget.rotation);
        }
        isCompleted = true;
        Debug.Log("Задание выполнено: Создание подвеса!");
        CloseAllPanelsAndStopGame();
    }

    private void CloseAllPanelsAndStopGame()
    {
        if (gamePanel != null) gamePanel.SetActive(false);
        if (rolePanel != null) rolePanel.SetActive(false);
        if (connectionPanel != null) connectionPanel.SetActive(false);
        if (phrasePanel != null) phrasePanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (completionMessage != null) completionMessage.SetActive(false);
        StopGame();
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
            if (promptText != null) promptText.text = "Нажмите E для создания подвеса";
        }
    }
    void OnTriggerExit(Collider other) 
    { 
        if (other.CompareTag("Player")) 
        { 
            isPlayerNear = false; 
            if (interactionPrompt != null) interactionPrompt.SetActive(false); 
            if (isGameActive) 
            { 
                StopGame(); 
            } 
            player = null; 
            playerMovement = null; 
            playerRigidbody = null; 
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
        if (playerCollider != null) playerCollider.enabled = !hide; 
    }
}