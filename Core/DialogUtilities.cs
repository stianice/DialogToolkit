using System.ComponentModel;
using System.Reflection;
using System.Windows.Threading;

namespace Mvvm.DialogToolkit.Dialogs;

/// <summary>
/// Provides utilities for the Dialog Service to be able to reuse
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class DialogUtilities
{
    /// <summary>
    /// Initializes <see cref="IDialogAware.RequestClose"/>
    /// </summary>
    /// <param name="dialogAware"></param>
    /// <param name="callback"></param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void InitializeListener(
        IDialogAware dialogAware,
        Func<IDialogResult, Task> callback
    )
    {
        var listener = new DialogCloseListener(callback);
        SetListener(dialogAware, listener);
    }

    /// <summary>
    /// 初始化 IDialogAware.Dispatcher
    /// </summary>
    /// <param name="dialogAware">目标对话框实例</param>
    /// <param name="dispatcher">要设置的 Dispatcher</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void InitializeDispatcher(IDialogAware dialogAware, Dispatcher dispatcher)
    {
        if (dialogAware == null)
            throw new ArgumentNullException(nameof(dialogAware));

        if (dispatcher == null)
            throw new ArgumentNullException(nameof(dispatcher));

        var setter = GetDispatcherSetter(dialogAware, dialogAware.GetType());
        setter(dispatcher);
    }

    private static Action<Dispatcher> GetDispatcherSetter(IDialogAware dialogAware, Type type)
    {
        // 1. 优先检查属性
        var propInfo = type.GetProperty(nameof(IDialogAware.Dispatcher));

        if (
            propInfo != null
            && propInfo.PropertyType == typeof(Dispatcher)
            && propInfo.SetMethod != null
        )
        {
            return x => propInfo.SetValue(dialogAware, x);
        }

        // 2. 检查后备字段
        var fields = type.GetRuntimeFields().Where(x => x.FieldType == typeof(Dispatcher)).ToList();

        // 2.1 查找标准命名后备字段
        var backingField = fields.FirstOrDefault(x =>
            x.Name == $"<{nameof(IDialogAware.Dispatcher)}>k__BackingField"
        );

        if (backingField != null)
        {
            return x => backingField.SetValue(dialogAware, x);
        }

        // 2.2 查找常见命名模式的字段
        var commonField = fields.FirstOrDefault(x =>
            x.Name.Equals("_dispatcher", StringComparison.OrdinalIgnoreCase)
            || x.Name.Equals("dispatcher", StringComparison.OrdinalIgnoreCase)
        );

        if (commonField != null)
        {
            return x => commonField.SetValue(dialogAware, x);
        }

        // 2.3 使用第一个匹配类型的字段
        if (fields.Count > 0)
        {
            return x => fields[0].SetValue(dialogAware, x);
        }

        // 3. 检查基类
        var baseType = type.BaseType;
        if (baseType == null || baseType == typeof(object))
            throw new InvalidOperationException("无法设置 Dispatcher: 未找到可用的属性或字段");

        return GetDispatcherSetter(dialogAware, baseType);
    }

    /// <summary>
    /// Initializes <see cref="IDialogAware.RequestClose"/>
    /// </summary>
    /// <param name="dialogAware"></param>
    /// <param name="callback"></param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void InitializeListener(IDialogAware dialogAware, Action<IDialogResult> callback)
    {
        var listener = new DialogCloseListener(callback);
        SetListener(dialogAware, listener);
    }

    private static void SetListener(IDialogAware dialogAware, DialogCloseListener listener)
    {
        var setter = GetListenerSetter(dialogAware, dialogAware.GetType());
        setter(listener);
    }

    private static Action<DialogCloseListener> GetListenerSetter(
        IDialogAware dialogAware,
        Type type
    )
    {
        var propInfo = type.GetProperty(nameof(IDialogAware.RequestClose));

        if (
            propInfo is not null
            && propInfo.PropertyType == typeof(DialogCloseListener)
            && propInfo.SetMethod is not null
        )
        {
            return x => propInfo.SetValue(dialogAware, x);
        }

        var fields = type.GetRuntimeFields().Where(x => x.FieldType == typeof(DialogCloseListener));
        var field = fields.FirstOrDefault(x =>
            x.Name == $"<{nameof(IDialogAware.RequestClose)}>k__BackingField"
        );
        if (field is not null)
        {
            return x => field.SetValue(dialogAware, x);
        }
        else if (fields.Any())
        {
            field = fields.First();
            return x => field.SetValue(dialogAware, x);
        }

        var baseType = type.BaseType;
        if (baseType is null || baseType == typeof(object))
            throw new DialogException(DialogException.UnableToSetTheDialogCloseListener);

        return GetListenerSetter(dialogAware, baseType);
    }
}
