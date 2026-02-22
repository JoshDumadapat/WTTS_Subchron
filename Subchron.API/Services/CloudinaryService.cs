using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using Subchron.API.Models.Settings;

namespace Subchron.API.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IOptions<CloudinarySettings> opts)
    {
        var s = opts.Value;

        if (string.IsNullOrWhiteSpace(s.CloudName) ||
            string.IsNullOrWhiteSpace(s.ApiKey) ||
            string.IsNullOrWhiteSpace(s.ApiSecret))
        {
            throw new InvalidOperationException("Cloudinary settings are missing.");
        }

        _cloudinary = new Cloudinary(new Account(s.CloudName, s.ApiKey, s.ApiSecret))
        {
            Api = { Secure = true }
        };
    }

    public async Task<string> UploadAvatarOverwriteAsync(
        Stream stream,
        string fileName,
        string folder,
        string publicId,
        CancellationToken ct = default)
    {
        // Key part: same public id + overwrite = replaces old
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, stream),
            Folder = folder,
            PublicId = publicId,
            Overwrite = true,
            Invalidate = true
        };

        var result = await _cloudinary.UploadAsync(uploadParams, ct);

        if (result.Error != null)
            throw new Exception("Cloudinary upload failed: " + result.Error.Message);

        var url = result.SecureUrl?.ToString();
        if (string.IsNullOrWhiteSpace(url))
            throw new Exception("Cloudinary upload failed: secure URL missing.");

        return url;
    }
}