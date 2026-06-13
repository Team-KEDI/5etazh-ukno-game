using UnityEngine;
using TMPro;

public class PuzzleHolder : MonoBehaviour
{
    [Header("UI Текст")]
    public TextMeshProUGUI puzzleText; // Перетащите сюда ваш текст "Пазлы: 0"

    [Header("Настройки сброса")]
    [SerializeField] private bool resetOnStart = true; // Если true, пазлы обнуляются при перезапуске игры

    // Текущее количество пазлов в текущей сессии игры
    public int CurrentPuzzles { get; private set; }

    void Start()
    {
        // Проверяем, нужно ли сбросить счетчик при старте игры
        if (resetOnStart)
        {
            PlayerPrefs.DeleteKey("CollectedPuzzles");
            PlayerPrefs.Save();
            CurrentPuzzles = 0;
        }
        else
        {
            // Если сброс не нужен, загружаем старое сохранение
            CurrentPuzzles = PlayerPrefs.GetInt("CollectedPuzzles", 0);
        }

        UpdateVisuals();
    }

    // ТА САМАЯ ФУНКЦИЯ ДЛЯ ВЫЗОВА В ОДНУ СТРОЧКУ
    public void AddPuzzle()
    {
        CurrentPuzzles++; // Прибавляем 1

        // Сохраняем на случай, если игра вылетит
        PlayerPrefs.SetInt("CollectedPuzzles", CurrentPuzzles);
        PlayerPrefs.Save();

        UpdateVisuals(); // Сразу обновляем текст на экране
        Debug.Log($"Пазл добавлен! Текущий счет: {CurrentPuzzles}");
    }

    // Метод обновления UI текста
    private void UpdateVisuals()
    {
        if (puzzleText != null)
        {
            puzzleText.text = $"Собрано пазлов: {CurrentPuzzles}";
        }
    }
}
