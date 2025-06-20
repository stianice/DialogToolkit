using System.Text;
using System.Windows.Threading;

namespace Mvvm.DialogToolkit.Helper;

public static class UIDispatcherHelper
{
    /// <summary>
    /// Gets a reference to the UI thread's dispatcher, after the
    /// <see cref="M:GalaSoft.MvvmLight.Threading.UIDispatcherHelper.Initialize" /> method has been called on the UI thread.
    /// </summary>
    public static Dispatcher UIDispatcher { get; private set; }

    /// <summary>
    /// Executes an action on the UI thread. If this method is called
    /// from the UI thread, the action is executed immendiately. If the
    /// method is called from another thread, the action will be enqueued
    /// on the UI thread's dispatcher and executed asynchronously.
    /// <para>For additional operations on the UI thread, you can get a
    /// reference to the UI thread's dispatcher thanks to the property
    /// <see cref="P:GalaSoft.MvvmLight.Threading.UIDispatcherHelper.UIDispatcher" /></para>.
    /// </summary>
    /// <param name="action">The action that will be executed on the UI
    /// thread.</param>
    public static void CheckBeginInvokeOnUI(Action action)
    {
        if (action != null)
        {
            CheckDispatcher();
            if (UIDispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                UIDispatcher.BeginInvoke(action);
            }
        }
    }

    private static void CheckDispatcher()
    {
        if (UIDispatcher == null)
        {
            var stringBuilder = new StringBuilder("The UIDispatcherHelper is not initialized.");
            stringBuilder.AppendLine();
            stringBuilder.Append(
                "Call UIDispatcherHelper.Initialize() in the static App constructor."
            );
            throw new InvalidOperationException(stringBuilder.ToString());
        }
    }

    /// <summary>
    /// Invokes an action asynchronously on the UI thread.
    /// </summary>
    /// <param name="action">The action that must be executed.</param>
    /// <returns>An object, which is returned immediately after BeginInvoke is called, that can be used to interact
    ///  with the delegate as it is pending execution in the event queue.</returns>
    public static DispatcherOperation BeginInvoke(Delegate action)
    {
        CheckDispatcher();
        return UIDispatcher.BeginInvoke(action);
    }

    public static DispatcherOperation InvokeAsync(Action method)
    {
        CheckDispatcher();
        return UIDispatcher.InvokeAsync(method);
    }

    public static DispatcherOperation<TResult> InvokeAsync<TResult>(Func<TResult> callback)
    {
        CheckDispatcher();
        return UIDispatcher.InvokeAsync(callback);
    }

    public static void Invoke(Action method)
    {
        CheckDispatcher();
        UIDispatcher.Invoke(method);
    }

    /// <summary>
    /// This method should be called once on the UI thread to ensure that
    /// the <see cref="P:GalaSoft.MvvmLight.Threading.UIDispatcherHelper.UIDispatcher" /> property is initialized.
    /// <para>In a Silverlight application, call this method in the
    /// Application_Startup event handler, after the MainPage is constructed.</para>
    /// <para>In WPF, call this method on the static App() constructor.</para>
    /// </summary>
    public static void Initialize()
    {
        if (UIDispatcher == null || !UIDispatcher.Thread.IsAlive)
        {
            UIDispatcher = Dispatcher.CurrentDispatcher;
        }
    }

    /// <summary>
    /// Resets the class by deleting the <see cref="P:GalaSoft.MvvmLight.Threading.UIDispatcherHelper.UIDispatcher" />
    /// </summary>
    public static void Reset()
    {
        UIDispatcher = null;
    }
}
