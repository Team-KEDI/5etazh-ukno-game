using UnityEngine;

public class ClickableDetailForSlots : MonoBehaviour
{
    [SerializeField] private int _detailID;
    private Vector3 originalScale;
    private bool isSelected = false;

    // Свойство только для чтения. Защищает ID от изменений кодом
    public int detailID => _detailID;

    public void Initialize(int id)
    {
        // СТРОКА С ТРЭШ-ИЗМЕНЕНИЕМ ID УДАЛЕНА: detailID = id;
        // Теперь значение из инспектора железно сохраняется!

        originalScale = transform.localScale;
        if (GetComponent<Collider>() == null) gameObject.AddComponent<BoxCollider>();
    }

    public void SetSelected(bool state)
    {
        if (isSelected == state) return;
        isSelected = state;

        if (isSelected)
        {
            transform.localScale = originalScale * 1.2f;
            transform.position += Vector3.up * 0.1f;
        }
        else
        {
            transform.localScale = originalScale;
            transform.position -= Vector3.up * 0.1f;
        }
    }
}
