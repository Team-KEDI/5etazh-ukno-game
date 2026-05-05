using UnityEngine;

public class AssemblyCameraController : MonoBehaviour
{
    [Header("��������� �������")]
    public Transform assemblyTable;
    public Vector3 offsetFromTable = new Vector3(0f, 1.8f, 0f);
    public float moveDuration = 0.6f;

    private Vector3 startPos;
    private Quaternion startRot;
    private Vector3 targetPos;
    private Quaternion targetRot;
    private float progress = 1f;
    private bool isMoving = false;

    private Vector3 playerOriginalPosition;
    private Quaternion playerOriginalRotation;
    private bool hasSavedPlayerPosition = false;

    public void MoveToTable()
    {
        if (isMoving) return;

        if (!hasSavedPlayerPosition)
        {
            playerOriginalPosition = transform.position;
            playerOriginalRotation = transform.rotation;
            hasSavedPlayerPosition = true;
        }

        startPos = transform.position;
        startRot = transform.rotation;

        targetPos = assemblyTable.position + offsetFromTable;

        targetRot = Quaternion.Euler(90f, 0f, 0f);

        progress = 0f;
        isMoving = true;
    }

    public void ReturnToPlayer()
    {
        if (isMoving) return;

        startPos = transform.position;
        startRot = transform.rotation;

        targetPos = playerOriginalPosition;
        targetRot = playerOriginalRotation;

        progress = 0f;
        isMoving = true;
        hasSavedPlayerPosition = false;
    }

    void Update()
    {
        if (!isMoving) return;

        progress += Time.unscaledDeltaTime / moveDuration;
        float t = Mathf.SmoothStep(0f, 1f, progress);

        transform.position = Vector3.Lerp(startPos, targetPos, t);
        transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

        if (progress >= 1f)
        {
            isMoving = false;
        }
    }
}
