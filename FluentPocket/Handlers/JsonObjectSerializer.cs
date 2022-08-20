using Microsoft.Toolkit.Helpers;
using System.Text.Json;

namespace FluentPocket.Handlers
{
    public class JsonObjectSerializer : IObjectSerializer
    {
        public string Serialize<T>(T value) => JsonSerializer.Serialize(value);

        public T Deserialize<T>(string value) => JsonSerializer.Deserialize<T>(value);
    }
}
