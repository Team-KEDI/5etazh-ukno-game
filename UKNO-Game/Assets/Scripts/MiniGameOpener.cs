using UnityEngine;

public class MiniGameOpener : MonoBehaviour
{
    public GameObject miniGamePanel; // Панель с мини-игрой

    private bool isPlayerNear = false;
    private GameObject player;
    private PlayerMovement playerMovement;

    void Start()
    {
        if (miniGamePanel != null)
            miniGamePanel.SetActive(false);
    }

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            OpenMiniGame();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            player = other.gameObject;
            playerMovement = player.GetComponent<PlayerMovement>();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            player = null;
            playerMovement = null;
        }
    }

    void OpenMiniGame()
    {
        if (miniGamePanel == null) return;

        // Открываем панель
        miniGamePanel.SetActive(true);

        // Отключаем движение игрока
        if (playerMovement != null)
            playerMovement.canMove = false;

        // Показываем курсор
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseMiniGame()
    {
        if (miniGamePanel == null) return;

        // Закрываем панель
        miniGamePanel.SetActive(false);

        // Включаем движение игрока
        if (playerMovement != null)
            playerMovement.canMove = true;

        // Скрываем курсор
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}