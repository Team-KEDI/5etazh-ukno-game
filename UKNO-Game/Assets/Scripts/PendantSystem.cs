using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PendantSystem : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject gamePanel;
    public GameObject rolePanel;
    public GameObject connectionPanel;
    public GameObject phrasePanel;
    public GameObject resultPanel;

    [Header("Role Selection")]
    public Button[] roleButtons;
    public TextMeshProUGUI[] roleButtonTexts; // TMP тексты для кнопок

    [Header("Connection Selection")]
    public Button[] connectionButtons;
    public TextMeshProUGUI[] connectionButtonTexts; // TMP тексты для кнопок
    public Image[] connectionIcons;

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

        // Роли
        string[] roles = {
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

        for (int i = 0; i < roleButtons.Length && i < roles.Length; i++)
        {
            int index = i;
            roleButtonTexts[i].text = roles[i];
            roleButtons[i].onClick.AddListener(() => SelectRole(roles[index]));
        }

        // Связи
        string[] connections = {
            "не имею прямого отношения к району",
            "живу здесь более 5 лет",
            "живу здесь менее 5 лет",
            "провожу свободное время здесь",
            "работаю в Чкаловском районе",
            "родился(-ась) в Чкаловском районе"
        };

        for (int i = 0; i < connectionButtons.Length && i < connections.Length; i++)
        {
            int index = i;
            connectionButtonTexts[i].text = connections[i];
            connectionButtons[i].onClick.AddListener(() => SelectConnection(connections[index]));
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

    void SelectRole(string role)
    {
        selectedRole = role;
        Debug.Log($"Выбрана роль: {selectedRole}");
        ShowConnectionPanel();
    }

    void SelectConnection(string connection)
    {
        selectedConnection = connection;
        Debug.Log($"Выбрана связь: {selectedConnection}");
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
        PlayerPrefs.SetString("PendantRole", selectedRole);
        PlayerPrefs.SetString("PendantConnection", selectedConnection);
        PlayerPrefs.SetString("PendantPhrase", userPhrase);
        PlayerPrefs.SetInt("PendantCompleted", 1);
        PlayerPrefs.Save();

        if (pendantPrefab != null && pendantTarget != null)
        {
            GameObject pendant = Instantiate(pendantPrefab, pendantTarget.position, pendantTarget.rotation);
        }

        isCompleted = true;

        Debug.Log("Задание выполнено: Создание подвеса!");

        // Сразу закрываем панели и останавливаем игру
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

            if (promptText != null)
                promptText.text = "Нажмите E для создания подвеса";
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
        if (playerCollider != null)
            playerCollider.enabled = !hide;
    }
}