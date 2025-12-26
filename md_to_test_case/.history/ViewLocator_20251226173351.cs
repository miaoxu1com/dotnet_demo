using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using md_to_test_case.ViewModels;
using md_to_test_case.Views;

namespace md_to_test_case;

/// <summary>
/// Given a view model, returns the corresponding view if possible.
/// </summary>
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;
        
        // 使用工厂方法直接映射ViewModel到View
        if (param is MainWindowViewModel)
        {
            return new MainWindow();
        }
        
        return new TextBlock { Text = "Not Found: " + param.GetType().FullName };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
