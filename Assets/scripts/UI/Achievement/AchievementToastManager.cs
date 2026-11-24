using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 成就解锁提示的队列管理器（类似 Steam 成就弹窗）：
///   解锁 → 立即入队 → 依次弹出 → 停留 → 自动消失；多条同时达成时上下排列、互不遮挡。
///
/// 搭建方式（UI 全部手工做，本脚本不生成任何界面）：
///   1. 做一条提示框预制体：根节点挂 AchievementToastUI，内部文字 / 图标 / 背景随意，
///      把"成就名字"那个 Text 拖到 AchievementToastUI.nameText（只需要这一处会变）；
///   2. 在常驻 UI 下新建空节点（或直接用现有 Canvas 节点）挂本脚本；
///   3. Inspector 里拖入：
///        · toastPrefab    —— 第 1 步的提示框预制体
///        · toastContainer —— 提示框父节点（建议挂 VerticalLayoutGroup 自动排列；
///                            留空则用本节点，并用 stackSpacing / stackUpward 手工堆叠）
///
/// 排列规则：
///   · 容器有 LayoutGroup → 交给布局组排（新条目追加为最后一个子节点）；
///   · 没有布局组 → 手工堆叠：最新一条停在预制体的原始位置，较早的沿 stackUpward 方向
///     依次推开（间距 = 条高 + stackSpacing），保证多条不重叠。
///   · 同屏条数上限 maxVisible：满员时后面的排队等待，最早一条消失后自动补位，不截断任何成就。
/// </summary>
[DisallowMultipleComponent]
public class AchievementToastManager : MonoBehaviour
{
    [Header("预制体与容器")]
    [Tooltip("提示框预制体：根节点挂 AchievementToastUI")]
    public GameObject toastPrefab;

    [Tooltip("提示框父节点（建议挂 VerticalLayoutGroup）；留空则用本节点")]
    public Transform toastContainer;

    [Header("队列")]
    [Tooltip("同屏最多显示几条；超出的排队等待，有位置空出再弹，保证每条都完整显示")]
    public int maxVisible = 4;

    [Tooltip("相邻两条的弹出间隔（秒）：让多条成就依次出现，而不是同一帧一起闪出")]
    public float spawnInterval = 0.15f;

    [Header("手工堆叠（容器没有 LayoutGroup 时生效）")]
    [Tooltip("两条之间的间距（像素）")]
    public float stackSpacing = 8f;

    [Tooltip("true = 最新一条在下方、较早的被向上推开；false = 反向")]
    public bool stackUpward = true;

    [Tooltip("量不出预制体高度时的兜底高度（像素）")]
    public float fallbackItemHeight = 100f;

    [Header("其它")]
    [Tooltip("自动订阅 AchievementManager.AchievementUnlocked（通常保持勾选）")]
    public bool autoSubscribe = true;

    /// <summary>当前存活的管理器（后创建的覆盖先创建的；供外部无引用调用 Notify）</summary>
    public static AchievementToastManager Instance { get; private set; }

    private readonly Queue<AchievementData> pending = new Queue<AchievementData>();
    private readonly List<AchievementToastUI> live = new List<AchievementToastUI>();

    private Coroutine pumpCoroutine;
    private bool warnedMissingPrefab;

    /// <summary>还在排队、尚未弹出的成就数量</summary>
    public int PendingCount { get { return pending.Count; } }

    /// <summary>当前正在显示（含淡出中）的提示框数量</summary>
    public int LiveCount { get { return live.Count; } }

    private Transform Container
    {
        get { return toastContainer != null ? toastContainer : transform; }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        if (autoSubscribe)
        {
            AchievementManager.AchievementUnlocked -= HandleAchievementUnlocked;
            AchievementManager.AchievementUnlocked += HandleAchievementUnlocked;
        }

        // 中途启用（切场景 / 编辑器里重新勾选）时把遗留队列续上
        if (pending.Count > 0) EnsurePump();
    }

    private void OnDisable()
    {
        AchievementManager.AchievementUnlocked -= HandleAchievementUnlocked;

        if (pumpCoroutine != null)
        {
            StopCoroutine(pumpCoroutine);
            pumpCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ===================== 对外入口 =====================

    /// <summary>解锁事件入口</summary>
    private void HandleAchievementUnlocked(AchievementData achievement)
    {
        Enqueue(achievement);
    }

    /// <summary>手动弹出某条成就（测试 / 自定义触发）</summary>
    public void ShowAchievement(AchievementData achievement)
    {
        Enqueue(achievement);
    }

    /// <summary>手动弹出（直接给成就名字）</summary>
    public void ShowAchievementByName(string achievementName)
    {
        if (string.IsNullOrEmpty(achievementName)) return;

        AchievementData achievement = null;
        for (int i = 0; i < AchievementManager.Count; i++)
        {
            AchievementData candidate = AchievementManager.GetAchievement(i);
            if (candidate != null && candidate.name == achievementName)
            {
                achievement = candidate;
                break;
            }
        }

        if (achievement != null)
        {
            Enqueue(achievement);
        }
        else
        {
            // 表里没有的名字：借用临时数据弹出，仅用于调试
            Enqueue(new AchievementData(-1, achievementName, string.Empty));
        }
    }

    /// <summary>外部无需持有引用即可弹提示（管理器不存在时静默忽略）</summary>
    public static void Notify(AchievementData achievement)
    {
        if (Instance != null) Instance.Enqueue(achievement);
    }

    /// <summary>清空队列并立刻收掉正在显示的提示（重开游戏 / 调试用）</summary>
    public void ClearAll()
    {
        pending.Clear();

        for (int i = live.Count - 1; i >= 0; i--)
        {
            AchievementToastUI item = live[i];
            live.RemoveAt(i);
            if (item != null) item.Dismiss();
        }
        Reflow();
    }

    // ===================== 队列与生成 =====================

    private void Enqueue(AchievementData achievement)
    {
        if (achievement == null) return;

        if (toastPrefab == null)
        {
            if (!warnedMissingPrefab)
            {
                warnedMissingPrefab = true;
                Debug.LogWarning("[成就] 提示框预制体未配置（toastPrefab），成就解锁提示不会弹出。");
            }
            return;
        }

        pending.Enqueue(achievement);
        EnsurePump();
    }

    private void EnsurePump()
    {
        if (pumpCoroutine != null) return;
        if (!isActiveAndEnabled) return;
        pumpCoroutine = StartCoroutine(PumpRoutine());
    }

    /// <summary>依次消费队列：每弹出一条等 spawnInterval；满员则等待空位</summary>
    private IEnumerator PumpRoutine()
    {
        while (pending.Count > 0)
        {
            // 满员：等最早的一条消失后再补位（不截断成就）
            while (live.Count >= Mathf.Max(1, maxVisible))
            {
                yield return null;
            }

            AchievementData achievement = pending.Dequeue();
            Spawn(achievement);

            if (spawnInterval > 0f)
            {
                yield return new WaitForSecondsRealtime(spawnInterval);
            }
            else
            {
                yield return null;
            }
        }

        pumpCoroutine = null;
    }

    /// <summary>生成一条提示框：追加为最后一个子节点 → 记录堆叠基准 → 重排 → 播放</summary>
    private void Spawn(AchievementData achievement)
    {
        GameObject go = Instantiate(toastPrefab, Container, false);
        go.transform.localScale = Vector3.one;
        go.transform.SetAsLastSibling();

        AchievementToastUI item = go.GetComponent<AchievementToastUI>();
        if (item == null) item = go.GetComponentInChildren<AchievementToastUI>(true);

        if (item == null)
        {
            Debug.LogWarning($"[成就] 提示框预制体「{toastPrefab.name}」上没有 AchievementToastUI，无法填文字。");
            Destroy(go);
            return;
        }

        // 堆叠基准 = 预制体自己摆好的位置（在重排之前采集）
        RectTransform rect = item.Rect;
        if (rect != null)
        {
            item.stackOrigin = rect.anchoredPosition;
            item.stackOriginCaptured = true;
        }

        live.Insert(0, item); // 索引 0 = 最新一条
        Reflow();

        // Play 内部会先填文字、置零透明度再激活，避免出现一帧白框
        item.Play(achievement, HandleItemFinished);
    }

    private void HandleItemFinished(AchievementToastUI item)
    {
        if (item != null) live.Remove(item);
        Reflow();

        if (item != null) Destroy(item.gameObject);
    }

    /// <summary>
    /// 重排：有布局组就交给布局组（只强制重建一次）；
    /// 没有布局组则按 BuildStackOffsets 的偏移手工摆放。
    /// </summary>
    private void Reflow()
    {
        LayoutGroup layout = Container != null ? Container.GetComponent<LayoutGroup>() : null;
        if (layout != null)
        {
            RectTransform containerRect = Container as RectTransform;
            if (containerRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
            return;
        }

        List<float> heights = new List<float>(live.Count);
        for (int i = 0; i < live.Count; i++)
        {
            AchievementToastUI item = live[i];
            heights.Add(item != null ? item.MeasuredHeight(fallbackItemHeight) : fallbackItemHeight);
        }

        float direction = stackUpward ? 1f : -1f;
        float[] offsets = BuildStackOffsets(heights, stackSpacing, direction);

        for (int i = 0; i < live.Count; i++)
        {
            AchievementToastUI item = live[i];
            if (item == null) continue;

            RectTransform rect = item.Rect;
            if (rect == null) continue;

            Vector2 position = item.stackOrigin;
            position.y += offsets[i];
            rect.anchoredPosition = position;
        }
    }

    /// <summary>
    /// 纯计算：按条目高度算堆叠偏移（索引 0 = 最新一条，停在原点）。
    /// direction = +1 时越靠后越向上推开，-1 时向下。离线回归直接测这个函数。
    /// </summary>
    public static float[] BuildStackOffsets(IList<float> heights, float spacing, float direction)
    {
        if (heights == null) return new float[0];

        float[] offsets = new float[heights.Count];
        float cursor = 0f;

        for (int i = 0; i < heights.Count; i++)
        {
            offsets[i] = cursor;

            float height = heights[i];
            if (height < 0f) height = 0f;
            cursor += (height + spacing) * direction;
        }

        return offsets;
    }
}
