using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CardSystem : MonoBehaviour
{
    [Header("Карточки")]
    public Card3D[] allCards;

    [Header("Позиции")]
    public Transform playerViewPoint;
    public Transform cameraViewPoint;

    [Header("UI")]
    public GameObject winPanel;
    public Text progressText;

    [Header("Настройки")]
    public float moveSpeed = 5f;

    [Header("Подсказка")]
    public GameObject interactionPrompt;

    private bool isViewingWall = false;
    private bool isCardFloating = false;
    private Card3D currentFloatingCard;
    private int floatingSlotIndex = -1;

    // Основные данные
    private Vector3[] slotPositions;
    private Quaternion[] slotRotations;
    private int[] expectedYears;

    // Текущее расположение карточек (индекс карточки -> индекс слота)
    private Dictionary<Card3D, int> cardToSlot;

    private Vector3 originalPlayerPos;
    private Quaternion originalPlayerRot;
    private Vector3 originalCameraPos;
    private Quaternion originalCameraRot;

    private GameObject player;
    private PlayerMovement playerMovement;
    private Rigidbody playerRigidbody;
    private Camera mainCamera;
    private MonoBehaviour cameraController;

    private bool isPlayerNear = false;
    private int correctCount = 0;

    void Start()
    {
        mainCamera = Camera.main;

        if (mainCamera != null)
        {
            cameraController = mainCamera.GetComponent<MonoBehaviour>();
            if (cameraController == null)
                cameraController = mainCamera.GetComponent("MouseLook") as MonoBehaviour;
        }

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
        if (winPanel != null)
            winPanel.SetActive(false);

        InitializeData();
    }

    void InitializeData()
    {
        // Запоминаем позиции слотов (по текущим позициям карточек)
        slotPositions = new Vector3[allCards.Length];
        slotRotations = new Quaternion[allCards.Length];
        expectedYears = new int[allCards.Length];
        cardToSlot = new Dictionary<Card3D, int>();

        for (int i = 0; i < allCards.Length; i++)
        {
            if (allCards[i] != null)
            {
                slotPositions[i] = allCards[i].transform.position;
                slotRotations[i] = allCards[i].transform.rotation;
                expectedYears[i] = allCards[i].GetYear();
                cardToSlot[allCards[i]] = i;
                allCards[i].SetOriginalPosition(slotPositions[i], slotRotations[i]);
                allCards[i].SetExpectedYear(expectedYears[i]);
                allCards[i].SetCurrentSlotIndex(i);
            }
        }
    }

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.E) && !isViewingWall)
        {
            StartViewingWall();
        }

        if (isViewingWall && Input.GetKeyDown(KeyCode.Escape))
        {
            if (isCardFloating)
                ReturnFloatingCard();
            else
                StopViewingWall();
        }

        if (isCardFloating && Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (!Physics.Raycast(ray, out hit) || hit.collider.GetComponent<Card3D>() == null)
            {
                ReturnFloatingCard();
            }
        }

        if (isViewingWall && player != null && playerViewPoint != null)
        {
            Vector3 targetPos = player.transform.position;
            targetPos.x = Mathf.Lerp(targetPos.x, playerViewPoint.position.x, moveSpeed * Time.deltaTime);
            targetPos.z = Mathf.Lerp(targetPos.z, playerViewPoint.position.z, moveSpeed * Time.deltaTime);
            player.transform.position = targetPos;
        }

        if (isViewingWall && mainCamera != null && cameraViewPoint != null)
        {
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, cameraViewPoint.position, moveSpeed * Time.deltaTime);
            mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, cameraViewPoint.rotation, moveSpeed * Time.deltaTime);
        }
    }

    void StartViewingWall()
    {
        isViewingWall = true;

        if (player != null)
        {
            originalPlayerPos = player.transform.position;
            originalPlayerRot = player.transform.rotation;
        }

        if (mainCamera != null)
        {
            originalCameraPos = mainCamera.transform.position;
            originalCameraRot = mainCamera.transform.rotation;
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = true;
            playerRigidbody.velocity = Vector3.zero;
        }

        if (playerMovement != null)
            playerMovement.canMove = false;

        if (cameraController != null)
            cameraController.enabled = false;

        ShuffleCards();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        HidePlayerModel(true);
        UpdateProgress();
    }

    void StopViewingWall()
    {
        if (isCardFloating)
            ReturnFloatingCard();

        isViewingWall = false;

        ResetCardsToOriginalPositions();

        if (player != null)
        {
            player.transform.position = originalPlayerPos;
            player.transform.rotation = originalPlayerRot;

            if (playerRigidbody != null)
            {
                playerRigidbody.velocity = Vector3.zero;
                playerRigidbody.isKinematic = false;
            }
        }

        if (mainCamera != null)
        {
            mainCamera.transform.position = originalCameraPos;
            mainCamera.transform.rotation = originalCameraRot;
        }

        if (playerMovement != null)
            playerMovement.canMove = true;

        if (cameraController != null)
            cameraController.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        HidePlayerModel(false);

        if (winPanel != null)
            winPanel.SetActive(false);
    }

    void ResetCardsToOriginalPositions()
    {
        for (int i = 0; i < allCards.Length; i++)
        {
            if (allCards[i] != null)
            {
                allCards[i].transform.position = slotPositions[i];
                allCards[i].transform.rotation = slotRotations[i];
                cardToSlot[allCards[i]] = i;
                allCards[i].SetCurrentSlotIndex(i);
                allCards[i].SetCorrect(false);
                allCards[i].GetComponent<Renderer>().material.color = Color.white;
                allCards[i].StopAllAnimations();
            }
        }
        correctCount = 0;
    }

    void ShuffleCards()
    {
        // Создаем список индексов слотов и перемешиваем
        List<int> slotIndices = new List<int>();
        for (int i = 0; i < allCards.Length; i++) slotIndices.Add(i);

        for (int i = 0; i < allCards.Length; i++)
        {
            int randomIndex = Random.Range(i, slotIndices.Count);
            int targetSlot = slotIndices[randomIndex];
            slotIndices[randomIndex] = slotIndices[i];
            slotIndices[i] = targetSlot;
        }

        // Расставляем карточки по перемешанным слотам
        for (int i = 0; i < allCards.Length; i++)
        {
            int targetSlot = slotIndices[i];
            allCards[i].transform.position = slotPositions[targetSlot];
            allCards[i].transform.rotation = slotRotations[targetSlot];
            cardToSlot[allCards[i]] = targetSlot;
            allCards[i].SetCurrentSlotIndex(targetSlot);
            allCards[i].SetCorrect(false);
            allCards[i].GetComponent<Renderer>().material.color = Color.white;
        }

        correctCount = 0;
    }

    public void OnCardClicked(Card3D clickedCard)
    {
        if (!isViewingWall) return;
        if (clickedCard.IsInCorrectPlace()) return;

        if (!isCardFloating)
        {
            // Берем карточку
            currentFloatingCard = clickedCard;
            floatingSlotIndex = cardToSlot[clickedCard];
            isCardFloating = true;

            // Убираем карточку из словаря
            cardToSlot.Remove(clickedCard);

            clickedCard.FlyToCamera(mainCamera.transform.position, mainCamera.transform.forward);
        }
        else
        {
            if (clickedCard == currentFloatingCard) return;
            if (clickedCard.IsInCorrectPlace()) return;

            int targetSlotIndex = cardToSlot[clickedCard];

            // Сохраняем позицию кликнутой карточки
            Vector3 targetPos = clickedCard.transform.position;
            Quaternion targetRot = clickedCard.transform.rotation;

            // Отправляем летающую карточку на место кликнутой
            currentFloatingCard.FlyToPosition(targetPos, targetRot);
            currentFloatingCard.SetCurrentSlotIndex(targetSlotIndex);

            // Обновляем словарь: летающая карточка теперь на месте кликнутой
            cardToSlot[currentFloatingCard] = targetSlotIndex;

            // Отправляем кликнутую карточку к камере (она становится новой летающей)
            clickedCard.FlyToCamera(mainCamera.transform.position, mainCamera.transform.forward);

            // Обновляем текущую летающую
            currentFloatingCard = clickedCard;
            floatingSlotIndex = targetSlotIndex;

            // Убираем новую летающую из словаря (она в полете)
            cardToSlot.Remove(clickedCard);

            Invoke("CheckAllCards", 0.3f);
        }
    }

    void ReturnFloatingCard()
    {
        if (currentFloatingCard != null)
        {
            // Ищем свободный слот (которого нет в словаре)
            int emptySlotIndex = -1;
            for (int i = 0; i < allCards.Length; i++)
            {
                if (!cardToSlot.ContainsValue(i))
                {
                    emptySlotIndex = i;
                    break;
                }
            }

            if (emptySlotIndex != -1)
            {
                Vector3 targetPos = slotPositions[emptySlotIndex];
                Quaternion targetRot = slotRotations[emptySlotIndex];

                currentFloatingCard.FlyToPosition(targetPos, targetRot);
                currentFloatingCard.SetCurrentSlotIndex(emptySlotIndex);

                // Возвращаем карточку в словарь
                cardToSlot[currentFloatingCard] = emptySlotIndex;
            }

            currentFloatingCard = null;
        }
        isCardFloating = false;

        CheckAllCards();
    }

    void CheckAllCards()
    {
        correctCount = 0;

        foreach (var kvp in cardToSlot)
        {
            Card3D card = kvp.Key;
            int slotIndex = kvp.Value;

            bool isCorrect = card.GetYear() == expectedYears[slotIndex];
            card.SetCorrect(isCorrect);

            if (isCorrect)
                correctCount++;
        }

        UpdateProgress();

        if (correctCount >= allCards.Length)
        {
            WinGame();
        }
    }

    void UpdateProgress()
    {
        if (progressText != null)
        {
            progressText.text = $"Пазл: {correctCount}/{allCards.Length}";
        }
    }

    void WinGame()
    {
        Debug.Log("Победа! Все карточки на своих местах!");

        if (winPanel != null)
            winPanel.SetActive(true);

        PlayerPrefs.SetInt("CardPuzzleCompleted", 1);
        PlayerPrefs.Save();

        Invoke("StopViewingWall", 3f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            player = other.gameObject;
            playerMovement = player.GetComponent<PlayerMovement>();
            playerRigidbody = player.GetComponent<Rigidbody>();

            if (interactionPrompt != null)
                interactionPrompt.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;

            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);

            if (isViewingWall)
            {
                StopViewingWall();
            }

            player = null;
            playerMovement = null;
            playerRigidbody = null;
        }
    }

    void HidePlayerModel(bool hide)
    {
        if (player == null) return;

        Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer.GetComponent<Camera>() != null) continue;
            renderer.enabled = !hide;
        }

        Collider playerCollider = player.GetComponent<Collider>();
        if (playerCollider != null)
            playerCollider.enabled = !hide;
    }
}

//using UnityEngine;

//public class CardSystem : MonoBehaviour
//{
//    [Header("Карточки")]
//    public Card3D[] allCards;

//    [Header("Позиции")]
//    public Transform playerViewPoint;
//    public Transform cameraViewPoint;

//    [Header("Настройки")]
//    public float moveSpeed = 5f;

//    [Header("Подсказка")]
//    public GameObject interactionPrompt;

//    private bool isViewingWall = false;
//    private bool isCardFloating = false;
//    private Card3D currentCard; // Текущая летающая карточка

//    private Vector3 originalPlayerPos;
//    private Quaternion originalPlayerRot;
//    private Vector3 originalCameraPos;
//    private Quaternion originalCameraRot;

//    private GameObject player;
//    private PlayerMovement playerMovement;
//    private Rigidbody playerRigidbody;
//    private Camera mainCamera;
//    private MonoBehaviour cameraController;

//    private bool isPlayerNear = false;

//    void Start()
//    {
//        mainCamera = Camera.main;

//        if (mainCamera != null)
//        {
//            cameraController = mainCamera.GetComponent<MonoBehaviour>();
//            if (cameraController == null)
//                cameraController = mainCamera.GetComponent("MouseLook") as MonoBehaviour;
//            if (cameraController == null)
//                cameraController = mainCamera.GetComponent("FirstPersonCamera") as MonoBehaviour;
//        }

//        if (interactionPrompt != null)
//            interactionPrompt.SetActive(false);
//    }

//    void Update()
//    {
//        if (isPlayerNear && Input.GetKeyDown(KeyCode.E) && !isViewingWall)
//        {
//            StartViewingWall();
//        }

//        if (isViewingWall && Input.GetKeyDown(KeyCode.Escape))
//        {
//            if (isCardFloating)
//                ReturnFloatingCard();
//            else
//                StopViewingWall();
//        }

//        // Клик по пустому пространству - возвращаем карточку
//        if (isCardFloating && Input.GetMouseButtonDown(0))
//        {
//            RaycastHit hit;
//            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

//            if (!Physics.Raycast(ray, out hit) || hit.collider.GetComponent<Card3D>() == null)
//            {
//                ReturnFloatingCard();
//            }
//        }

//        // Плавное перемещение игрока
//        if (isViewingWall && player != null && playerViewPoint != null)
//        {
//            Vector3 targetPos = player.transform.position;
//            targetPos.x = Mathf.Lerp(targetPos.x, playerViewPoint.position.x, moveSpeed * Time.deltaTime);
//            targetPos.z = Mathf.Lerp(targetPos.z, playerViewPoint.position.z, moveSpeed * Time.deltaTime);
//            player.transform.position = targetPos;
//        }

//        // Плавное перемещение камеры
//        if (isViewingWall && mainCamera != null && cameraViewPoint != null)
//        {
//            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, cameraViewPoint.position, moveSpeed * Time.deltaTime);
//            mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, cameraViewPoint.rotation, moveSpeed * Time.deltaTime);
//        }
//    }

//    void OnTriggerEnter(Collider other)
//    {
//        if (other.CompareTag("Player"))
//        {
//            isPlayerNear = true;
//            player = other.gameObject;
//            playerMovement = player.GetComponent<PlayerMovement>();
//            playerRigidbody = player.GetComponent<Rigidbody>();

//            if (interactionPrompt != null)
//                interactionPrompt.SetActive(true);
//        }
//    }

//    void OnTriggerExit(Collider other)
//    {
//        if (other.CompareTag("Player"))
//        {
//            isPlayerNear = false;

//            if (interactionPrompt != null)
//                interactionPrompt.SetActive(false);

//            if (isViewingWall)
//            {
//                StopViewingWall();
//            }

//            player = null;
//            playerMovement = null;
//            playerRigidbody = null;
//        }
//    }

//    void StartViewingWall()
//    {
//        isViewingWall = true;
//        interactionPrompt.SetActive(!isViewingWall);

//        if (player != null)
//        {
//            originalPlayerPos = player.transform.position;
//            originalPlayerRot = player.transform.rotation;
//        }

//        if (mainCamera != null)
//        {
//            originalCameraPos = mainCamera.transform.position;
//            originalCameraRot = mainCamera.transform.rotation;
//        }

//        if (playerRigidbody != null)
//        {
//            playerRigidbody.velocity = Vector3.zero;
//            playerRigidbody.isKinematic = true;

//        }

//        if (playerMovement != null)
//            playerMovement.canMove = false;

//        if (cameraController != null)
//            cameraController.enabled = false;

//        Cursor.lockState = CursorLockMode.None;
//        Cursor.visible = true;

//        HidePlayerModel(true);
//    }

//    void StopViewingWall()
//    {
//        // Возвращаем карточку если она летает
//        if (isCardFloating)
//            ReturnFloatingCard();

//        isViewingWall = false;

//        if (player != null)
//        {
//            player.transform.position = originalPlayerPos;
//            player.transform.rotation = originalPlayerRot;

//            if (playerRigidbody != null)
//            {
//                playerRigidbody.velocity = Vector3.zero;
//                playerRigidbody.isKinematic = false;
//            }
//        }

//        if (mainCamera != null)
//        {
//            mainCamera.transform.position = originalCameraPos;
//            mainCamera.transform.rotation = originalCameraRot;
//        }

//        if (playerMovement != null)
//            playerMovement.canMove = true;

//        if (cameraController != null)
//            cameraController.enabled = true;

//        Cursor.lockState = CursorLockMode.Locked;
//        Cursor.visible = false;

//        HidePlayerModel(false);
//    }

//    void HidePlayerModel(bool hide)
//    {
//        if (player == null) return;

//        Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
//        foreach (Renderer renderer in renderers)
//        {
//            if (renderer.GetComponent<Camera>() != null) continue;
//            renderer.enabled = !hide;
//        }

//        Collider playerCollider = player.GetComponent<Collider>();
//        if (playerCollider != null)
//            playerCollider.enabled = !hide;
//    }

//    public void ShowCard(Card3D card)
//    {
//        if (!isViewingWall) return;

//        // НЕЛЬЗЯ взять новую карточку, если какая-то уже летает
//        if (isCardFloating)
//        {
//            Debug.Log("Сначала верните текущую карточку!");
//            return;
//        }

//        currentCard = card;
//        isCardFloating = true;

//        card.FlyToCamera(mainCamera.transform.position, mainCamera.transform.forward);
//    }

//    void ReturnFloatingCard()
//    {
//        if (currentCard != null)
//        {
//            currentCard.ReturnToOriginal();
//            currentCard = null;
//        }
//        isCardFloating = false;
//    }

//    public bool IsCardFloating()
//    {
//        return isCardFloating;
//    }
//}