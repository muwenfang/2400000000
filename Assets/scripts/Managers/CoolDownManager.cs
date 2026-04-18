using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用冷却管理器
/// 用于管理按钮、卡牌选择、购买等操作的冷却时间
/// 防止用户连续快速点击导致的逻辑错误
/// </summary>
public class CooldownManager : MonoBehaviour
{
    public static CooldownManager Instance { get; private set; }

    /// <summary>
    /// 冷却类型枚举
    /// </summary>
    public enum CooldownType
    {
        CardSelection,      // 卡牌选择冷却
        ShopPurchase,       // 商店购买冷却
        SlotUnlock,         // 槽位解锁冷却
        CardDeletion,       // 卡牌删除冷却
        General             // 通用冷却
    }

    /// <summary>
    /// 冷却信息类
    /// </summary>
    private class CooldownInfo
    {
        public float remainingTime;
        public float totalDuration;

        public CooldownInfo(float duration)
        {
            remainingTime = duration;
            totalDuration = duration;
        }

        public void Update(float deltaTime)
        {
            remainingTime = Mathf.Max(0, remainingTime - deltaTime);
        }

        public bool IsActive => remainingTime > 0;

        public float Progress => 1f - (remainingTime / totalDuration);
    }

    // 存储各类型的冷却时间
    private Dictionary<CooldownType, CooldownInfo> cooldowns = new Dictionary<CooldownType, CooldownInfo>();

    // 冷却时间配置（秒）
    [SerializeField] private float cardSelectionCooldown = 0.1f;
    [SerializeField] private float shopPurchaseCooldown = 0.2f;
    [SerializeField] private float slotUnlockCooldown = 0.1f;
    [SerializeField] private float cardDeletionCooldown = 0.06f;
    [SerializeField] private float generalCooldown = 0.1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // 更新所有冷却时间
        foreach (var cooldown in cooldowns.Values)
        {
            if (cooldown.IsActive)
            {
                cooldown.Update(Time.deltaTime);
            }
        }
    }

    /// <summary>
    /// 检查指定类型是否处于冷却状态
    /// </summary>
    public bool IsInCooldown(CooldownType type)
    {
        if (!cooldowns.ContainsKey(type))
            return false;

        return cooldowns[type].IsActive;
    }

    /// <summary>
    /// 检查是否可以执行操作（不在冷却状态）
    /// </summary>
    public bool CanExecute(CooldownType type)
    {
        return !IsInCooldown(type);
    }

    /// <summary>
    /// 开始冷却
    /// </summary>
    public void StartCooldown(CooldownType type)
    {
        float duration = GetCooldownDuration(type);
        StartCooldown(type, duration);
    }

    /// <summary>
    /// 开始指定时长的冷却
    /// </summary>
    public void StartCooldown(CooldownType type, float duration)
    {
        cooldowns[type] = new CooldownInfo(duration);

        Debug.Log($"[CooldownManager] 开始冷却: {type}, 时长: {duration}秒");
    }

    /// <summary>
    /// 获取剩余冷却时间（秒）
    /// </summary>
    public float GetRemainingTime(CooldownType type)
    {
        if (!cooldowns.ContainsKey(type))
            return 0f;

        return cooldowns[type].remainingTime;
    }

    /// <summary>
    /// 获取冷却进度（0-1）
    /// </summary>
    public float GetCooldownProgress(CooldownType type)
    {
        if (!cooldowns.ContainsKey(type))
            return 0f;

        return cooldowns[type].Progress;
    }

    /// <summary>
    /// 重置冷却
    /// </summary>
    public void ResetCooldown(CooldownType type)
    {
        if (cooldowns.ContainsKey(type))
        {
            cooldowns.Remove(type);
            Debug.Log($"[CooldownManager] 重置冷却: {type}");
        }
    }

    /// <summary>
    /// 重置所有冷却
    /// </summary>
    public void ResetAllCooldowns()
    {
        cooldowns.Clear();
        Debug.Log("[CooldownManager] 重置所有冷却");
    }

    /// <summary>
    /// 获取冷却时间配置
    /// </summary>
    private float GetCooldownDuration(CooldownType type)
    {
        return type switch
        {
            CooldownType.CardSelection => cardSelectionCooldown,
            CooldownType.ShopPurchase => shopPurchaseCooldown,
            CooldownType.SlotUnlock => slotUnlockCooldown,
            CooldownType.CardDeletion => cardDeletionCooldown,
            CooldownType.General => generalCooldown,
            _ => 0.2f
        };
    }

    /// <summary>
    /// 设置冷却时间配置
    /// </summary>
    public void SetCooldownDuration(CooldownType type, float duration)
    {
        switch (type)
        {
            case CooldownType.CardSelection:
                cardSelectionCooldown = duration;
                break;
            case CooldownType.ShopPurchase:
                shopPurchaseCooldown = duration;
                break;
            case CooldownType.SlotUnlock:
                slotUnlockCooldown = duration;
                break;
            case CooldownType.CardDeletion:
                cardDeletionCooldown = duration;
                break;
            case CooldownType.General:
                generalCooldown = duration;
                break;
        }
    }

    /// <summary>
    /// 执行带冷却保护的操作
    /// </summary>
    /// <param name="type">冷却类型</param>
    /// <param name="action">要执行的操作</param>
    /// <returns>是否成功执行</returns>
    public bool ExecuteWithCooldown(CooldownType type, Action action)
    {
        if (IsInCooldown(type))
        {
            Debug.LogWarning($"[CooldownManager] 操作在冷却中: {type}，剩余时间: {GetRemainingTime(type):F2}秒");
            return false;
        }

        action?.Invoke();
        StartCooldown(type);
        return true;
    }

    /// <summary>
    /// 执行带冷却保护且有返回值的操作
    /// </summary>
    /// <param name="type">冷却类型</param>
    /// <param name="func">要执行的操作（有返回值）</param>
    /// <param name="defaultValue">冷却时返回的默认值</param>
    /// <returns>操作的返回值或默认值</returns>
    public T ExecuteWithCooldown<T>(CooldownType type, Func<T> func, T defaultValue = default)
    {
        if (IsInCooldown(type))
        {
            Debug.LogWarning($"[CooldownManager] 操作在冷却中: {type}，剩余时间: {GetRemainingTime(type):F2}秒");
            return defaultValue;
        }

        T result = func != null ? func() : defaultValue;
        StartCooldown(type);
        return result;
    }
}