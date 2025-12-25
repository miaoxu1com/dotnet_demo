using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia;
using md_to_test_case.ViewModels;
using System;
using System.Linq;
using System.Reflection;

namespace md_to_test_case.Views;

public partial class MainWindow : Window
{
    public MainWindowViewModel ViewModel { get; }

    public MainWindow()
    {
        ViewModel = new MainWindowViewModel();
        DataContext = ViewModel;
        
        InitializeComponent();
        
        // 设置窗口图标
        SetWindowIcon();
        
        // 添加拖拽事件处理（使用路由事件，确保整个窗口都能接收拖拽）
        AddHandler(DragDrop.DropEvent, OnDrop, RoutingStrategies.Bubble);
        AddHandler(DragDrop.DragOverEvent, OnDragOver, RoutingStrategies.Bubble);
    }

    private void SetWindowIcon()
    {
        try
        {
            // 使用反射创建 ForkAwesome 图标（因为命名空间可能不同）
            var forkAwesomeAssembly = Assembly.Load("IconPacks.Avalonia.ForkAwesome");
            var iconType = forkAwesomeAssembly.GetType("IconPacks.Avalonia.ForkAwesome.PackIconForkAwesome");
            var kindType = forkAwesomeAssembly.GetType("IconPacks.Avalonia.ForkAwesome.PackIconForkAwesomeKind");
            
            if (iconType != null && kindType != null)
            {
                var icon = Activator.CreateInstance(iconType);
                
                // 设置属性
                var widthProp = iconType.GetProperty("Width");
                var heightProp = iconType.GetProperty("Height");
                var foregroundProp = iconType.GetProperty("Foreground");
                var kindProp = iconType.GetProperty("Kind");
                
                widthProp?.SetValue(icon, 32.0);
                heightProp?.SetValue(icon, 32.0);
                foregroundProp?.SetValue(icon, Avalonia.Media.Brushes.Black);
                
                // 设置 Kind 枚举值
                var angleDoubleRight = Enum.Parse(kindType, "AngleDoubleRight");
                kindProp?.SetValue(icon, angleDoubleRight);
                
                // 渲染图标为位图
                var renderTarget = new RenderTargetBitmap(new PixelSize(32, 32));
                var measureMethod = iconType.GetMethod("Measure", new[] { typeof(Avalonia.Size) });
                var arrangeMethod = iconType.GetMethod("Arrange", new[] { typeof(Avalonia.Rect) });
                
                measureMethod?.Invoke(icon, new object[] { new Avalonia.Size(32, 32) });
                arrangeMethod?.Invoke(icon, new object[] { new Avalonia.Rect(0, 0, 32, 32) });
                renderTarget.Render((Avalonia.Visual)icon);
                
                // 创建窗口图标
                Icon = new WindowIcon(renderTarget);
            }
        }
        catch
        {
            // 如果设置图标失败，使用默认图标或忽略
        }
    }

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

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        // 只允许文件拖拽 - 使用新的 DataTransfer API
        if (e.DataTransfer.Contains(DataFormat.File))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        // 获取拖拽的文件 - 使用 Data 属性（虽然已过时，但在 Avalonia 11.3.9 中仍然可用）
        var files = e.Data.GetFiles()?.Select(f => f.Path.LocalPath).ToArray();
        if (files != null && files.Length > 0)
        {
            await ViewModel.HandleFileDropCommand.ExecuteAsync(files);
        }
    }
}