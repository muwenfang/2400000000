using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FloatingMessage : MonoBehaviour
{
    public float fadeDurationi = 0.2f;   // 淡入淡出所需时间
    public float fadeDurationo = 0.85f;
    public float displayDuration = 0.2f;  // 窗口完全显示后停留的时间

    private CanvasGroup canvasGroup;
    public Text scoreText;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if(scoreText == null )
            scoreText = GetComponentInChildren<Text>();
    }


    IEnumerator ShowAndFadeOut()
    {
        // 1. 淡入
        float elapsedTime = 0f;
        canvasGroup.alpha = 0f;
        while (elapsedTime < fadeDurationi)
        {
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDurationi);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // 2. 完全显示并停留
        yield return new WaitForSeconds(displayDuration);

        // 3. 淡出
        elapsedTime = 0f;
        while (elapsedTime < fadeDurationo)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDurationo);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 0f;

        //隐藏
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 外部调用方法，设置文本并显示
    /// </summary>
    public void SetScoreMessage(string msg)
    {
        if (scoreText != null)
        {
            scoreText.text = msg;
        }
        else
        {
            Debug.Log("未传入分数");
        }

        // OnEnable 会自动启动协程
        gameObject.SetActive(true);

        StartCoroutine(ShowAndFadeOut());
    }
}
