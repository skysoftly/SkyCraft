using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SkyCraft.Launcher.Services.Minecraft;

public class ServerService
{
    private readonly HttpClient _httpClient;

    public ServerService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    public async Task<int> GetOnlineAsync(string host, int port = 25565)
    {
        try
        {
            string url = $"https://api.mcsrvstat.us/2/{host}:{port}";
            string json = await _httpClient.GetStringAsync(url);

            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            if (root.GetProperty("online").GetBoolean())
            {
                return root.GetProperty("players").GetProperty("online").GetInt32();
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }
}