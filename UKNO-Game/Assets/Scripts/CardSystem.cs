using UnityEngine;

public class CardSystem : MonoBehaviour
{
    [Header("Карточки")]
    public Card3D[] allCards;

    [Header("Позиции")]
    public Transform playerViewPoint;
    public Transform cameraViewPoint;

    [Header("Настройки")]
    public float moveSpeed = 5f;

    [Header("Подсказка")]
    public GameObject interactionPrompt;

    private bool isViewingWall = false;
    private bool isCardFloating = false;
    private Card3D currentCard; // Текущая летающая карточка

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

    void Start()
    {
        mainCamera = Camera.main;

        if (mainCamera != null)
        {
            cameraController = mainCamera.GetComponent<MonoBehaviour>();
            if (cameraController == null)
                cameraController = mainCamera.GetComponent("MouseLook") as MonoBehaviour;
            if (cameraController == null)
                cameraController = mainCamera.GetComponent("FirstPersonCamera") as MonoBehaviour;
        }

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
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

        // Клик по пустому пространству - возвращаем карточку
        if (isCardFloating && Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (!Physics.Raycast(ray, out hit) || hit.collider.GetComponent<Card3D>() == null)
            {
                ReturnFloatingCard();
            }
        }

        // Плавное перемещение игрока
        if (isViewingWall && player != null && playerViewPoint != null)
        {
            Vector3 targetPos = player.transform.position;
            targetPos.x = Mathf.Lerp(targetPos.x, playerViewPoint.position.x, moveSpeed * Time.deltaTime);
            targetPos.z = Mathf.Lerp(targetPos.z, playerViewPoint.position.z, moveSpeed * Time.deltaTime);
            player.transform.position = targetPos;
        }

        // Плавное перемещение камеры
        if (isViewingWall && mainCamera != null && cameraViewPoint != null)
        {
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, cameraViewPoint.position, moveSpeed * Time.deltaTime);
            mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, cameraViewPoint.rotation, moveSpeed * Time.deltaTime);
        }
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

    void StartViewingWall()
    {
        isViewingWall = true;
        interactionPrompt.SetActive(!isViewingWall);

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
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.isKinematic = true;
            
        }

        if (playerMovement != null)
            playerMovement.canMove = false;

        if (cameraController != null)
            cameraController.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        HidePlayerModel(true);
    }

    void StopViewingWall()
    {
        // Возвращаем карточку если она летает
        if (isCardFloating)
            ReturnFloatingCard();

        isViewingWall = false;

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

    public void ShowCard(Card3D card)
    {
        if (!isViewingWall) return;

        // НЕЛЬЗЯ взять новую карточку, если какая-то уже летает
        if (isCardFloating)
        {
            Debug.Log("Сначала верните текущую карточку!");
            return;
        }

        currentCard = card;
        isCardFloating = true;

        card.FlyToCamera(mainCamera.transform.position, mainCamera.transform.forward);
    }

    void ReturnFloatingCard()
    {
        if (currentCard != null)
        {
            currentCard.ReturnToOriginal();
            currentCard = null;
        }
        isCardFloating = false;
    }

    public bool IsCardFloating()
    {
        return isCardFloating;
    }
}