using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 成就解锁提示框 —— 单条实例，挂在"提示框预制体"的根节点上
/// （UI 全部在 Inspector 里手工搭建，本脚本不生成任何 UI、不改布局）。
///
/// 预制体结构示例（节点名随意，脚本只认你拖进来的引用）：
///   AchievementToast（本脚本 + CanvasGroup + 背景 Image）
///     ├─ TitleText   ← 固定文案（如"成就解锁"），本脚本不碰
///     └─ NameText    ← 成就名字，拖到 nameText
///
/// 本脚本只做三件事：
///   1. 把成就名字写进 nameText（其余文字 / 配色 / 尺寸 / 布局沿用预制体，完全不动）；
///   2. 播放"弹入 → 停留 → 淡出"动画（类似 Steam 的成就弹窗）；
///   3. 播完回调管理器，由管理器销毁并重排剩余提示框。
///
/// 触发、排队、多条排列都不在这里 —— 见 AchievementToastManager。
/// </summary>
[DisallowMultipleComponent]
public class AchievementToastUI : MonoBehaviour
{
    [Header("引用（Inspector 拖入）")]
    [Tooltip("成就名字文本：本脚本只改这一处文字，其它文案/配色/布局沿用预制体")]
    public Text nameText;

    [Tooltip("成就描述文本（可选）：拖了才替换，留空则预制体里的描述保持原样")]
    public Text descText;

    [Tooltip("淡入淡出载体；留空则自动取/补 CanvasGroup（只加这一个组件，不动布局）")]
    public CanvasGroup canvasGroup;

    [Header("动画参数")]
    [Tooltip("弹入时长（秒）：透明度 0→1，同时从 popScale 放大到 1")]
    public float fadeInDuration = 0.25f;

    [Tooltip("完整显示后的停留时长（秒）")]
    public float holdDuration = 2.2f;

    [Tooltip("淡出时长（秒）")]
    public float fadeOutDuration = 0.45f;

    [Tooltip("出现瞬间的缩放（1 = 不做缩放动画）")]
    public float popScale = 0.85f;

    [Tooltip("勾选后用不受 Time.timeScale 影响的真实时间：结算动画/暂停期间也能正常弹出与消失")]
    public bool useUnscaledTime = true;

    [Header("文案")]
    [Tooltip("文本格式；{0} = 成就名字。想加前缀可改成「成就解锁：{0}」")]
    public string toastTextFormat = "{0}";

    // ===== 以下由 AchievementToastManager 在生成时写入（不需要在 Inspector 配置） =====
    [NonSerialized] public Vector2 stackOrigin;        // 预制体原始锚点位置（堆叠基准）
    [NonSerialized] public bool stackOriginCaptured;

    private RectTransform rectTransform;
    private Coroutine playCoroutine;
    private Action<AchievementToastUI> onFinished;
    private bool finished;

    /// <summary>本提示框的 RectTransform（缓存；预制体不是 UI 对象时为 null）</summary>
    public RectTransform Rect
    {
        get
        {
            if (rectTransform == null) rectTransform = transform as RectTransform;
            return rectTransform;
        }
    }

    /// <summary>本提示框当前高度（用于堆叠间距计算）</summary>
    public float MeasuredHeight(float fallback)
    {
        RectTransform rect = Rect;
        if (rect != null)
        {
            float height = rect.rect.height;
            if (height > 1f) return height;

            float preferred = LayoutUtility.GetPreferredHeight(rect);
            if (preferred > 1f) return preferred;
        }
        return fallback;
    }

    /// <summary>把成就数据写进预制体上的文字（只改文字，不动其它任何视觉）</summary>
    public void Bind(AchievementData achievement)
    {
        if (achievement == null) return;

        if (nameText == null) nameText = ResolveNameText();

        if (nameText != null)
        {
            nameText.text = string.Format(toastTextFormat, achievement.name);
        }

        if (descText != null)
        {
            descText.text = achievement.description;
        }
    }

    /// <summary>
    /// 没拖 nameText 时的兜底：优先按节点名找（Name / 名字 / AchievementName），
    /// 整条提示框只有一个 Text 时直接用它；有多个且认不出则保持原样并提示去拖引用。
    /// </summary>
    private Text ResolveNameText()
    {
        Text[] texts = GetComponentsInChildren<Text>(true);
        if (texts == null || texts.Length == 0) return null;

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null) continue;

            string nodeName = texts[i].gameObject.name;
            if (nodeName == null) continue;
            if (nodeName.IndexOf("name", StringComparison.OrdinalIgnoreCase) >= 0
                || nodeName.IndexOf("名字", StringComparison.Ordinal) >= 0)
            {
                return texts[i];
            }
        }

        if (texts.Length == 1) return texts[0];

        Debug.LogWarning("[成就] 提示框里有多个 Text，认不出哪个是成就名字，请在 Inspector 把名字文本拖到 nameText。");
        return null;
    }

    /// <summary>
    /// 开始播放：先填文字，再弹入 → 停留 → 淡出。
    /// finished 回调在动画结束时触发一次（管理器据此销毁本节点并重排）。
    /// </summary>
    public void Play(AchievementData achievement, Action<AchievementToastUI> finishedCallback)
    {
        Bind(achievement);

        onFinished = finishedCallback;
        finished = false;

        EnsureCanvasGroup();

        // 预制体可能是"从 0 缩放"的建法：先归一化，再交给动画驱动
        transform.localScale = Vector3.one * Mathf.Max(0.01f, popScale);

        if (canvasGroup != null) canvasGroup.alpha = 0f;

        // StartCoroutine 要求对象处于激活状态
        gameObject.SetActive(true);

        if (playCoroutine != null) StopCoroutine(playCoroutine);
        playCoroutine = StartCoroutine(PlayRoutine());
    }

    /// <summary>立即提前结束（管理器降级/清场时用）：快速淡出后走正常收尾</summary>
    public void Dismiss()
    {
        if (finished) return;
        if (!isActiveAndEnabled) { Finish(); return; }

        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
            playCoroutine = null;
        }
        playCoroutine = StartCoroutine(FadeOutRoutine(0.15f));
    }

    private IEnumerator PlayRoutine()
    {
        // 弹入：透明度 + 轻微放大，起手就能被看见
        yield return FadeInRoutine(fadeInDuration, popScale);

        if (holdDuration > 0f) yield return Wait(holdDuration);

        yield return FadeOutRoutine(fadeOutDuration);

        playCoroutine = null;
        Finish();
    }

    private IEnumerator FadeInRoutine(float duration, float fromScale)
    {
        if (duration <= 0f)
        {
            Apply(1f, 1f);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += DeltaTime;
            float progress = Mathf.Clamp01(t / duration);
            // 略微缓出，落位更利落
            float eased = 1f - (1f - progress) * (1f - progress);
            Apply(progress, Mathf.Lerp(fromScale, 1f, eased));
            yield return null;
        }
        Apply(1f, 1f);
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        if (duration <= 0f)
        {
            Apply(0f, 1f);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += DeltaTime;
            Apply(Mathf.Clamp01(1f - t / duration), 1f);
            yield return null;
        }
        Apply(0f, 1f);
    }

    private void Apply(float alpha, float scale)
    {
        if (canvasGroup != null) canvasGroup.alpha = alpha;
        transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
    }

    private float DeltaTime
    {
        get { return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime; }
    }

    private object Wait(float seconds)
    {
        if (useUnscaledTime) return new WaitForSecondsRealtime(seconds);
        return new WaitForSeconds(seconds);
    }

    private void Finish()
    {
        if (finished) return;
        finished = true;

        Action<AchievementToastUI> callback = onFinished;
        onFinished = null;

        if (callback != null)
        {
            callback(this);
        }
        else
        {
            // 没人接管（例如手动放进场景测试）：自行销毁，避免残留半透明白框
            Destroy(gameObject);
        }
    }

    private void OnDisable()
    {
        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
            playCoroutine = null;
        }
    }

    /// <summary>取 CanvasGroup；没有就补一个（只影响透明度与点击穿透，不改布局）</summary>
    private void EnsureCanvasGroup()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 提示框是纯展示：不拦截点击，避免挡住下面的按钮
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
}
