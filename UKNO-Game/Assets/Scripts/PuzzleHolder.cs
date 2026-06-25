using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class PuzzleHolder : MonoBehaviour
{
    [Header("UI Текст счетчика")]
    public TextMeshProUGUI puzzleText;

    [Header("Настройки сброса")]
    [SerializeField] private bool resetOnStart = true;

    [Header("Финальная сцена с маскотом")]
    public GameObject finalCutscenePanel;
    public TextMeshProUGUI speechText;

    [Header("Скорость анимации")]
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("Время удержания экрана")]
    [SerializeField] private float displayDuration = 5.0f;

    [Header("Задержка перед зачислением")]
    [SerializeField] private float delayBeforeAdding = 1.0f; // Через сколько секунд после вызова прибавится пазл

    public int CurrentPuzzles { get; private set; }

    private List<Image> allImages = new List<Image>();
    private List<TextMeshProUGUI> allTexts = new List<TextMeshProUGUI>();
    private List<float> targetImageAlphas = new List<float>();
    private List<float> targetTextAlphas = new List<float>();

    void Start()
    {
        if (resetOnStart)
        {
            PlayerPrefs.DeleteKey("CollectedPuzzles");
            PlayerPrefs.Save();
            CurrentPuzzles = 0;
        }
        else
        {
            CurrentPuzzles = PlayerPrefs.GetInt("CollectedPuzzles", 0);
        }

        if (finalCutscenePanel != null)
        {
            CacheOriginalColors();
            finalCutscenePanel.SetActive(false);
        }

        UpdateVisuals();
    }

    // ТЕПЕРЬ ЭТОТ МЕТОД ЗАПУСКАЕТ КОРОТИНУ С ЗАДЕРЖКОЙ
    public void AddPuzzle()
    {
        StartCoroutine(AddPuzzleWithDelay());
    }

    private IEnumerator AddPuzzleWithDelay()
    {
        // 1. Ждем N секунд, пока закрывается интерфейс мини-игры
        yield return new WaitForSeconds(delayBeforeAdding);

        // 2. Только после ожидания прибавляем пазл и сохраняем
        CurrentPuzzles++;
        PlayerPrefs.SetInt("CollectedPuzzles", CurrentPuzzles);
        PlayerPrefs.Save();

        UpdateVisuals();
        Debug.Log($"Пазл добавлен после задержки! Текущий счет: {CurrentPuzzles}");

        // 3. Проверяем, наступил ли финал
        if (CurrentPuzzles == 8)
        {
            TriggerFinalCutscene();
        }
    }

    private void CacheOriginalColors()
    {
        foreach (Image img in finalCutscenePanel.GetComponentsInChildren<Image>(true))
        {
            allImages.Add(img);
            targetImageAlphas.Add(img.color.a);
        }
        foreach (TextMeshProUGUI txt in finalCutscenePanel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            allTexts.Add(txt);
            targetTextAlphas.Add(txt.color.a);
        }
    }

    private void TriggerFinalCutscene()
    {
        if (finalCutscenePanel != null)
        {
            string savedPhrase = PlayerPrefs.GetString("MascotPhrase", "Привет, друг!");
            if (speechText != null)
            {
                speechText.text = savedPhrase;
            }

            finalCutscenePanel.SetActive(true);
            StartCoroutine(FinalCutsceneSequence());
        }
    }

    private IEnumerator FinalCutsceneSequence()
    {
        float currentTime = 0f;

        // 1. ПОЯВЛЕНИЕ
        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            float progress = Mathf.Clamp01(currentTime / fadeDuration);
            AnimateAlpha(progress, isFadingOut: false);
            yield return null;
        }
        SetAlphaToTarget(true);

        // 2. ОЖИДАНИЕ
        yield return new WaitForSeconds(displayDuration);

        // 3. ЗАТУХАНИЕ
        currentTime = 0f;
        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            float progress = Mathf.Clamp01(currentTime / fadeDuration);
            AnimateAlpha(1f - progress, isFadingOut: true);
            yield return null;
        }
        SetAlphaToTarget(false);

        finalCutscenePanel.SetActive(false);
    }

    private void AnimateAlpha(float progress, bool isFadingOut)
    {
        for (int i = 0; i < allImages.Count; i++)
        {
            if (allImages[i] == null) continue;
            Color c = allImages[i].color;
            c.a = Mathf.Lerp(0f, targetImageAlphas[i], progress);
            allImages[i].color = c;
        }
        for (int i = 0; i < allTexts.Count; i++)
        {
            if (allTexts[i] == null) continue;
            Color c = allTexts[i].color;
            c.a = Mathf.Lerp(0f, targetTextAlphas[i], progress);
            allTexts[i].color = c;
        }
    }

    private void SetAlphaToTarget(bool maxAlpha)
    {
        for (int i = 0; i < allImages.Count; i++)
        {
            if (allImages[i] == null) continue;
            Color c = allImages[i].color;
            c.a = maxAlpha ? targetImageAlphas[i] : 0f;
            allImages[i].color = c;
        }
        for (int i = 0; i < allTexts.Count; i++)
        {
            if (allTexts[i] == null) continue;
            Color c = allTexts[i].color;
            c.a = maxAlpha ? targetTextAlphas[i] : 0f;
            allTexts[i].color = c;
        }
    }

    private void UpdateVisuals()
    {
        if (puzzleText != null)
        {
            puzzleText.text = $"Пазлы: {CurrentPuzzles}";
        }
    }
}
