using UnityEngine;

public class ClickableDetailForSlots : MonoBehaviour
{
    public int detailID;
    private Vector3 originalScale;
    private bool isSelected = false;

    public void Initialize(int id)
    {
        detailID = id;
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
