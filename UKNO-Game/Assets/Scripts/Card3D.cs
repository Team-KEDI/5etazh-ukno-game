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
    public Vector3 flyRotationOffset = new Vector3(90f, 90f, 0f); // Поворот при подлёте

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private int originalYear;
    private int currentSlotIndex = -1;
    private bool isCorrect = false;
    private bool isFloating = false;
    private bool isAnimating = false;
    private bool isFacingCamera = false; // Отслеживаем, повернута ли карточка к камере

    private CardSystem cardSystem;

    void Start()
    {
        cardSystem = FindObjectOfType<CardSystem>();
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalYear = year;
        targetPosition = originalPosition;
        targetRotation = originalRotation;
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

                // Если карточка прилетела к камере, отмечаем что она в режиме просмотра
                if (isFloating && !isFacingCamera)
                {
                    isFacingCamera = true;
                }
            }
        }
    }

    void OnMouseDown()
    {
        if (cardSystem != null)
        {
            // Если карточка летает и уже у камеры - переворачиваем
            if (isFloating && isFacingCamera)
            {
                FlipCard();
            }
            else
            {
                cardSystem.OnCardClicked(this);
            }
        }
    }

    public void FlyToCamera(Vector3 cameraPos, Vector3 cameraForward)
    {
        StopAllAnimations();
        isFloating = true;
        isFacingCamera = false;
        isAnimating = true;

        targetPosition = cameraPos + cameraForward * 1.2f;

        Quaternion baseRotation = Quaternion.LookRotation(cameraForward);
        targetRotation = baseRotation * Quaternion.Euler(flyRotationOffset);
    }

    public void FlyToPosition(Vector3 pos, Quaternion rot)
    {
        StopAllAnimations();
        isFloating = false;
        isFacingCamera = false;
        isAnimating = true;
        targetPosition = pos;
        targetRotation = rot;
    }

    public void FlipCard()
    {
        if (!isAnimating)
        {
            // Поворачиваем на 180 градусов по оси Z
            targetRotation *= Quaternion.Euler(0, 0, 180f);
            isAnimating = true;
        }
    }

    public void StopAllAnimations()
    {
        isAnimating = false;
        isFloating = false;
        isFacingCamera = false;
    }

    public void SetTargetPosition(Vector3 pos, Quaternion rot)
    {
        targetPosition = pos;
        targetRotation = rot;
    }

    public void SetOriginalPosition(Vector3 pos, Quaternion rot)
    {
        originalPosition = pos;
        originalRotation = rot;
        targetPosition = originalPosition;
        targetRotation = originalRotation;
    }

    public void SetExpectedYear(int year)
    {
        // Для совместимости
    }

    public void SetOriginalYear(int yr)
    {
        originalYear = yr;
    }

    public void SetCurrentSlotIndex(int index)
    {
        currentSlotIndex = index;
    }

    public int GetCurrentSlotIndex()
    {
        return currentSlotIndex;
    }

    public int GetYear()
    {
        return year;
    }

    public int GetOriginalYear()
    {
        return originalYear;
    }

    public bool IsInCorrectPlace()
    {
        return isCorrect;
    }

    public void SetCorrect(bool correct)
    {
        isCorrect = correct;
        GetComponent<Renderer>().material.color = correct ? Color.green : Color.white;
    }
}

//using UnityEngine;

//public class Card3D : MonoBehaviour
//{
//    [Header("Данные карточки")]
//    public int year;
//    public string eventTitle;
//    [TextArea(3, 5)]
//    public string eventDescription;

//    [Header("Настройки анимации")]
//    public float flySpeed = 5f;
//    public float flipSpeed = 360f;

//    private Vector3 originalPosition;
//    private Quaternion originalRotation;
//    private bool isFloating = false;
//    private bool isFlipped = false;
//    private bool isAnimating = false;
//    private Vector3 targetPosition;
//    private Quaternion targetRotation;

//    private CardSystem cardSystem;

//    void Start()
//    {
//        originalPosition = transform.position;
//        originalRotation = transform.rotation;
//        cardSystem = FindObjectOfType<CardSystem>();
//    }

//    void Update()
//    {
//        if (isAnimating)
//        {
//            transform.position = Vector3.MoveTowards(transform.position, targetPosition, flySpeed * Time.deltaTime);
//            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, flipSpeed * Time.deltaTime);

//            if (Vector3.Distance(transform.position, targetPosition) < 0.01f &&
//                Quaternion.Angle(transform.rotation, targetRotation) < 0.5f)
//            {
//                isAnimating = false;
//            }
//        }
//    }

//    void OnMouseDown()
//    {
//        // Не реагируем на клики если карточка уже летает ИЛИ это не наша летающая карточка
//        if (cardSystem == null) return;

//        // Если какая-то карточка уже летает И это не текущая карточка - игнорируем
//        // Это защита от попытки взять другую карточку
//        if (!isFloating && cardSystem.IsCardFloating())
//        {
//            Debug.Log("Сначала верните текущую карточку!");
//            return;
//        }

//        if (!isFloating)
//        {
//            cardSystem.ShowCard(this);
//        }
//        else
//        {
//            FlipCard();
//        }
//    }

//    public void FlyToCamera(Vector3 cameraPos, Vector3 cameraForward)
//    {
//        isFloating = true;

//        targetPosition = cameraPos + cameraForward * 1.5f;
//        targetRotation = Quaternion.Euler(90, 0, 0);

//        isAnimating = true;
//    }

//    public void ReturnToOriginal()
//    {
//        isFloating = false;
//        isFlipped = false;

//        targetPosition = originalPosition;
//        targetRotation = originalRotation;

//        isAnimating = true;
//    }

//    void FlipCard()
//    {
//        targetRotation *= Quaternion.Euler(0, 180, 0);
//        isAnimating = true;
//    }

//    public void ResetCard()
//    {
//        if (isFloating)
//        {
//            ReturnToOriginal();
//        }
//    }
//}