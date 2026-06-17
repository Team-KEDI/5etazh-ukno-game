using UnityEngine;

public class PhoneButton : MonoBehaviour
{
    [Header("Настройки кнопки")]
    public string digit;
    public int buttonIndex;

    [Header("Визуальные эффекты")]
    public Color normalColor = Color.white;
    public Color pressedColor = Color.gray;
    public AudioClip clickSound;

    [Header("Фоновый звук (Белый шум)")]
    public AudioClip dialToneSound;

    private Renderer buttonRenderer;
    private Material originalMaterial;
    private PhoneSystem phoneSystem;
    private AudioSource backgroundAudioSource;
    private bool isDialingStarted = false;
    private bool isDialToneEnabled = false;

    void Start()
    {
        buttonRenderer = GetComponent<Renderer>();
        if (buttonRenderer != null)
            originalMaterial = buttonRenderer.material;

        phoneSystem = FindObjectOfType<PhoneSystem>();

        backgroundAudioSource = GetComponent<AudioSource>();
        if (backgroundAudioSource == null)
        {
            backgroundAudioSource = gameObject.AddComponent<AudioSource>();
        }
        backgroundAudioSource.playOnAwake = false;
        backgroundAudioSource.spatialBlend = 0f; // 2D звук для чистоты
        backgroundAudioSource.loop = true;
    }

    void Update()
    {
        if (phoneSystem == null) return;

        // Включаем белый шум при старте игры
        if (phoneSystem.IsGameActive() && !isDialToneEnabled && !isDialingStarted)
        {
            PlayDialTone();
        }

        // Если игрок вышел из игры — глушим всё
        if (!phoneSystem.IsGameActive() && backgroundAudioSource.isPlaying)
        {
            StopDialTone();
        }
    }

    void OnMouseDown()
    {
        if (phoneSystem == null) return;
        if (!phoneSystem.IsGameActive()) return;

        // Визуальный эффект нажатия
        if (buttonRenderer != null)
            buttonRenderer.material.color = pressedColor;

        // Звук нажатия
        if (clickSound != null)
            AudioSource.PlayClipAtPoint(clickSound, transform.position);

        // Выключаем белый шум при наборе
        if (backgroundAudioSource.isPlaying)
        {
            StopDialTone();
            isDialingStarted = true;
            Debug.Log("[Телефон]: Начат набор. Белый шум выключен.");
        }

        // Передаём цифру
        phoneSystem.OnButtonPressed(digit);

        Invoke("ResetColor", 0.1f);
    }

    void ResetColor()
    {
        if (buttonRenderer != null)
            buttonRenderer.material.color = normalColor;
    }

    void PlayDialTone()
    {
        if (dialToneSound == null)
        {
            Debug.LogWarning("[Телефон]: dialToneSound не назначен!");
            return;
        }

        backgroundAudioSource.clip = dialToneSound;
        backgroundAudioSource.loop = true;
        backgroundAudioSource.Play();
        isDialToneEnabled = true;
        Debug.Log("[Телефон]: Белый шум включен.");
    }

    void StopDialTone()
    {
        if (backgroundAudioSource.isPlaying)
        {
            backgroundAudioSource.Stop();
        }
        isDialToneEnabled = false;
        Debug.Log("[Телефон]: Белый шум выключен.");
    }

    // Вызывается из PhoneSystem при завершении номера
    public void OnNumberComplete()
    {
        isDialingStarted = false;
        if (phoneSystem != null && phoneSystem.IsGameActive())
        {
            // Включаем белый шум обратно после завершения номера
            if (!backgroundAudioSource.isPlaying && !isDialToneEnabled)
            {
                PlayDialTone();
            }
        }
    }
}