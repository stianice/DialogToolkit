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
    }
}
