using System.Text.Json;
using System.Text.Json.Serialization;

namespace Helpdesk.Tests.Integration
{
    public static class TestJsonOptions
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters =
        {
            new JsonStringEnumConverter()
        }
        };
    }
}
