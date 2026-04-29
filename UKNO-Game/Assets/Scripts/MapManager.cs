using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MapManager : MonoBehaviour
{
    [System.Serializable]
    public class MapZone
    {
        public string zoneName;          // название зоны (для отладки)
        public Image zoneImage;          // компонент Image этой зоны
        public Material normalMaterial;  // цветной материал (обычный)
        public Material grayscaleMaterial; // ч/б материал
        public string saveKey;           // ключ для PlayerPrefs (например, "Zone_1_Unlocked")
    }

    public List<MapZone> zones = new List<MapZone>();

    void Start()
    {
        LoadProgress();
    }

    // Вызывается из мини-игры при её завершении
    public void UnlockZone(int zoneIndex)
    {
        if (zoneIndex < 0 || zoneIndex >= zones.Count) return;
        MapZone zone = zones[zoneIndex];
        
        // Меняем материал на цветной
        if (zone.zoneImage != null && zone.normalMaterial != null)
            zone.zoneImage.material = zone.normalMaterial;
        
        // Сохраняем в PlayerPrefs
        PlayerPrefs.SetInt(zone.saveKey, 1);
        PlayerPrefs.Save();
    }

    void LoadProgress()
    {
        foreach (MapZone zone in zones)
        {
            bool isUnlocked = PlayerPrefs.GetInt(zone.saveKey, 0) == 1;
            if (isUnlocked)
            {
                if (zone.zoneImage != null && zone.normalMaterial != null)
                    zone.zoneImage.material = zone.normalMaterial;
            }
            else
            {
                if (zone.zoneImage != null && zone.grayscaleMaterial != null)
                    zone.zoneImage.material = zone.grayscaleMaterial;
            }
        }
    }

    // Опционально: сброс прогресса (для тестов)
    public void ResetAllZones()
    {
        foreach (MapZone zone in zones)
        {
            PlayerPrefs.DeleteKey(zone.saveKey);
            if (zone.zoneImage != null && zone.grayscaleMaterial != null)
                zone.zoneImage.material = zone.grayscaleMaterial;
        }
        PlayerPrefs.Save();
    }
}