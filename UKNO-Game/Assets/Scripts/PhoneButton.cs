using UnityEngine;

public class PhoneButton : MonoBehaviour
{
    [Header("Настройки кнопки")]
    public string digit; // Цифра/символ на кнопке (0-9, *, #)
    public int buttonIndex;

    [Header("Визуальные эффекты")]
    public Color normalColor = Color.white;
    public Color pressedColor = Color.gray;
    public AudioClip clickSound;

    [Header("Фоновый звук (Белый шум)")]
    [Tooltip("Звук непрерывного гудка телефонной трубки")]
    public AudioClip dialToneSound;

    private Renderer buttonRenderer;
    private Material originalMaterial;
    private PhoneSystem phoneSystem;
    private AudioSource backgroundAudioSource;
    private bool isDailingStarted = false;

    void Start()
    {
        buttonRenderer = GetComponent<Renderer>();
        if (buttonRenderer != null)
            originalMaterial = buttonRenderer.material;

        phoneSystem = FindObjectOfType<PhoneSystem>();

        // Создаем локальный источник звука для белого шума на этой кнопке
        backgroundAudioSource = GetComponent<AudioSource>();
        if (backgroundAudioSource == null)
        {
            backgroundAudioSource = gameObject.AddComponent<AudioSource>();
        }
        backgroundAudioSource.playOnAwake = false;
        backgroundAudioSource.spatialBlend = 1f; // 3D звук для пространства УКНО
        backgroundAudioSource.maxDistance = 5f;
    }

    void Update()
    {
        if (phoneSystem == null) return;

        // СОСТОЯНИЕ 1: Игрок начал игру на телефоне по кнопке E
        if (phoneSystem.IsGameActive() && !backgroundAudioSource.isPlaying && !isDailingStarted && !IsAnyAudioPlayingInScene())
        {
            backgroundAudioSource.clip = dialToneSound;
            backgroundAudioSource.loop = true; // Белый шум идет циклично
            backgroundAudioSource.Play();
            Debug.Log("[Телефон]: Игрок сел за телефон. Включен белый шум линии.");
        }

        // СОСТОЯНИЕ 3: Номер отзвучал (в сцене наступила тишина), возвращаем белый шум трубки обратно
        if (phoneSystem.IsGameActive() && !backgroundAudioSource.isPlaying && isDailingStarted && !IsAnyAudioPlayingInScene())
        {
            isDailingStarted = false; // Сбрасываем флаг набора, возвращаем шум
            backgroundAudioSource.clip = dialToneSound;
            backgroundAudioSource.loop = true;
            backgroundAudioSource.Play();
            Debug.Log("[Телефон]: Воспроизведение записи завершено. Белый шум возвращен в трубку.");
        }

        // Если игрок принудительно вышел из игры (нажал Escape) — полностью глушим аппарат
        if (!phoneSystem.IsGameActive() && backgroundAudioSource.isPlaying)
        {
            backgroundAudioSource.Stop();
            isDailingStarted = false;
            Debug.Log("[Телефон]: Игрок встал из-за стола. Шум выключен.");
        }
    }

    void OnMouseDown()
    {
        if (phoneSystem == null) return;
        if (!phoneSystem.IsGameActive()) return;

        // Визуальный эффект нажатия
        if (buttonRenderer != null)
            buttonRenderer.material.color = pressedColor;

        // Обычный пикающий звук нажатия кнопки телефона
        if (clickSound != null)
            AudioSource.PlayClipAtPoint(clickSound, transform.position);

        // Передаем нажатую цифру в систему телефона
        phoneSystem.OnButtonPressed(digit);

        // СОСТОЯНИЕ 2: Как только игрок начал нажимать кнопки набора, выключаем фоновый белый шум
        if (backgroundAudioSource != null && backgroundAudioSource.isPlaying)
        {
            backgroundAudioSource.Stop();
            isDailingStarted = true; // Выставляем флаг, что идет процесс набора/ожидания ответа
            Debug.Log("[Телефон]: Нажат номер. Белый шум временно отключен.");
        }

        Invoke("ResetColor", 0.1f);
    }

    void ResetColor()
    {
        if (buttonRenderer != null)
            buttonRenderer.material.color = normalColor;
    }

    // Служебный метод, проверяющий, играет ли сейчас какой-либо звук ответа/ошибки на сцене
    private bool IsAnyAudioPlayingInScene()
    {
        AudioSource[] sources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource source in sources)
        {
            // Игнорируем собственный источник белого шума, проверяем только чужие (музыку/ответы)
            if (source != backgroundAudioSource && source.isPlaying && source.spatialBlend > 0.5f)
                return true;
        }
        return false;
    }
}
