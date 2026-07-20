using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using SkyCraft.Installer.Models;
using SkyCraft.Shared.Helpers;

namespace SkyCraft.Installer.Services;

public class DownloadService
{
    private readonly HttpClient _httpClient;

    public DownloadService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task DownloadAsync(
        string url,
        string destination,
        ProgressModel progress)
    {
        Logger.Info("Downloading launcher.");
        
        using var response = await _httpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;

        await using var input = await response.Content.ReadAsStreamAsync();
        await using var output = File.Create(destination);

        var buffer = new byte[81920];

        long downloadedBytes = 0;

        while (true)
        {
            var read = await input.ReadAsync(buffer);

            if (read == 0)
                break;

            await output.WriteAsync(buffer.AsMemory(0, read));

            downloadedBytes += read;

            progress.TotalBytes = totalBytes;
            progress.DownloadedBytes = downloadedBytes;

            if (totalBytes > 0)
            {
                progress.Progress =
                    downloadedBytes * 100d / totalBytes;
            }
        }
        
        Logger.Info("Launcher download completed.");
    }
}