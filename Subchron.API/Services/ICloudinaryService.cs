namespace Subchron.API.Services;

public interface ICloudinaryService
{
    Task<string> UploadAvatarOverwriteAsync(
        Stream stream,
        string fileName,
        string folder,
        string publicId,
        CancellationToken ct = default);

    /// <summary>Upload an image to a folder with a unique public ID. Returns the secure URL.</summary>
    Task<string> UploadImageAsync(
        Stream stream,
        string fileName,
        string folder,
        string publicId,
        CancellationToken ct = default);
}