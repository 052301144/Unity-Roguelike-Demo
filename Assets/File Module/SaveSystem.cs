using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Save data root. Extend with new fields without breaking existing data.
/// </summary>
[System.Serializable]
public class SaveData
{
    public PlayerStateData player = new PlayerStateData();
    public InventoryData inventory = new InventoryData();
    // Add more sections here (e.g., quests, settings) as needed.
}

[System.Serializable]
public class PlayerStateData
{
    public float health;
    public float mana;
    public int coins;
    public string mapId;
    public SerializableVector3 position;
}

[System.Serializable]
public class InventoryData
{
    public List<ItemStackData> items = new List<ItemStackData>();
}

/// <summary>
/// Represents one stack of an item. Extend with extra runtime stats if needed.
/// </summary>
[System.Serializable]
public class ItemStackData
{
    public string itemId;
    public int count;
    public int enhancementLevel; // optional runtime stat
    public int durability;       // optional runtime stat
    // Add more properties for affixes, timers, etc.
}

/// <summary>
/// Serializable Vector3 for JSON persistence.
/// </summary>
[System.Serializable]
public struct SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }

    public Vector3 ToVector3() => new Vector3(x, y, z);
}

/// <summary>
/// Simple JSON-based save/load helper. Stores data in persistentDataPath.
/// </summary>
public static class SaveSystem
{
    private const string FileName = "save.json";

    private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    public static void Save(SaveData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[SaveSystem] Save data is null, skip save.");
            return;
        }

        var json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[SaveSystem] Saved to {SavePath}");
    }

    public static SaveData Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("[SaveSystem] No save file found, returning new SaveData.");
            return new SaveData();
        }

        var json = File.ReadAllText(SavePath);
        var data = JsonUtility.FromJson<SaveData>(json);
        return data ?? new SaveData();
    }
}
