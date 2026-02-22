namespace Subchron.API.Services;

public interface ICloudinaryService
{
    Task<string> UploadAvatarOverwriteAsync(
        Stream stream,
        string fileName,
        string folder,
        string publicId,
        CancellationToken ct = default);
}