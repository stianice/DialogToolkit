using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace Mvvm.DialogToolkit.Dialogs;

/// <summary>
/// Implements <see cref="IDialogService"/> to show modal and non-modal dialogs.
/// </summary>
/// <remarks>
/// The dialog's ViewModel must implement IDialogAware.
/// </remarks>
public class DialogService : IDialogService
{
    private readonly IServiceProvider _containerExtension;

    /// <summary>
    /// Initializes a new instance of the <see cref="DialogService"/> class.
    /// </summary>
    /// <param name="containerExtension">The <see cref="IContainerExtension" /></param>
    public DialogService(IServiceProvider containerExtension)
    {
        _containerExtension = containerExtension;
    }

    /// <summary>
    /// Shows a modal dialog.
    /// </summary>
    /// <param name="name">The name of the dialog to show.</param>
    /// <param name="parameters">The parameters to pass to the dialog.</param>
    /// <param name="callback">The action to perform when the dialog is closed.</param>
    public void ShowDialog(Type name, IDialogParameters parameters, DialogCallback callback)
    {
        parameters ??= new DialogParameters();
        var isModal =
            !parameters.TryGetValue<bool>(KnownDialogParameters.ShowNonModal, out var show)
            || !show;
        var windowName = parameters.TryGetValue<string>(
            KnownDialogParameters.WindowName,
            out var wName
        )
            ? wName
            : null;

        var dialogWindow = CreateDialogWindow(windowName);
        ConfigureDialogWindowEvents(dialogWindow, callback);
        ConfigureDialogWindowContent(name, dialogWindow, parameters);

        ShowDialogWindow(dialogWindow, isModal);
    }

    /// <summary>
    /// Shows the dialog window.
    /// </summary>
    /// <param name="dialogWindow">The dialog window to show.</param>
    /// <param name="isModal">If true; dialog is shown as a modal</param>
    protected virtual void ShowDialogWindow(IDialogWindow dialogWindow, bool isModal)
    {
        if (isModal)
        {
            dialogWindow.ShowDialog();
        }
        else
        {
            dialogWindow.Show();
        }
    }

    /// <summary>
    /// Create a new <see cref="IDialogWindow"/>.
    /// </summary>
    /// <param name="name">The name of the hosting window registered with the IContainerRegistry.</param>
    /// <returns>The created <see cref="IDialogWindow"/>.</returns>
    protected virtual IDialogWindow CreateDialogWindow(string name)
    {
        // if (string.IsNullOrWhiteSpace(name))
        return _containerExtension.GetRequiredService<IDialogWindow>();
        /*  else
              return _containerExtension.GetRequiredKeyedService<IDialogWindow>(name);*/
    }

    /// <summary>
    /// Configure <see cref="IDialogWindow"/> content.
    /// </summary>
    /// <param name="dialogName">The name of the dialog to show.</param>
    /// <param name="window">The hosting window.</param>
    /// <param name="parameters">The parameters to pass to the dialog.</param>
    protected virtual void ConfigureDialogWindowContent(
        Type type,
        IDialogWindow window,
        IDialogParameters parameters
    )
    {
        var content = _containerExtension.GetRequiredService(type);
        if (content is not FrameworkElement dialogContent)
        {
            throw new NullReferenceException("A dialog's content must be a FrameworkElement");
        }

        if (dialogContent.DataContext is not IDialogAware viewModel)
        {
            throw new NullReferenceException(
                "A dialog's ViewModel must implement the IDialogAware interface"
            );
        }

        ConfigureDialogWindowProperties(window, dialogContent, viewModel);

        viewModel.OnDialogOpened(parameters);
    }

    /// <summary>
    /// Configure <see cref="IDialogWindow"/> and <see cref="IDialogAware"/> events.
    /// </summary>
    /// <param name="dialogWindow">The hosting window.</param>
    /// <param name="callback">The action to perform when the dialog is closed.</param>
    protected virtual void ConfigureDialogWindowEvents(
        IDialogWindow dialogWindow,
        DialogCallback callback
    )
    {
        Action<IDialogResult> requestCloseHandler = (r) =>
        {
            dialogWindow.Result = r;
            dialogWindow.Close();
        };

        RoutedEventHandler loadedHandler = null;
        loadedHandler = (o, e) =>
        {
            //加载完成后，移除事件处理器，避免重复绑定，对viewmodel设置监听器
            dialogWindow.Loaded -= loadedHandler;
            DialogUtilities.InitializeListener(
                dialogWindow.GetDialogViewModel(),
                requestCloseHandler //回调关闭窗口，并将viewmodel dialog结果传递窗体
            );
        };
        dialogWindow.Loaded += loadedHandler;

        CancelEventHandler closingHandler = null;
        closingHandler = (o, e) =>
        {
            //关闭前。检查viewmodel是否允许关闭窗口
            if (!dialogWindow.GetDialogViewModel().CanCloseDialog())
            {
                e.Cancel = true;
            }
        };
        dialogWindow.Closing += closingHandler;

        EventHandler closedHandler = null;
        closedHandler = async (o, e) =>
        {
            //关闭后，移除所有事件处理器
            dialogWindow.Closed -= closedHandler;
            dialogWindow.Closing -= closingHandler;

            dialogWindow.GetDialogViewModel().OnDialogClosed();

            //如果viewmodel调用没有设置结果，则设置一个默认的结果
            dialogWindow.Result ??= new DialogResult();

            await callback.Invoke(dialogWindow.Result);

            dialogWindow.DataContext = null!;
            dialogWindow.Content = null!;
        };
        dialogWindow.Closed += closedHandler;
    }

    /// <summary>
    /// Configure <see cref="IDialogWindow"/> properties.
    /// </summary>
    /// <param name="window">The hosting window.</param>
    /// <param name="dialogContent">The dialog to show.</param>
    /// <param name="viewModel">The dialog's ViewModel.</param>
    protected virtual void ConfigureDialogWindowProperties(
        IDialogWindow window,
        FrameworkElement dialogContent,
        IDialogAware viewModel
    )
    {
        var windowStyle = Dialog.GetWindowStyle(dialogContent);
        if (windowStyle != null)
        {
            window.Style = windowStyle;
        }

        window.Content = dialogContent;
        window.DataContext = viewModel; //we want the host window and the dialog to share the same data context
#pragma warning disable CS8601 // 引用类型赋值可能为 null。
        window.Owner ??= Application
            .Current?.Windows.OfType<Window>()
            .FirstOrDefault(x => x.IsActive);
#pragma warning restore CS8601 // 引用类型赋值可能为 null。
    }
}
