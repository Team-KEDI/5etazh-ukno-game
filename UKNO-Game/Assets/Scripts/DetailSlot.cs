using UnityEngine;

public class DetailSlot : MonoBehaviour
{
    [Header("Настройки слота")]
    [Tooltip("ID детали, которая должна встать в этот слот (1-5)")]
    public int slotID;
    public bool isOccupied = false;

    [Header("Прямая привязка формы")]
    [Tooltip("Перетащите сюда модель детали (префаб из ProBuilder) для этого слота")]
    public GameObject detailModelPrefab;

    [HideInInspector] public GameObject visualCube;
    private MeshCollider slotCollider;

    // ПОЛНОЕ ИСПРАВЛЕНИЕ ЦВЕТОВЫХ ПОЗИЦИЙ СЛОТОВ
    private Color GetColorByID(int id, float alpha)
    {
        switch (id)
        {
            case 1: return new Color(0.25f, 0.25f, 0.25f, alpha); // ID 1 — Черный (встает на место синего)
            case 2: return new Color(0.85f, 0.25f, 0.85f, alpha); // ID 2 — Фиолетовый (на своем месте)
            case 3: return new Color(0.4f, 1.0f, 0.4f, alpha);    // ID 3 — Зеленый (на своем месте)
            case 4: return new Color(1.0f, 0.25f, 0.25f, alpha);  // ID 4 — Красный (встает на место черного)
            case 5: return new Color(0.2f, 0.6f, 1.0f, alpha);    // ID 5 — Синий (встает на место красного)
            default: return new Color(0f, 1f, 0f, alpha);
        }
    }



    void Start()
    {
        BoxCollider oldBox = GetComponent<BoxCollider>();
        if (oldBox != null) Destroy(oldBox);

        // 1. Создаем объект маркера
        visualCube = new GameObject("Slot_Visual_Marker");
        visualCube.transform.SetParent(this.transform);
        visualCube.transform.localPosition = Vector3.zero;

        // ЯВНЫЙ РАЗВОРOT СИЛУЭТОВ ПО ID В ИГРЕ:
        if (slotID == 4) // Черный
        {
            visualCube.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }
        else if (slotID == 5) // Красный
        {
            visualCube.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        }
        else
        {
            visualCube.transform.localRotation = Quaternion.identity;
        }

        MeshFilter slotMeshFilter = visualCube.AddComponent<MeshFilter>();
        MeshRenderer slotMeshRenderer = visualCube.AddComponent<MeshRenderer>();

        // 2. Копирование геометрии и правильного масштаба
        if (detailModelPrefab != null)
        {
            MeshFilter detailMeshFilter = detailModelPrefab.GetComponentInChildren<MeshFilter>();
            if (detailMeshFilter != null && detailMeshFilter.sharedMesh != null)
            {
                slotMeshFilter.sharedMesh = detailMeshFilter.sharedMesh;

                Vector3 pScale = transform.lossyScale;
                Vector3 dScale = detailModelPrefab.transform.localScale;

                Vector3 finalScale = new Vector3(
                    pScale.x != 0 ? dScale.x / pScale.x : dScale.x,
                    pScale.y != 0 ? dScale.y / pScale.y : dScale.y,
                    pScale.z != 0 ? dScale.z / pScale.z : dScale.z
                );

                // Корректный масштаб для синей детали (ID 1)
                if (slotID == 1)
                {
                    finalScale = dScale;
                }

                visualCube.transform.localScale = finalScale;
            }
            else
            {
                slotMeshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            }
        }

        // 3. Настройка цвета и прозрачности
        Shader standardShader = Shader.Find("Universal Render Pipeline/Lit");
        if (standardShader == null) standardShader = Shader.Find("Standard");

        Material slotMat = new Material(standardShader);
        slotMat.color = GetColorByID(slotID, 0.35f);

        slotMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        slotMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        slotMat.SetInt("_ZWrite", 0);
        slotMat.DisableKeyword("_ALPHATEST_ON");
        slotMat.EnableKeyword("_ALPHABLEND_ON");
        slotMat.renderQueue = 3500;

        if (slotMeshFilter.sharedMesh != null)
        {
            Material[] mats = new Material[slotMeshFilter.sharedMesh.subMeshCount];
            for (int i = 0; i < mats.Length; i++) mats[i] = slotMat;
            slotMeshRenderer.materials = mats;
        }

        // 4. Настройка области нажатия
        slotCollider = visualCube.AddComponent<MeshCollider>();
        if (slotMeshFilter.sharedMesh != null)
        {
            slotCollider.sharedMesh = slotMeshFilter.sharedMesh;
            slotCollider.convex = true;
            slotCollider.isTrigger = true;
        }
    }

    public void ClearSlot()
    {
        isOccupied = true;
        if (visualCube != null) Destroy(visualCube);
        if (slotCollider != null) slotCollider.enabled = false;
    }

    void OnDrawGizmos()
    {
        if (isOccupied) return;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.color = GetColorByID(slotID, 0.15f);
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        Gizmos.color = GetColorByID(slotID, 0.7f);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
}
