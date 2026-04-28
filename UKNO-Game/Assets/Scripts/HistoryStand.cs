using UnityEngine;

public class HistoryStand : MonoBehaviour
{
    [Header("Контейнер с карточками")]
    public GameObject cardsContainer;

    [Header("Подсказка")]
    public GameObject interactionPrompt;

    private bool isPlayerNear = false;
    private GameObject currentPlayer;
    private PlayerMovement playerMovement;

    void Start()
    {
        if (cardsContainer != null)
            cardsContainer.SetActive(false);

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            ToggleCards();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            currentPlayer = other.gameObject;
            playerMovement = currentPlayer.GetComponent<PlayerMovement>();

            if (interactionPrompt != null)
                interactionPrompt.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            currentPlayer = null;
            playerMovement = null;

            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);

            if (cardsContainer != null && cardsContainer.activeSelf)
                CloseCards();
        }
    }

    void ToggleCards()
    {
        if (cardsContainer.activeSelf)
            CloseCards();
        else
            OpenCards();
    }

    void OpenCards()
    {
        cardsContainer.SetActive(true);

        if (playerMovement != null)
            playerMovement.canMove = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseCards()
    {
        cardsContainer.SetActive(false);

        // Сбрасываем все карточки
        foreach (var card in cardsContainer.GetComponentsInChildren<CardObject>())
        {
            card.ResetCard();
        }

        if (playerMovement != null)
            playerMovement.canMove = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}