using UnityEngine;

public class MapUnlocker : MonoBehaviour
{
    [Header("Карта")]
    public GameObject mapUI;              // весь Canvas или объект карты (сначала выключен)
    
    
    private bool isMapUnlocked = false;
    
    void Start()
    {
        
        // Показываем или скрываем карту при старте
        if (mapUI != null)
            mapUI.SetActive(isMapUnlocked);
    }
    
    // Этот метод вызывается из другой мини-игры при её завершении
    public void UnlockMap()
    {
        if (!isMapUnlocked)
        {
            isMapUnlocked = true;
            if (mapUI != null)
                mapUI.SetActive(true);
            
            Debug.Log("🗺️ Карта разблокирована!");
        }
    }
    
    // Опционально: метод для проверки (чтобы другие скрипты могли спросить)
    public bool IsMapUnlocked()
    {
        return isMapUnlocked;
    }
}