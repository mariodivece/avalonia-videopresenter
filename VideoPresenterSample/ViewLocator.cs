using Avalonia.Controls;
using Avalonia.Controls.Templates;
using VideoPresenterSample.ViewModels;
using System;

namespace VideoPresenterSample
{
    public class ViewLocator : IDataTemplate
    {
        public Control Build(object? data)
        {
            var typeName = data?.GetType().FullName!.Replace("ViewModel", "View");
            var type = Type.GetType(typeName ?? string.Empty);

            return type is not null
                ? (Control)Activator.CreateInstance(type)!
                : new TextBlock { Text = "Not Found: " + typeName };
        }

        public bool Match(object? data) => data is ViewModelBase;
    }
}