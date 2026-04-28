using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PuzzleBoard : MonoBehaviour
{
    [Header("Настройки пазла")]
    public GameObject[] allComponents; // Все элементы для размещения
    public ComponentSlot[] allSlots; // Все слоты на плате

    [Header("UI")]
    public TextMeshProUGUI completionText;
    public GameObject completionMessage;
    public AudioClip successSound;

    [Header("Прогресс")]
    public int requiredComponents; // Сколько нужно разместить
    private int placedComponents = 0;
    private bool isCompleted = false;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        requiredComponents = allSlots.Length;
        UpdateUI();

        if (completionMessage != null)
            completionMessage.SetActive(false);
    }

    public void CheckCompletion()
    {
        // Подсчитываем размещенные элементы
        placedComponents = 0;
        foreach (var slot in allSlots)
        {
            if (slot.isOccupied)
                placedComponents++;
        }

        UpdateUI();

        // Проверяем полностью ли собрана схема
        if (placedComponents >= requiredComponents && !isCompleted)
        {
            CompletePuzzle();
        }
    }

    void CompletePuzzle()
    {
        isCompleted = true;
        Debug.Log("Схема собрана!");

        // Сохраняем прогресс
        PlayerPrefs.SetInt("PuzzleCompleted", 1);
        PlayerPrefs.Save();

        // Показываем сообщение
        if (completionMessage != null)
            completionMessage.SetActive(true);

        // Звук успеха
        if (successSound != null && audioSource != null)
            audioSource.PlayOneShot(successSound);

        // Можно добавить получение фрагмента пазла
        if (completionText != null)
            completionText.text = "Схема собрана! Задание выполнено!";
    }

    void UpdateUI()
    {
        if (completionText != null)
            completionText.text = $"Прогресс: {placedComponents}/{requiredComponents}";
    }

    // Сброс пазла (если нужно начать заново)
    public void ResetPuzzle()
    {
        isCompleted = false;
        placedComponents = 0;

        foreach (var slot in allSlots)
        {
            slot.ClearSlot();
        }

        foreach (var component in allComponents)
        {
            DraggableComponent comp = component.GetComponent<DraggableComponent>();
            if (comp != null)
            {
                comp.isPlaced = false;
                comp.ReturnToOriginalPosition();
                CanvasGroup cg = component.GetComponent<CanvasGroup>();
                if (cg != null)
                    cg.blocksRaycasts = true;
            }
        }

        UpdateUI();
        if (completionMessage != null)
            completionMessage.SetActive(false);
    }
}