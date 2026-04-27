using UnityEngine;

public class DoorsToggle : MonoBehaviour
{
    [Header("Ссылка на дверь")]
    [Tooltip("Объект двери (должен иметь BoxCollider или MeshFilter для автоматического расчёта левого края)")]
    public Transform doorTransform;

    [Header("Настройки анимации")]
    public AnimationCurve openSpeedCurve = new AnimationCurve(
        new Keyframe(0, 1, 0, 0),
        new Keyframe(0.8f, 1, 0, 0),
        new Keyframe(1, 0, 0, 0)
    );
    public float openSpeedMultiplier = 2.0f;
    public float doorOpenAngle = 90.0f;

    [Header("Направление вращения")]
    [Tooltip("Если true, дверь открывается в противоположную сторону (например, -90° вместо +90°)")]
    public bool reverseDirection = false;

    [Header("Ось вращения (левая кромка)")]
    public Vector3 rotationAxis = Vector3.up;
    public Vector3 pivotOffset = Vector3.zero;

    private bool open = false;
    private bool playerInTrigger = false;

    private float startAngle;
    private float targetAngle;
    private float currentAngle;
    private float openTime = 1f;

    private Vector3 pivotLocalPosition;
    private Vector3 pivotWorldPosition;

    void Start()
    {
        if (doorTransform == null)
        {
            Debug.LogError($"DoorTransform не назначен на объекте {name}", this);
            enabled = false;
            return;
        }

        Collider trigCol = GetComponent<Collider>();
        if (trigCol != null && !trigCol.isTrigger)
            trigCol.isTrigger = true;

        pivotLocalPosition = CalculateLeftEdgePivot() + pivotOffset;
        UpdatePivotWorldPosition();

        currentAngle = 0f;
        startAngle = 0f;
        targetAngle = 0f;
        openTime = 1f;
    }

    void Update()
    {
        if (doorTransform == null) return;

        UpdatePivotWorldPosition();

        if (playerInTrigger && Input.GetKeyDown(KeyCode.E))
        {
            open = !open;
            startAngle = currentAngle;
            float angle = open ? doorOpenAngle : 0f;
            if (reverseDirection && open) angle = -angle;
            targetAngle = angle;
            openTime = 0f;
        }

        if (openTime < 1f)
        {
            float speed = openSpeedMultiplier * openSpeedCurve.Evaluate(openTime);
            openTime += Time.deltaTime * speed;
            if (openTime > 1f) openTime = 1f;

            float newAngle = Mathf.Lerp(startAngle, targetAngle, openTime);
            float deltaAngle = newAngle - currentAngle;

            if (Mathf.Abs(deltaAngle) > 0.001f)
            {
                doorTransform.RotateAround(pivotWorldPosition, rotationAxis, deltaAngle);
                currentAngle = newAngle;
            }
        }
    }

    private Vector3 CalculateLeftEdgePivot()
    {
        Bounds bounds = GetLocalBounds(doorTransform);
        return new Vector3(bounds.min.x, bounds.center.y, bounds.center.z);
    }

    private Bounds GetLocalBounds(Transform target)
    {
        BoxCollider box = target.GetComponent<BoxCollider>();
        if (box != null)
            return new Bounds(box.center, box.size);

        MeshFilter meshFilter = target.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
            return meshFilter.sharedMesh.bounds;

        Debug.LogWarning($"У объекта {target.name} нет BoxCollider или Mesh, используются приблизительные границы", this);
        return new Bounds(Vector3.zero, target.lossyScale);
    }

    private void UpdatePivotWorldPosition()
    {
        pivotWorldPosition = doorTransform.TransformPoint(pivotLocalPosition);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInTrigger = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInTrigger = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (doorTransform == null) return;

        Vector3 pivot;
        if (Application.isPlaying)
            pivot = pivotWorldPosition;
        else
        {
            Bounds bounds = GetLocalBounds(doorTransform);
            Vector3 localPivot = new Vector3(bounds.min.x, bounds.center.y, bounds.center.z) + pivotOffset;
            pivot = doorTransform.TransformPoint(localPivot);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pivot, 0.1f);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(pivot, pivot + rotationAxis.normalized * 0.5f);
    }
}