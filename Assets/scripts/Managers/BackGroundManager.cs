using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackGroundManager : MonoBehaviour
{
    [Header("Background Objects")]
    [SerializeField] private GameObject[] backgroundOptions;

    [Header("Checkmark Objects")]
    [SerializeField] private GameObject[] checkmarkOptions;
    [SerializeField] private Toggle[] toggleOptions;

    [Header("Default Settings")]
    [SerializeField] private int defaultSelectedIndex = 0;

    private int currentSelectedIndex = -1;

    private void Start()
    {
        RegisterToggleListeners();

        int initialIndex = GetInitialSelectedIndex();
        ApplySelection(initialIndex, false);
    }


    /// <summary>
    /// Entry point for Toggle.onValueChanged.
    /// Only switch when the toggle becomes true.
    /// </summary>
    public void SelectBackgroundFromToggle(int index, bool isOn)
    {
        if (!isOn)
        {
            return;
        }

        ApplySelection(index, true);
    }

    public int GetCurrentSelectedIndex()
    {
        return currentSelectedIndex;
    }

    private void RegisterToggleListeners()
    {
        if (toggleOptions == null)
        {
            return;
        }

        for (int i = 0; i < toggleOptions.Length; i++)
        {
            if (toggleOptions[i] == null)
            {
                continue;
            }

            int capturedIndex = i;
            toggleOptions[i].onValueChanged.AddListener(isOn => SelectBackgroundFromToggle(capturedIndex, isOn));
        }
    }

    private int GetInitialSelectedIndex()
    {
        if (HasValidIndex(defaultSelectedIndex))
        {
            return defaultSelectedIndex;
        }

        if (backgroundOptions == null)
        {
            return -1;
        }

        for (int i = 0; i < backgroundOptions.Length; i++)
        {
            if (backgroundOptions[i] != null && backgroundOptions[i].activeSelf)
            {
                return i;
            }
        }

        return backgroundOptions.Length > 0 ? 0 : -1;
    }

    private void ApplySelection(int index, bool logSelection)
    {
        if (!HasValidIndex(index))
        {
            Debug.LogWarning($"[BackGroundManager] Invalid background index: {index}");
            return;
        }

        currentSelectedIndex = index;

        for (int i = 0; i < backgroundOptions.Length; i++)
        {
            if (backgroundOptions[i] != null)
            {
                backgroundOptions[i].SetActive(i == index);
            }
        }

        if (checkmarkOptions != null)
        {
            for (int i = 0; i < checkmarkOptions.Length; i++)
            {
                if (checkmarkOptions[i] != null)
                {
                    checkmarkOptions[i].SetActive(i == index);
                }
            }
        }

        if (toggleOptions != null)
        {
            for (int i = 0; i < toggleOptions.Length; i++)
            {
                if (toggleOptions[i] != null)
                {
                    toggleOptions[i].SetIsOnWithoutNotify(i == index);
                }
            }
        }

        if (logSelection)
        {
            Debug.Log($"[BackGroundManager] Switched background to index {index}");
        }
    }

    private bool HasValidIndex(int index)
    {
        return backgroundOptions != null && index >= 0 && index < backgroundOptions.Length;
    }
}
