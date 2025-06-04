using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Mvvm.DialogToolkit.Dialogs;

namespace Mvvm.DialogToolkit
{
    public static class DialogServiceCollectionExtensionsn
    {
        public static IServiceCollection AddDialogService(this IServiceCollection services)
        {
            services.AddSingleton<IDialogService, DialogService>();
            services.AddTransient<IDialogWindow, DialogWindow>();

            return services;
        }

        public static IServiceCollection RegisterForDialogs<TView, TViewModel>(
            this IServiceCollection services
        )
            where TView : FrameworkElement, new()
            where TViewModel : class
        {
            services.AddTransient<TViewModel>();

            return services.AddTransient(serviceProvider => new TView()
            {
                DataContext = serviceProvider.GetRequiredService<TViewModel>(),
            });
        }

        public static IServiceCollection RegisterMainWindow<TView, TViewModel>(
            this IServiceCollection services
        )
            where TView : FrameworkElement, new()
            where TViewModel : class
        {
            services.AddSingleton<TViewModel>();
            return services.AddSingleton(serviceProvider => new TView()
            {
                DataContext = serviceProvider.GetRequiredService<TViewModel>(),
            });
        }

        public static IServiceCollection RegisterDialogWindow<TWindow>(
            this IServiceCollection services
        )
            where TWindow : Window, IDialogWindow, new()
        {
            return services.AddTransient<IDialogWindow, TWindow>();
        }
    }
}
