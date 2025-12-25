using System.Collections.Generic;
using System.Threading.Tasks;
using md_to_test_case.Models;

namespace md_to_test_case.Services
{
    public interface IXmindService
    {
        Task GenerateXmindFileAsync(List<TestCase> testCases, string outputPath);
    }
}