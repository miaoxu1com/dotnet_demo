using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia;
using md_to_test_case.ViewModels;
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
            // 创建 ForkAwesome 图标
            var icon = new PackIconForkAwesome
            {
                Kind = PackIconForkAwesomeKind.AngleDoubleRight,
                Width = 32,
                Height = 32,
                Foreground = Avalonia.Media.Brushes.Black
            };
            
            // 渲染图标为位图
            var renderTarget = new RenderTargetBitmap(new PixelSize(32, 32));
            icon.Measure(new Avalonia.Size(32, 32));
            icon.Arrange(new Avalonia.Rect(0, 0, 32, 32));
            renderTarget.Render(icon);
            
            // 创建窗口图标
            Icon = new WindowIcon(renderTarget);
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