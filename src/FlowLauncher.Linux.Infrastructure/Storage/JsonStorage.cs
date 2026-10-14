using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FlowLauncher.Linux.Infrastructure.Storage;

public class JsonStorage<T> where T : class, new()
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    public JsonStorage(string filePath) => _filePath = filePath;

    public T Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return new T();
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<T>(json, _options) ?? new T();
        }
        catch
        {
            return new T();
        }
    }

    public async Task<T> LoadAsync()
    {
        try
        {
            if (!File.Exists(_filePath)) return new T();
            await using var stream = File.OpenRead(_filePath);
            return await JsonSerializer.DeserializeAsync<T>(stream, _options) ?? new T();
        }
        catch
        {
            return new T();
        }
    }

    public void Save(T data)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(data, _options);
        File.WriteAllText(_filePath, json);
    }

    public async Task SaveAsync(T data)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, data, _options);
    }
}
