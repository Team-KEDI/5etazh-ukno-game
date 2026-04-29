using UnityEngine;
using TMPro; // если используете TextMeshPro, иначе используйте UnityEngine.UI.Text

public class GetMap : MonoBehaviour
{
    [Header("Что происходит при старте")]
    public GameObject objectToRemove;     // объект, который исчезнет (например, камень, заслон)
    public GameObject[] wallsToDisable;   // невидимые стены или другие препятствия

    [Header("UI")]
    public TextMeshProUGUI hintText;      // текстовое поле для подсказки (можно в инспекторе)
    public string hintMessage = "Нажмите E, чтобы начать";

    [Header("Звук")]
    public AudioClip startSound;

    private bool playerInRange = false;
    private bool activated = false;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && startSound != null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Скрываем подсказку при старте, если она привязана
        if (hintText != null)
            hintText.text = "";
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !activated)
        {
            playerInRange = true;
            if (hintText != null)
                hintText.text = hintMessage;
            else
                Debug.Log(hintMessage);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (hintText != null)
                hintText.text = "";
        }
    }

    void Update()
    {
        if (playerInRange && !activated && Input.GetKeyDown(KeyCode.E))
        {
            ActivateStart();
        }
    }

    void ActivateStart()
    {
        activated = true;

        // Удаляем объект
        if (objectToRemove != null)
            Destroy(objectToRemove);

        // Отключаем стены
        foreach (GameObject wall in wallsToDisable)
        {
            if (wall != null)
                wall.SetActive(false);
        }

        // Проигрываем звук
        if (startSound != null && audioSource != null)
            audioSource.PlayOneShot(startSound);

        // Убираем подсказку
        if (hintText != null)
            hintText.text = "";

        // Отключаем триггер, чтобы повторно не сработал
        GetComponent<Collider>().enabled = false;

        Debug.Log("Старт активирован: проход открыт");
    }
}