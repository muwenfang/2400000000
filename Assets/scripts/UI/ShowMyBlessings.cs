using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowMyBlessings : MonoBehaviour
{
    public Transform contentRoot;
    public ScrollRect scrollRect;

    private Dictionary<BlessingData, GameObject> cardGameObjects = new Dictionary<BlessingData, GameObject>();
    private List<BlessingData> displayedBlessings = new List<BlessingData>();
    private Dictionary<int, int> blessingStackCounts = new Dictionary<int, int>();
    public List<Button> tempAddedButtons = new List<Button>();

    public float cardScale = 1.0f;

    private void OnEnable()
    {
        RefreshAllBlessings();
    }

    public void RefreshAllBlessings()
    {
        foreach (var kvp in cardGameObjects)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        cardGameObjects.Clear();
        displayedBlessings.Clear();
        blessingStackCounts.Clear();

        List<BlessingInstance> ownedList = BlessingManager.Instance.GetOwnedBlessings();
        if (ownedList == null || ownedList.Count == 0) return;

        Dictionary<int, (BlessingData data, int totalCount)> blessingDict =
            new Dictionary<int, (BlessingData, int)>();

        foreach (BlessingInstance instance in ownedList)
        {
            if (instance.data == null) continue;
            if (blessingDict.ContainsKey(instance.data.blessingId))
            {
                var existing = blessingDict[instance.data.blessingId];
                blessingDict[instance.data.blessingId] =
                    (existing.data, existing.totalCount + instance.purchaseCount);
            }
            else
            {
                blessingDict[instance.data.blessingId] =
                    (instance.data, instance.purchaseCount);
            }
        }

        foreach (var kvp in blessingDict.Values)
        {
            CreateBlessingCard(kvp.data, kvp.totalCount);
            displayedBlessings.Add(kvp.data);
            if (!blessingStackCounts.ContainsKey(kvp.data.blessingId))
                blessingStackCounts[kvp.data.blessingId] = kvp.totalCount;
        }
    }

    public void ShowOnlyStackableOwnedBlessings()
    {
        foreach (var kvp in cardGameObjects)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        cardGameObjects.Clear();
        displayedBlessings.Clear();

        List<BlessingData> stackableBlessings = BlessingManager.Instance.blessingLibrary.GetAllStackableBlessing();
        if (stackableBlessings == null) return;

        foreach (BlessingData blessing in stackableBlessings)
        {
            int ownedCount = BlessingManager.Instance.GetBlessingCount(blessing.blessingId);
            if (ownedCount > 0)
            {
                CreateBlessingCard(blessing, ownedCount);
                displayedBlessings.Add(blessing);
            }
        }
    }

    void CreateBlessingCard(BlessingData blessing, int stackCount = 1)
    {
        if (UIManager.Instance.blessingCardPrefab == null)
        {
            Debug.LogError("Blessing Card Prefab is not assigned!");
            return;
        }
        GameObject newCard = Instantiate(UIManager.Instance.blessingCardPrefab, contentRoot);
        newCard.transform.localScale = Vector3.one;
        BlessingUI blessingUI = newCard.GetComponent<BlessingUI>();
        if (blessingUI != null)
        {
            blessingUI.SetBlessingData(blessing);
        }
        cardGameObjects[blessing] = newCard;
    }

    public void ClearTempWishCoinButtons()
    {
        foreach (Button btn in tempAddedButtons)
        {
            if (btn != null) Destroy(btn);
        }
        tempAddedButtons.Clear();
    }

    public void ClearTempDarkBoxButtons()
    {
        foreach (Button btn in tempAddedButtons)
        {
            if (btn != null) Destroy(btn);
        }
        tempAddedButtons.Clear();
    }
}
