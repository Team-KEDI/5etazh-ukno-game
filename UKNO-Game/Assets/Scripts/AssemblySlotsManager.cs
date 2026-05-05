using UnityEngine;
using TMPro;
using System.Collections;

public class AssemblySlotsManager : MonoBehaviour
{
    [Header("Íàñòðîéêè ëó÷à (Raycast)")]
    public float rayDistance = 50f;
    public LayerMask clickLayer = -1;

    [Header("UI Ññûëêè")]
    public GameObject assemblyUIPanel;
    public TMP_Text statusText;
    public GameObject hintPanel;
    public TMP_Text hintText;

    [Header("Ôèíàëüíûé UI")]
    public GameObject successTextObject; // Ñþäà ïåðåòàùè çåëåíûé òåêñò "Çàäàíèå âûïîëíåíî"
    public AudioSource successAudio;     // Ñþäà ïåðåòàùè êîìïîíåíò AudioSource ñî çâóêîì

    [Header("Ëîãèêà")]
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
        if (successTextObject) successTextObject.SetActive(false); // Ãàðàíòèðóåì, ÷òî òåêñò ñêðûò
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
        if (selectedDetail == null) { ShowHint("Выберите деталь"); return; }
        if (slot.isOccupied) { ShowHint("HandleSlotClick 2"); return; }
        if (selectedID != slot.slotID) { ShowHint("HandleSlotClick 3" + selectedID); return; }

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
            ShowHint("так держать!");
        }
    }

    void FinishMission()
    {
        if (exitBlocker) exitBlocker.SetActive(false);

        // Âêëþ÷àåì çåëåíûé òåêñò
        if (successTextObject) {
            successTextObject.SetActive(true);
            Invoke("TextCloser", 3f);
        }
        

        // Âîñïðîèçâîäèì çâóê
        if (successAudio) successAudio.Play();

        ShowHint("задание выполнено!");
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

    void UpdateStatus() { if (statusText) statusText.text = "Собрано " + placedCount + "/" + totalNeeded; }
    public void OpenAssemblyUI() { if (assemblyUIPanel) assemblyUIPanel.SetActive(true); UpdateStatus(); }
    public void CloseAssemblyUI() { if (assemblyUIPanel) assemblyUIPanel.SetActive(false); }
}
