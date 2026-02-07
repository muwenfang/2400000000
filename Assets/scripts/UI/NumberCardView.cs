using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NumberCardView : MonoBehaviour
{
    public Transform contentRoot;

    public GameObject singlePrefab;
    public GameObject addPrefab;
    public GameObject multiplyPrefab;
    public GameObject compositePrefab;

    public void Bind(NumberCardData data)
    {
        Clear();

        GameObject prefab = GetPrefab(data.layoutType);
        GameObject ui = Instantiate(prefab, contentRoot);

        ui.GetComponent<NumberCardLayoutView>()
          .Bind(data);
    }

    void Clear()
    {
        foreach (Transform child in contentRoot)
            Destroy(child.gameObject);
    }

    GameObject GetPrefab(NumberCardLayoutType type)
    {
        return type switch
        {
            NumberCardLayoutType.Single => singlePrefab,
            NumberCardLayoutType.Add_AB => addPrefab,
            NumberCardLayoutType.Multiply_AB => multiplyPrefab,
            NumberCardLayoutType.Composite_AB => compositePrefab,
            _ => singlePrefab
        };
    }
}

