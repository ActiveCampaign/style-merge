using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace StyleMerge.Tests.TestCases
{
    public class TestCaseLoader
    {
        private static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

        public async Task<TestData> LoadData(string key)
        {
            var prefix = $"{GetType().Namespace}.{key}";
            var input = await LoadResource($"{prefix}.Input.html");
            var output = await LoadResource($"{prefix}.Output.html");

            return new TestData
            {
                Input = input,
                Output = output
            };
        }

        private async Task<string> LoadResource(string key)
        {
            using var stream = Assembly.GetManifestResourceStream(key);
            using var reader = new StreamReader(stream ?? Stream.Null, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }
    }
}
