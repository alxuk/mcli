using System.Text.Json;

namespace mcli.utils;

public static class Install
{

    private static async Task<JsonDocument> FetchJsonFromUrl(string url)
    {
        using var http = new HttpClient();
        string Json = await http.GetStringAsync(url);
        return JsonDocument.Parse(Json);
    }
    public static async Task<string> FetchJarUrl(string url)
    {
        JsonDocument JsonD = await FetchJsonFromUrl(url);
        string JarUrl = JsonD.RootElement.GetProperty("downloads").GetProperty("client").GetProperty("url").GetString() ?? throw new Exception();
        return JarUrl;
    }

    public static async Task<JsonElement> FetchLibraries(string url)
    {
        JsonDocument JsonD = await FetchJsonFromUrl(url);
        JsonElement libraries = JsonD.RootElement.GetProperty("libraries");
        return libraries;
    }

    public static async Task<JsonDocument> FetchAssets(string url)
    {
        JsonDocument JsonD = await FetchJsonFromUrl(url);
        JsonElement assetIndex = JsonD.RootElement.GetProperty("assetIndex");
        string assetUrl = assetIndex.GetProperty("url").GetString() ?? throw new Exception();

        JsonDocument AssetJsonD = await FetchJsonFromUrl(assetUrl);
        return AssetJsonD;
    }

    public static async Task<string> FetchAssetIndexId(string url)
    {
        JsonDocument JsonD = await FetchJsonFromUrl(url);
        JsonElement assetIndex = JsonD.RootElement.GetProperty("assetIndex");
        return assetIndex.GetProperty("id").GetString() ?? throw new Exception();
    }
}