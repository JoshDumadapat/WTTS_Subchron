using System.Security.Cryptography;
using QRCoder;

namespace Subchron.API.Services;

public static class AttendanceQrHelper
{
    /// <summary>Generate a crypto-safe URL-safe base64 token for attendance QR. Same employee = same token unless rotated.</summary>
    public static string GenerateAttendanceQrToken(int bytes = 32)
    {
        var buffer = new byte[bytes];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToBase64String(buffer).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    /// <summary>Generate QR code PNG bytes for the given URL (e.g. {WebBaseUrl}/attendance/scan/{token}).</summary>
    public static byte[] GenerateQrPng(string url)
    {
        using var qr = new QRCodeGenerator();
        using var data = qr.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        using var code = new PngByteQRCode(data);
        return code.GetGraphic(4);
    }
}
