using System.Security.Cryptography;

namespace SkyCraft.Shared.Helpers;

public static class HashHelper
{
    public static async Task<string> CalculateSha256Async(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found.", filePath);

        await using var stream = File.OpenRead(filePath);

        using var sha256 = SHA256.Create();

        var hash = await sha256.ComputeHashAsync(stream);

        return Convert.ToHexString(hash);
    }

    public static async Task<bool> VerifySha256Async(
        string filePath,
        string expectedHash)
    {
        if (!File.Exists(filePath))
            return false;

        var hash = await CalculateSha256Async(filePath);

        return hash.Equals(
            expectedHash,
            StringComparison.OrdinalIgnoreCase);
    }
}