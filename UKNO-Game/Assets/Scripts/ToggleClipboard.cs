using UnityEngine;

public class ToggleClipboard : MonoBehaviour
{
    [Header("Òî÷êà îñìîòðà ïåðåä ãëàçàìè")]
    public Transform inspectPosition; // Ñþäà ïåðåòàùèòü InspectPos_Anchor èç Êàìåðû

    [Header("Òî÷êà ñêðûòîãî õðàíåíèÿ")]
    public Transform hiddenPosition;  // Ñþäà ïåðåòàùèòü HiddenPos_Anchor èç Êàìåðû

    [Header("Ìàññèâ îáúåêòîâ-ãàëî÷åê")]
    public GameObject[] taskCheckmarks; // Ñþäà ïåðåòàùèòå âàøè îáúåêòû Square ïî ïîðÿäêó!

    [Header("Íàñòðîéêè ñêîðîñòè")]
    public float moveSpeed = 12f;
    public float rotateSpeed = 12f;

    private bool isPlayerNear = false;
    private bool isPickedUp = false;
    private bool isInspecting = false;
    private Collider objectCollider;
    public GameObject list;
    private Renderer renderer;

    void Start()
    {
        objectCollider = GetComponent<Collider>();
        renderer = list.GetComponent<Renderer>();
    }

    void Update()
    {
        // 1. Ïîäáèðàåì êîíòåéíåð ñî ñòîëà íà êíîïêó E
        if (isPlayerNear && !isPickedUp && Input.GetKeyDown(KeyCode.E))
        {
            PickUp();
        }

        // 2. Åñëè ïîäîáðàëè, ïëàâíî ïåðåìåùàåì êîíòåéíåð ïî ÏÊÌ
        if (isPickedUp)
        {
            if (!renderer.enabled)
            {
                Debug.Log("рендер включен");
                renderer.enabled = true;
                UpdateCheckmarks();
            }
            if (Input.GetMouseButtonDown(1))
            {
                isInspecting = !isInspecting;
            }

            // Âûáèðàåì öåëåâîé ÿêîðü â êàìåðå
            Transform targetAnchor = isInspecting ? inspectPosition : hiddenPosition;

            // Äâèãàåì è êðóòèì âíåøíèé êîíòåéíåð
            transform.position = Vector3.Lerp(transform.position, targetAnchor.position, Time.deltaTime * moveSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetAnchor.rotation, Time.deltaTime * rotateSpeed);
        }
    }

    void PickUp()
    {
        isPickedUp = true;
        if (objectCollider != null) objectCollider.enabled = false;

        // Íàõîäèì ñàìó 3D-ìîäåëü ïëàíøåòêè (ïåðâûé äî÷åðíèé îáúåêò)
        Transform modelTransform = transform.GetChild(0);

        // Ïðèâÿçûâàåì âíåøíèé êîíòåéíåð ê êàìåðå èãðîêà
        transform.SetParent(inspectPosition.parent);

        // Ñáðàñûâàåì êîîðäèíàòû êîíòåéíåðà
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // Çàäàåì âàøè èäåàëüíûå ëîêàëüíûå êîîðäèíàòû äëÿ 3D-ìîäåëè
        if (modelTransform != null)
        {
            modelTransform.localPosition = new Vector3(0.9f, 0.2f, 0.8f);
            modelTransform.localRotation = Quaternion.Euler(90f, 25f, 180f);
        }
    }

    // Ýòó ôóíêöèþ áóäóò âûçûâàòü âàøè ìèíè-èãðû ïðè ïîáåäå
    public void CompleteTask(int taskIndex)
    {
        if (taskIndex >= 0 && taskIndex < taskCheckmarks.Length)
        {
            taskCheckmarks[taskIndex].SetActive(true); // Âêëþ÷àåì íóæíóþ ãàëî÷êó
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) isPlayerNear = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) isPlayerNear = false;
    }

    private void UpdateCheckmarks()
    {
        foreach (GameObject cm in taskCheckmarks)
        {
            Renderer rnd = cm.GetComponent<Renderer>();
            if (rnd != null)
            {
                rnd.enabled = true;
            }
        }
    }
}
