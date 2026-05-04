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
    public GameObject successTextObject; // Сюда перетащи зеленый текст "Задание выполнено"
    public AudioSource successAudio;     // Сюда перетащи компонент AudioSource со звуком

    [Header("Логика")]
    public GameObject exitBlocker;
    public float moveSpeed = 10f;
    public int totalNeeded = 5;

    private int placedCount = 0;
    private GameObject selectedDetail = null;
    private int selectedID = -1;
    private Camera mainCam;
    private Coroutine hintCoroutine;

    void Start()
    {
        mainCam = Camera.main;
        if (successTextObject) successTextObject.SetActive(false); // Гарантируем, что текст скрыт
    }

    void Update()
    {
        if (assemblyUIPanel == null || !assemblyUIPanel.activeSelf) return;
        if (Input.GetMouseButtonDown(0)) ShootRay();
    }

    void ShootRay()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayDistance, clickLayer))
        {
            ClickableDetailForSlots detail = hit.collider.GetComponentInParent<ClickableDetailForSlots>();
            DetailSlot slot = hit.collider.GetComponentInParent<DetailSlot>();

            if (detail != null) HandleDetailClick(detail);
            else if (slot != null) HandleSlotClick(slot);
        }
    }

    void HandleDetailClick(ClickableDetailForSlots detail)
    {
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
            ShowHint("Выбор снят");
        }
        else
        {
            selectedDetail = detail.gameObject;
            selectedID = detail.detailID;
            detail.SetSelected(true);
            ShowHint("Выбрана деталь #" + selectedID);
        }
    }

    void HandleSlotClick(DetailSlot slot)
    {
        if (selectedDetail == null) { ShowHint("Сначала выбери деталь!"); return; }
        if (slot.isOccupied) { ShowHint("Слот занят!"); return; }
        if (selectedID != slot.slotID) { ShowHint("Нужен слот #" + selectedID); return; }

        GameObject targetDetail = selectedDetail;
        targetDetail.GetComponent<ClickableDetailForSlots>().SetSelected(false);

        selectedDetail = null;
        selectedID = -1;

        StartCoroutine(MoveRoutine(targetDetail, slot));
    }

    IEnumerator MoveRoutine(GameObject detail, DetailSlot slot)
    {
        slot.isOccupied = true;
        Vector3 targetPos = slot.transform.position;
        Quaternion targetRot = slot.transform.rotation;
        slot.ClearSlot();

        while (Vector3.Distance(detail.transform.position, targetPos) > 0.01f)
        {
            detail.transform.position = Vector3.MoveTowards(detail.transform.position, targetPos, moveSpeed * Time.unscaledDeltaTime);
            detail.transform.rotation = Quaternion.Slerp(detail.transform.rotation, targetRot, moveSpeed * Time.unscaledDeltaTime);
            yield return null;
        }

        detail.transform.position = targetPos;
        detail.transform.rotation = targetRot;
        detail.transform.parent = null;

        Destroy(detail.GetComponent<ClickableDetailForSlots>());
        if (detail.GetComponent<Collider>()) detail.GetComponent<Collider>().enabled = false;

        placedCount++;
        UpdateStatus();

        if (placedCount >= totalNeeded)
        {
            FinishMission();
        }
        else
        {
            ShowHint("Установлено!");
        }
    }

    void FinishMission()
    {
        if (exitBlocker) exitBlocker.SetActive(false);

        // Включаем зеленый текст
        if (successTextObject) {
            successTextObject.SetActive(true);
            Invoke("TextCloser", 3f);
        }
        

        // Воспроизводим звук
        if (successAudio) successAudio.Play();

        ShowHint("СБОРКА ЗАВЕРШЕНА!");
    }

    private void TextCloser() 
    {
        successTextObject.SetActive(false);
    }

    public void ShowHint(string msg)
    {
        if (hintCoroutine != null) StopCoroutine(hintCoroutine);
        hintCoroutine = StartCoroutine(HintTimer(msg));
    }

    IEnumerator HintTimer(string msg)
    {
        hintText.text = msg;
        hintPanel.SetActive(true);
        yield return new WaitForSecondsRealtime(2.5f);
        hintPanel.SetActive(false);
    }

    void UpdateStatus() { if (statusText) statusText.text = "Собрано: " + placedCount + "/" + totalNeeded; }
    public void OpenAssemblyUI() { if (assemblyUIPanel) assemblyUIPanel.SetActive(true); UpdateStatus(); }
    public void CloseAssemblyUI() { if (assemblyUIPanel) assemblyUIPanel.SetActive(false); }
}
