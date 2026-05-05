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
        for(int i = 0; i<zones.Count; i++)
        {
            if (i == 7 | i == 3 | i == 4)
            {
                zones[i].zoneImage.material = zones[i].grayscaleMaterial;
            }
        }
    }

    // Вызывается из мини-игры при её завершении
    public void UnlockZone(int zoneIndex)
    {
        if (zoneIndex < 0 || zoneIndex >= zones.Count) return;
        MapZone zone = zones[zoneIndex];
        
        // Меняем материал на цветной
        if (zone.zoneImage != null)
            zone.zoneImage.material = zone.normalMaterial;
    }
}