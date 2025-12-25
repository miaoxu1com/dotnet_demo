using Avalonia.Controls;
using System;
using System.Linq;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace md_to_test_case.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        Console.WriteLine("[DEBUG] MainWindow ctor: InitializeComponent start");
        InitializeComponent();
        Console.WriteLine("[DEBUG] MainWindow ctor: InitializeComponent end");
    }

    public System.Collections.Generic.IEnumerable<string> DisplayChars => "拖拽上传".ToCharArray().Select(c => c.ToString());

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MinButton_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}