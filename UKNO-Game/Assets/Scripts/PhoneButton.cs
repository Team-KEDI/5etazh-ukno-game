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

    private Renderer buttonRenderer;
    private Material originalMaterial;
    private PhoneSystem phoneSystem;

    void Start()
    {
        buttonRenderer = GetComponent<Renderer>();
        if (buttonRenderer != null)
            originalMaterial = buttonRenderer.material;

        phoneSystem = FindObjectOfType<PhoneSystem>();
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

        // Передаем нажатую цифру в систему телефона
        phoneSystem.OnButtonPressed(digit);

        // Возвращаем цвет
        Invoke("ResetColor", 0.1f);
    }

    void ResetColor()
    {
        if (buttonRenderer != null)
            buttonRenderer.material.color = normalColor;
    }
}