using UnityEngine;

public class CardObject : MonoBehaviour
{
    public int year;
    public string eventTitle;
    public string eventDescription;

    public GameObject frontText; // текст с годом
    public GameObject backText;  // панель с информацией

    private bool isFlipped = false;
    private Quaternion targetRotation;
    private bool isAnimating = false;

    void Start()
    {
        // Устанавливаем текст
        frontText.GetComponent<TextMesh>().text = year.ToString();
        backText.GetComponentInChildren<TextMesh>().text = $"{eventTitle}\n{eventDescription}";

        frontText.SetActive(true);
        backText.SetActive(false);
    }

    void OnMouseDown()
    {
        if (isAnimating) return;
        isAnimating = true;

        isFlipped = !isFlipped;
        targetRotation = transform.rotation * Quaternion.Euler(0, 180, 0);
        
    }

    void Update()
    {
        if (isAnimating)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 360 * Time.deltaTime);

            if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
            {
                isAnimating = false;
                // После поворота переключаем видимость текста
                frontText.SetActive(!isFlipped);
                backText.SetActive(isFlipped);
            }
        }
    }

    public void ResetCard()
    {
        if (!isFlipped) return;

        isFlipped = false;
        targetRotation = Quaternion.identity;
        isAnimating = true;
    }
}