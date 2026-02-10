using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VisibilityDebugger : MonoBehaviour
{
    void Start()
    {
        var canvas = GetComponentInParent<Canvas>();
        var graphic = GetComponent<Graphic>();

        Debug.Log($"[调试] 物体名: {name}");
        Debug.Log($"[调试] 父级 Canvas: {(canvas != null ? canvas.name : "空")}");
        Debug.Log($"[调试] 层级 Layer: {LayerMask.LayerToName(gameObject.layer)}");

        if (graphic != null)
        {
            Debug.Log($"[调试] 颜色 Alpha: {graphic.color.a}");
            Debug.Log($"[调试] 是否开启渲染: {graphic.enabled}");
            Debug.Log($"[调试] 材质: {graphic.material.name}");
        }
    }

}
