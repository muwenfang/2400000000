using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NumberCardPrefabEntry
{
    public NumberCardLayoutType layoutType;
    public GameObject prefab;
}
public class NumberCardUIFactory : MonoBehaviour
{
    public List<NumberCardPrefabEntry> prefabs;

    private Dictionary<NumberCardLayoutType, GameObject> cache;

    void Awake()
    {
        cache = new Dictionary<NumberCardLayoutType, GameObject>();
        foreach (var entry in prefabs)
        {
            cache[entry.layoutType] = entry.prefab;
        }
    }

    public GameObject GetPrefab(NumberCardLayoutType type)
    {
        if (cache.TryGetValue(type, out var prefab))
            return prefab;

        Debug.LogWarning($"未配置 {type} 的数字卡 prefab，使用默认");
        return null;
    }
}
