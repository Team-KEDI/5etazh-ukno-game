using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MicrophoneInteraction : MonoBehaviour
{
    [Header("Настройки")]
    public Material highlightMaterial;
    public Material defaultMaterial;

    [Header("UI элементы")]
    public GameObject textInputPanel;
    public TMP_InputField inputField;  // TMP_InputField вместо InputField
    public TextMeshProUGUI errorMessageText;  // TextMeshProUGUI вместо Text
    public TextMeshProUGUI puzzleCounterText;
    public GameObject hint;
    public TextMeshProUGUI successText;

    [Header("Эффекты")]
    public AudioClip successSound;
    private AudioSource audioSource;

    private bool isPlayerNear = false;
    private bool isCompleted = false;
    private Renderer objectRenderer;
    private GameObject player;
    private bool isOpened = false;

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        defaultMaterial = objectRenderer.material;
        audioSource = GetComponent<AudioSource>();

        // Скрываем UI при старте
        textInputPanel.SetActive(false);
        errorMessageText.gameObject.SetActive(false);
        hint.SetActive(false);
        successText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isCompleted && !isOpened && isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            OpenTextInput();
        }

        // Подсветка при наведении
        if (isPlayerNear && !isCompleted)
        {
            if (highlightMaterial != null)
                objectRenderer.material = highlightMaterial;
        }
        else
        {
            objectRenderer.material = defaultMaterial;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isCompleted)
        {
            isPlayerNear = true;
            player = other.gameObject;
            hint.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            player = null;
            hint.SetActive(false);
        }
    }

    void OpenTextInput()
    {
        textInputPanel.SetActive(true);
        inputField.text = "";
        errorMessageText.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isOpened = true;

        // Отключаем движение игрока
        if (player != null)
        {
            player.GetComponent<PlayerMovement>().canMove = false;
        }
    }

    public void SavePhrase()
    {
        string phrase = inputField.text.Trim();

        // Проверка на пустую строку
        if (string.IsNullOrEmpty(phrase))
        {
            ShowError("Введите фразу!");
            return;
        }

        // Проверка длины
        if (phrase.Length > 150)
        {
            ShowError("Фраза слишком длинная! (макс. 150 символов)");
            return;
        }

        // Сохраняем фразу
        PlayerPrefs.SetString("MascotPhrase", phrase);
        PlayerPrefs.Save();

        // Отмечаем задание выполненным
        PlayerPrefs.SetInt("PodcastQuestCompleted", 1);

        // Закрываем UI
        textInputPanel.SetActive(false);

        // Возвращаем курсор
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Включаем движение игрока
        if (player != null)
        {
            player.GetComponent<PlayerMovement>().canMove = true;
        }

        // Проигрываем звук успеха
        if (successSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(successSound);
        }

        // Показываем сообщение
        ShowCompletionMessage();

        // Обновляем счетчик пазлов
        UpdatePuzzleCounter();

        isCompleted = true;
    }

    public void ClosePanel()
    {
        textInputPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (player != null)
        {
            player.GetComponent<PlayerMovement>().canMove = true;
        }
    }

    void ShowError(string message)
    {
        errorMessageText.text = message;
        errorMessageText.gameObject.SetActive(true);
        Invoke("HideError", 2f);
    }

    void HideError()
    {
        errorMessageText.gameObject.SetActive(false);
    }

    void ShowCompletionMessage()
    {
        successText.text = "Задание выполнено: Подкастерская!\nПолучен фрагмент пазла!";
        successText.gameObject.SetActive(true);

        Invoke("HideNotification", 3f);
        
    }

    void UpdatePuzzleCounter()
    {
        if (puzzleCounterText != null)
        {
            int completedQuests = PlayerPrefs.GetInt("PodcastQuestCompleted", 0);
            puzzleCounterText.text = "Пазлы: " + completedQuests;
        }
    }

    void HideNotification()
    {
        successText.gameObject.SetActive(false);
    }
}
