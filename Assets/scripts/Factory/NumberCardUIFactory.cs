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
        // 如果 cache 还没初始化，或者里面没东西，手动初始化一次
        if (cache == null || cache.Count == 0)
        {
            InitCache();
        }

        if (cache.TryGetValue(type, out var prefab))
            return prefab;

        Debug.LogWarning($"未配置 {type} 的数字卡 prefab");
        return null;
    }

    private void InitCache()
    {
        cache = new Dictionary<NumberCardLayoutType, GameObject>();
        foreach (var entry in prefabs)
        {
            if (entry.prefab != null)
                cache[entry.layoutType] = entry.prefab;
        }
    }
}
