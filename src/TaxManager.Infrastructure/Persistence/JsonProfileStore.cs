using System.Text.Json;
using System.Text.Json.Serialization;
using TaxManager.Application.Abstractions;
using TaxManager.Application.Models;

namespace TaxManager.Infrastructure.Persistence;

/// <summary>
/// Persistenza dei profili contribuente su disco in un singolo file JSON.
/// Le scritture sono serializzate (lock) e atomiche (scrittura su file temporaneo + sostituzione).
/// </summary>
public sealed class JsonProfileStore : IProfileStore
{
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public JsonProfileStore(string path) => _path = path;

    public string StorePath => _path;

    public async Task<IReadOnlyList<SavedProfile>> LoadAllAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try { return await LoadInternalAsync(ct); }
        finally { _gate.Release(); }
    }

    public async Task SaveAsync(SavedProfile profile, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var list = await LoadInternalAsync(ct);
            profile.UpdatedUtc = DateTime.UtcNow;
            var idx = list.FindIndex(p => p.Id == profile.Id);
            if (idx >= 0) list[idx] = profile;
            else list.Add(profile);
            await WriteInternalAsync(list, ct);
        }
        finally { _gate.Release(); }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var list = await LoadInternalAsync(ct);
            list.RemoveAll(p => p.Id == id);
            await WriteInternalAsync(list, ct);
        }
        finally { _gate.Release(); }
    }

    private async Task<List<SavedProfile>> LoadInternalAsync(CancellationToken ct)
    {
        if (!File.Exists(_path)) return new List<SavedProfile>();
        await using var fs = File.OpenRead(_path);
        var list = await JsonSerializer.DeserializeAsync<List<SavedProfile>>(fs, JsonOptions, ct);
        return list ?? new List<SavedProfile>();
    }

    private async Task WriteInternalAsync(List<SavedProfile> list, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var tmp = _path + ".tmp";
        await using (var fs = File.Create(tmp))
            await JsonSerializer.SerializeAsync(fs, list, JsonOptions, ct);
        File.Copy(tmp, _path, overwrite: true);
        File.Delete(tmp);
    }
}
