using System.Threading.Tasks;

namespace md_to_test_case.Services
{
    public interface IDialogService
    {
        Task ShowErrorAsync(string title, string message);
        Task ShowSuccessAsync(string title, string message);
        Task ShowInfoAsync(string title, string message);
    }
}