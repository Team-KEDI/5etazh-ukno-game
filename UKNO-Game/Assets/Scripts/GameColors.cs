using UnityEngine;

public static class GameColors
{
    // Основные цвета
    public static readonly Color Primary = new Color32(242, 91, 57, 255);  // #f25b39
    public static readonly Color Secondary = new Color32(255, 142, 0, 255); //#ff8e00
    public static readonly Color Success = Color.green;
    public static readonly Color Error = Color.red;
    public static readonly Color Warning = Color.yellow;
    
    // Цвета для текста
    public static readonly Color TextDark = new Color32(30, 30, 40, 255);
    public static readonly Color TextLight = Color.white;
    
    // Дополнительные цвета
    public static readonly Color BackgroundDark = new Color32(20, 20, 30, 255);
    public static readonly Color BackgroundLight = new Color32(240, 240, 250, 255);
}