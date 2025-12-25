using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;

namespace md_to_test_case.Services
{
    public class DialogService : IDialogService
    {
        private Window GetMainWindow()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow;
            }
            return null;
        }

        public async Task ShowErrorAsync(string title, string message)
        {
            var window = GetMainWindow();
            if (window != null)
            {
                Window dialog = null;
                
                var button = new Button 
                { 
                    Content = "确定", 
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Margin = new Avalonia.Thickness(0, 20, 0, 0)
                };
                
                button.Click += (s, e) => dialog?.Close();
                
                dialog = new Window
                {
                    Title = title,
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new StackPanel
                    {
                        Margin = new Avalonia.Thickness(20),
                        Children =
                        {
                            new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                            button
                        }
                    }
                };
                
                await dialog.ShowDialog(window);
            }
        }

        public async Task ShowSuccessAsync(string title, string message)
        {
            await ShowInfoAsync(title, message);
        }

        public async Task ShowInfoAsync(string title, string message)
        {
            var window = GetMainWindow();
            if (window != null)
            {
                Window dialog = null;
                
                var button = new Button 
                { 
                    Content = "确定", 
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Margin = new Avalonia.Thickness(0, 20, 0, 0)
                };
                
                button.Click += (s, e) => dialog?.Close();
                
                dialog = new Window
                {
                    Title = title,
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new StackPanel
                    {
                        Margin = new Avalonia.Thickness(20),
                        Children =
                        {
                            new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                            button
                        }
                    }
                };
                
                await dialog.ShowDialog(window);
            }
        }
    }


}