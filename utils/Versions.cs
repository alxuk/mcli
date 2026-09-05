using System.Text.Json;

namespace mcli.utils;

public static class Versions
{
    public static async Task<JsonDocument> FetchVersions()
    {
        using var http = new HttpClient();
        string manifestJson = await http.GetStringAsync(MojangServer.VersionsManifest);

        return JsonDocument.Parse(manifestJson);
    }
}