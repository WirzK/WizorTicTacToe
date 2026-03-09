using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System;
using System.Reflection;

[RequireComponent(typeof(Button))]
public class CustomizedBtn : MonoBehaviour
{
    [Header("单例配置")]
    [Tooltip("单例类")]
    public string singletonClassName = "SaveManager";
    [Tooltip("单例中的函数名")]
    public string targetFunctionName = "ConfirmDeleteShared";

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_button == null)
        {
            Debug.LogError($"[{name}] 未找到Button组件");
            return;
        }

        BindButtonToSingletonFunction();
    }

    private void BindButtonToSingletonFunction()
    {
        Type singletonType = Type.GetType(singletonClassName);
        if (singletonType == null)
        {
            Debug.LogError($"[{name}] 未找到单例类：{singletonClassName}，请检查类名是否正确！");
            return;
        }

        FieldInfo instanceField = singletonType.GetField("instance", BindingFlags.Public | BindingFlags.Static);
        if (instanceField == null)
        {
            Debug.LogError($"[{name}] 单例类{singletonClassName}未找到public static的instance字段！");
            return;
        }

        object singletonInstance = instanceField.GetValue(null);
        if (singletonInstance == null)
        {
            Debug.LogError($"[{name}] 单例{singletonClassName}的instance为空！");
            return;
        }

        MethodInfo targetMethod = singletonType.GetMethod(
            targetFunctionName,
            BindingFlags.Public | BindingFlags.Instance,
            Type.DefaultBinder,
            Type.EmptyTypes,
            null
        );

        if (targetMethod == null)
        {
            Debug.LogError($"[{name}] 单例{singletonClassName}中未找到无参的public函数：{targetFunctionName}！");
            return;
        }

        UnityAction onClickAction = () => targetMethod.Invoke(singletonInstance, null);
        _button.onClick.AddListener(onClickAction);

        Debug.Log($"[{name}] 按钮已成功绑定到 {singletonClassName}.{targetFunctionName}()");
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
        }
    }
}