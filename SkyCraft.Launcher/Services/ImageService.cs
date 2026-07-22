using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;


namespace SkyCraft.Launcher.Services;

public class ImageService
{

    private readonly HttpClient _httpClient;

    public ImageService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    

    public async Task<Bitmap?> LoadAsync(string url)
    {
        try
        {

            var bytes = await _httpClient.GetByteArrayAsync(url);

            using var stream = new MemoryStream(bytes);
            return new Bitmap(stream);
        }
        catch  (Exception e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }
    
}