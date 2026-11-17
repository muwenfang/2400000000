using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 成就解锁提示条：挂在已经搭好的 accomplishBar 根节点上（Inspector 拖引用，代码不生成任何 UI）。
///
/// 预制体/场景节点结构（全部在 Inspector 里手工搭建）：
///   accomplishBar（本脚本）
///     └─ NameText ← 成就名字文本，拖到 nameText
///
/// 动画（CanvasGroup 淡入淡出，锚点/位置由你布置在底部）：
///   解锁瞬间 → 文本设为成就名字 → 淡入 → 停留 → 淡出 → 归零待命。
///
/// 触发方式（二选一或同时）：
///   · 自动：订阅 AchievementManager.AchievementUnlocked，任何成就解锁立即播放；
///   · 手动：任意代码调用 ShowAchievement(achievement)。
/// 连续解锁不重叠：按顺序排队逐条播放（如"感谢陪伴"级联解锁会紧接着主成就播完再播）。
/// </summary>
[DisallowMultipleComponent]
public class AchievementToastUI : MonoBehaviour
{
    [Header("引用（Inspector 拖入）")]
    [Tooltip("成就名字文本；文本内容格式见 toastTextFormat")]
    public Text nameText;

    [Tooltip("淡入淡出载体；留空则自动取/补 CanvasGroup（只加这一个组件，不动布局）")]
    public CanvasGroup canvasGroup;

    [Header("动画参数")]
    [Tooltip("淡入时长（秒）")]
    public float fadeInDuration = 0.4f;

    [Tooltip("停留时长（秒）")]
    public float holdDuration = 1.8f;

    [Tooltip("淡出时长（秒）")]
    public float fadeOutDuration = 0.6f;

    [Header("文案")]
    [Tooltip("文本格式；{0} = 成就名字。想显示前缀可改成「成就解锁：{0}」")]
    public string toastTextFormat = "{0}";

    // 待播放的成就名字队列（连续解锁不丢条、不重叠）
    private readonly Queue<string> pendingNames = new Queue<string>();
    private Coroutine playCoroutine;

    private void OnEnable()
    {
        EnsureCanvasGroup();

        // 订阅解锁事件：解锁瞬间立即进入播放队列
        AchievementManager.AchievementUnlocked += HandleAchievementUnlocked;

        // 提示条平时隐藏（上次可能中断在半透明状态），进入待命
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    private void OnDisable()
    {
        AchievementManager.AchievementUnlocked -= HandleAchievementUnlocked;

        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
            playCoroutine = null;
        }
    }

    /// <summary>解锁事件入口：只读成就名字入队，播放循环统一处理</summary>
    private void HandleAchievementUnlocked(AchievementData achievement)
    {
        if (achievement == null) return;
        Enqueue(achievement.name);
    }

    /// <summary>手动触发入口（测试 / 自定义文案用）</summary>
    public void ShowAchievement(AchievementData achievement)
    {
        if (achievement == null) return;
        Enqueue(achievement.name);
    }

    /// <summary>手动触发入口（直接给名字）</summary>
    public void ShowAchievementByName(string achievementName)
    {
        Enqueue(achievementName);
    }

    private void Enqueue(string achievementName)
    {
        if (string.IsNullOrEmpty(achievementName)) return;

        pendingNames.Enqueue(achievementName);

        // 没有正在播放的动画才启动；正在播则等循环自然接队
        if (playCoroutine == null)
        {
            playCoroutine = StartCoroutine(PlayQueue());
        }
    }

    /// <summary>依次播放队列里全部条目：淡入 → 停留 → 淡出 → 下一条</summary>
    private IEnumerator PlayQueue()
    {
        while (pendingNames.Count > 0)
        {
            string achievementName = pendingNames.Dequeue();

            if (nameText != null)
            {
                nameText.text = string.Format(toastTextFormat, achievementName);
            }

            if (canvasGroup == null)
            {
                // 没有可淡入淡出的载体：至少让文本出现一拍，保证提示不丢
                yield return new WaitForSeconds(holdDuration);
                continue;
            }

            // 淡入
            for (float t = 0f; t < fadeInDuration; t += Time.deltaTime)
            {
                canvasGroup.alpha = Mathf.Clamp01(t / fadeInDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;

            // 停留
            if (holdDuration > 0f)
            {
                yield return new WaitForSeconds(holdDuration);
            }

            // 淡出
            for (float t = 0f; t < fadeOutDuration; t += Time.deltaTime)
            {
                canvasGroup.alpha = Mathf.Clamp01(1f - t / fadeOutDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }

        playCoroutine = null;
    }

    /// <summary>取 CanvasGroup；没有就补一个（该组件只管透明度，不影响你的布局）</summary>
    private void EnsureCanvasGroup()
    {
        if (canvasGroup != null) return;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
}
