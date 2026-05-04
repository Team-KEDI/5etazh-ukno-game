using UnityEngine;

public class Card3D : MonoBehaviour
{
    [Header("Данные карточки")]
    public int year;
    public string eventTitle;
    [TextArea(3, 5)]
    public string eventDescription;

    [Header("Настройки анимации")]
    public float flySpeed = 5f;
    public float flipSpeed = 360f;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isFloating = false;
    private bool isFlipped = false;
    private bool isAnimating = false;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private CardSystem cardSystem;

    void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        cardSystem = FindObjectOfType<CardSystem>();
    }

    void Update()
    {
        if (isAnimating)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, flySpeed * Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, flipSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f &&
                Quaternion.Angle(transform.rotation, targetRotation) < 0.5f)
            {
                isAnimating = false;
            }
        }
    }

    void OnMouseDown()
    {
        // Не реагируем на клики если карточка уже летает ИЛИ это не наша летающая карточка
        if (cardSystem == null) return;

        // Если какая-то карточка уже летает И это не текущая карточка - игнорируем
        // Это защита от попытки взять другую карточку
        if (!isFloating && cardSystem.IsCardFloating())
        {
            Debug.Log("Сначала верните текущую карточку!");
            return;
        }

        if (!isFloating)
        {
            cardSystem.ShowCard(this);
        }
        else
        {
            FlipCard();
        }
    }

    public void FlyToCamera(Vector3 cameraPos, Vector3 cameraForward)
    {
        isFloating = true;

        targetPosition = cameraPos + cameraForward * 1.5f;
        targetRotation = Quaternion.Euler(90, 0, 0);

        isAnimating = true;
    }

    public void ReturnToOriginal()
    {
        isFloating = false;
        isFlipped = false;

        targetPosition = originalPosition;
        targetRotation = originalRotation;

        isAnimating = true;
    }

    void FlipCard()
    {
        targetRotation *= Quaternion.Euler(0, 180, 0);
        isAnimating = true;
    }

    public void ResetCard()
    {
        if (isFloating)
        {
            ReturnToOriginal();
        }
    }
}