using UnityEngine;

public class AssemblyCameraController : MonoBehaviour
{
    [Header("Настройки позиции")]
    public Transform assemblyTable;
    // X=0 (центр), Y=1.8 (высота над столом), Z=0 (центр)
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

        // Сохраняем позицию игрока/камеры перед перелетом
        if (!hasSavedPlayerPosition)
        {
            playerOriginalPosition = transform.position;
            playerOriginalRotation = transform.rotation;
            hasSavedPlayerPosition = true;
        }

        startPos = transform.position;
        startRot = transform.rotation;

        // Целевая точка: строго над объектом стола на заданной высоте
        targetPos = assemblyTable.position + offsetFromTable;

        // Поворот: 90 градусов по X (взгляд вниз), 0 по Y и Z
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

        // Используем unscaledDeltaTime, так как во время сборки Time.timeScale может быть 0
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
