using UnityEngine;
using TMPro;
using System.Collections;

public class FablabAssemblyTrigger : MonoBehaviour
{
    public KeyCode interactKey = KeyCode.E;
    public GameObject hintPanel;
    public TMP_Text hintText;

    private bool playerInTrigger = false;
    private FablabPickup pickup;
    private AssemblyCameraController camControl;
    private AssemblySlotsManager slotsManager;
    private bool isUIOpen = false;

    void Start()
    {
        pickup = Object.FindAnyObjectByType<FablabPickup>();
        camControl = Object.FindAnyObjectByType<AssemblyCameraController>();
        slotsManager = Object.FindAnyObjectByType<AssemblySlotsManager>();
        if (hintPanel) hintPanel.SetActive(false);
    }

    void Update()
    {
        if (playerInTrigger && !isUIOpen && Input.GetKeyDown(interactKey))
        {
            if (pickup != null && pickup.IsQuestCompleted()) OpenAssembly();
            else if (pickup != null) StartCoroutine(TempHint());
        }
    }

    public void OpenAssembly()
    {
        isUIOpen = true;
        SetPlayerControl(false);
        if (hintPanel) hintPanel.SetActive(false);
        if (pickup) pickup.HideCounter();
        if (camControl && pickup.assemblyTablePoint) camControl.MoveToTable();
        if (slotsManager) slotsManager.OpenAssemblyUI();

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseAssembly()
    {
        isUIOpen = false;
        Time.timeScale = 1f;
        SetPlayerControl(true);
        if (slotsManager) slotsManager.CloseAssemblyUI();
        if (camControl) camControl.ReturnToPlayer();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void SetPlayerControl(bool state)
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (!p) return;
        foreach (var s in p.GetComponentsInChildren<MonoBehaviour>())
        {
            if (!(s is FablabPickup || s is AssemblyCameraController || s is Camera)) s.enabled = state;
        }
    }

    void OnTriggerEnter(Collider other) { if (other.CompareTag("Player")) { playerInTrigger = true; ShowHint(); } }
    void OnTriggerExit(Collider other) { playerInTrigger = false; if (hintPanel) hintPanel.SetActive(false); }
    void ShowHint() { if (hintPanel) hintPanel.SetActive(true); }
    IEnumerator TempHint() { ShowHint(); yield return new WaitForSecondsRealtime(2f);}
}