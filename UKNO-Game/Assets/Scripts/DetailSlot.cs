using UnityEngine;

public class DetailSlot : MonoBehaviour
{
    public int slotID;
    public bool isOccupied = false;
    private GameObject visualCube;

    // Размеры твоих деталей для отображения слотов
    private Vector3 slotSize = new Vector3(1f, 1f, 1f);

    void Start()
    {
        // Создаем видимый маркер слота
        visualCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visualCube.transform.SetParent(this.transform);
        visualCube.transform.localPosition = Vector3.zero;
        visualCube.transform.localRotation = Quaternion.identity;

        // Устанавливаем масштаб по твоим размерам
        visualCube.transform.localScale = slotSize;

        // Настраиваем прозрачный зеленый цвет
        Renderer r = visualCube.GetComponent<Renderer>();
        if (r)
        {
            r.material = new Material(Shader.Find("Transparent/Diffuse"));
            r.material.color = new Color(0, 1, 0, 0.4f);
        }

        // Настраиваем коллайдер родителя под размер детали
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) col = gameObject.AddComponent<BoxCollider>();
        col.size = slotSize;
        col.isTrigger = true;
    }

    public void ClearSlot()
    {
        isOccupied = true;
        // Удаляем зеленый маркер, когда деталь вставлена
        if (visualCube != null) Destroy(visualCube);
        // Выключаем коллайдер слота
        if (GetComponent<BoxCollider>()) GetComponent<BoxCollider>().enabled = false;
    }
}
