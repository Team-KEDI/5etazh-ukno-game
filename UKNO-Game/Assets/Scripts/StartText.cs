using UnityEngine;
using TMPro; // если используете TextMeshPro, иначе замените на обычный Text

public class GameIntro : MonoBehaviour
{
    [Header("UI элементы")]
    public GameObject introPanel;          // панель с затемнением (фон)
    public TextMeshProUGUI introText;      // текст приветствия
    public string message = "Добро пожаловать!\nНажмите E, чтобы начать.";

    [Header("Настройки")]
    public KeyCode continueKey = KeyCode.E; // клавиша для пропуска

    private bool introActive = true;

    void Start()
    {
        // Если панель не назначена, создадим её программно
        if (introPanel == null)
            CreateIntroPanel();

        // Настраиваем текст
        if (introText != null)
            introText.text = message;
        else if (introPanel != null)
        {
            // Попробуем найти компонент TextMeshProUGUI в дочерних объектах панели
            introText = introPanel.GetComponentInChildren<TextMeshProUGUI>();
            if (introText != null)
                introText.text = message;
        }

        // Блокируем курсор (если нужно, чтобы игрок не вращал камерой во время показа)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (introActive && Input.GetKeyDown(continueKey))
        {
            HideIntro();
        }
    }

    void HideIntro()
    {
        introActive = false;
        if (introPanel != null)
            introPanel.SetActive(false);

        // Возвращаем стандартное состояние курсора (обычно locked для FPS)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Уничтожаем объект или просто отключаем скрипт – по желанию
        Destroy(gameObject); // или enabled = false;
    }

    void CreateIntroPanel()
    {
        // Создаём Canvas, если его нет
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("IntroCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // Создаём панель
        GameObject panel = new GameObject("IntroPanel");
        panel.transform.SetParent(canvas.transform, false);

        UnityEngine.UI.Image img = panel.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0, 0, 0, 0.9f); // тёмный полупрозрачный

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        // Создаём текст
        GameObject textGO = new GameObject("IntroText");
        textGO.transform.SetParent(panel.transform, false);

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = message;
        tmp.fontSize = 36;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        introPanel = panel;
        introText = tmp;
    }
}