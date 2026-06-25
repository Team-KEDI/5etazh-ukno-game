using UnityEngine;
using TMPro;
using System.Collections;

public class AssemblySlotsManager : MonoBehaviour
{
    [Header("Настройки луча (Raycast)")]
    public float rayDistance = 50f;
    public LayerMask clickLayer = -1;

    [Header("UI Ссылки")]
    public GameObject assemblyUIPanel;
    public TMP_Text statusText;
    public GameObject hintPanel;
    public TMP_Text hintText;

    [Header("Финальный UI")]
    public TextMeshProUGUI successTextObject;
    public AudioSource successAudio;

    [Header("Логика")]
    public GameObject exitBlocker;
    public float moveSpeed = 10f;
    public int totalNeeded = 5;

    private int placedCount = 0;
    private GameObject selectedDetail = null;
    private int selectedID = -1;
    private Camera mainCam;
    private Coroutine hintCoroutine;
    public PuzzleHolder puzzleHolder; // Ссылка на менеджер пазлов

    void Start()
    {
        mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogWarning($"[{name}] Camera.main не найдена! Убедитесь, что у камеры на сцене установлен тег 'MainCamera'.");
        }

        if (successTextObject != null)
        {
            successTextObject.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (assemblyUIPanel == null || !assemblyUIPanel.activeSelf) return;
        if (Input.GetMouseButtonDown(0)) ShootRay();
    }

    void ShootRay()
    {
        if (mainCam == null)
        {
            mainCam = Camera.main;
            if (mainCam == null)
            {
                Debug.LogError($"[{name}] Невозможно выполнить ShootRay: главная камера отсутствует на сцене.");
                return;
            }
        }

        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayDistance, clickLayer))
        {
            if (hit.collider != null)
            {
                ClickableDetailForSlots detail = hit.collider.GetComponentInParent<ClickableDetailForSlots>();
                DetailSlot slot = hit.collider.GetComponentInParent<DetailSlot>();

                if (detail != null) HandleDetailClick(detail);
                else if (slot != null) HandleSlotClick(slot);
            }
        }
    }

    void HandleDetailClick(ClickableDetailForSlots detail)
    {
        if (detail == null) return;

        if (selectedDetail != null && selectedDetail != detail.gameObject)
        {
            var oldScript = selectedDetail.GetComponent<ClickableDetailForSlots>();
            if (oldScript != null) oldScript.SetSelected(false);
        }

        if (selectedDetail == detail.gameObject)
        {
            detail.SetSelected(false);
            selectedDetail = null;
            selectedID = -1;
            ShowHint("Деталь оставлена");
        }
        else
        {
            selectedDetail = detail.gameObject;
            selectedID = detail.detailID;
            detail.SetSelected(true);
            ShowHint("Деталь " + selectedID);
        }
    }

    void HandleSlotClick(DetailSlot slot)
    {
        if (slot == null) return;
        if (selectedDetail == null) { ShowHint("Выберите деталь"); return; }
        if (slot.isOccupied) { ShowHint("HandleSlotClick 2"); return; }
        if (selectedID != slot.slotID) { ShowHint("HandleSlotClick 3" + selectedID); return; }

        GameObject targetDetail = selectedDetail;

        var detailScript = targetDetail.GetComponent<ClickableDetailForSlots>();
        if (detailScript != null)
        {
            detailScript.SetSelected(false);
        }

        selectedDetail = null;
        selectedID = -1;

        StartCoroutine(MoveRoutine(targetDetail, slot));
    }

    IEnumerator MoveRoutine(GameObject detail, DetailSlot slot)
    {
        if (detail == null || slot == null) yield break;

        slot.isOccupied = true;
        Vector3 targetPos = slot.transform.position;

        // ЖЕСТКАЯ СИНХРОНИЗАЦИЯ ПОВОРОТА:
        // Деталь принимает точное глобальное вращение силуэта, созданного в DetailSlot
        Quaternion targetRot = slot.transform.rotation;
        if (slot.visualCube != null)
        {
            targetRot = slot.visualCube.transform.rotation;
        }

        slot.ClearSlot();

        while (detail != null && Vector3.Distance(detail.transform.position, targetPos) > 0.01f)
        {
            detail.transform.position = Vector3.MoveTowards(detail.transform.position, targetPos, moveSpeed * Time.unscaledDeltaTime);
            detail.transform.rotation = Quaternion.Slerp(detail.transform.rotation, targetRot, moveSpeed * Time.unscaledDeltaTime);
            yield return null;
        }

        if (detail != null)
        {
            detail.transform.position = targetPos;
            detail.transform.rotation = targetRot;
            detail.transform.parent = null;

            var detailScript = detail.GetComponent<ClickableDetailForSlots>();
            if (detailScript != null) Destroy(detailScript);

            var col = detail.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        placedCount++;
        UpdateStatus();

        if (placedCount >= totalNeeded)
        {
            FinishMission();
        }
        else
        {
            ShowHint("так держать!");
        }
    }




    void FinishMission()
    {
        if (exitBlocker != null) exitBlocker.SetActive(false);

        MapManager mapManager = FindObjectOfType<MapManager>();
        if (mapManager != null)
        {
            mapManager.UnlockZone(7);
        }
        else
        {
            Debug.LogWarning($"[{name}] MapManager не найден на сцене. Зона 7 не разблокирована.");
        }

        ShowCompletionMessage();

        if (successAudio != null) successAudio.Play();

        ShowHint("задание выполнено!");
    }

    void ShowCompletionMessage()
    {
        ToggleClipboard clipboard = FindObjectOfType<ToggleClipboard>();
        if (clipboard != null)
        {
            clipboard.CompleteTask(1);
        }

        if (successTextObject != null)
        {
            successTextObject.text = "Задание выполнено: фаблаб!\nПолучен фрагмент пазла!";
            successTextObject.gameObject.SetActive(true);

            CancelInvoke("HideNotification");
            Invoke("HideNotification", 3f);
        }

        puzzleHolder.AddPuzzle(); // Прибавить один пазл и обновить экран!
    }

    public void ShowHint(string msg)
    {
        if (hintCoroutine != null) StopCoroutine(hintCoroutine);
        hintCoroutine = StartCoroutine(HintTimer(msg));
    }

    void HideNotification()
    {
        if (successTextObject != null)
        {
            successTextObject.gameObject.SetActive(false);
        }
    }

    IEnumerator HintTimer(string msg)
    {
        if (hintText != null) hintText.text = msg;
        if (hintPanel != null) hintPanel.SetActive(true);

        yield return new WaitForSecondsRealtime(2.5f);

        if (hintPanel != null) hintPanel.SetActive(false);
    }

    void UpdateStatus()
    {
        if (statusText != null) statusText.text = "Собрано " + placedCount + "/" + totalNeeded;
    }

    public void OpenAssemblyUI()
    {
        if (assemblyUIPanel != null) assemblyUIPanel.SetActive(true);
        UpdateStatus();
    }

    public void CloseAssemblyUI()
    {
        if (assemblyUIPanel != null) assemblyUIPanel.SetActive(false);
    }
}