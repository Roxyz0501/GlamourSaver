using System.ComponentModel;
using System.Runtime.InteropServices;
using GlamourSaver.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace GlamourSaver.Services;

public sealed class ScreenCaptureService(LocalizationService localization)
{
    private const int Srccopy = 0x00CC0020;
    private const int DibRgbColors = 0;

    public byte[] CapturePng(ScreenRegion region)
    {
        if (!region.IsValid)
            throw new ArgumentOutOfRangeException(nameof(region));

        var screen = GetDC(IntPtr.Zero);
        var memory = CreateCompatibleDC(screen);
        var bitmap = CreateCompatibleBitmap(screen, region.Width, region.Height);
        var previous = SelectObject(memory, bitmap);
        try
        {
            if (!BitBlt(memory, 0, 0, region.Width, region.Height, screen, region.X, region.Y, Srccopy))
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    localization.Text("Failed to capture the game window.", "ゲーム画面を取得できませんでした。"));

            var info = new BitmapInfo
            {
                Header = new BitmapInfoHeader
                {
                    Size = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
                    Width = region.Width,
                    Height = -region.Height,
                    Planes = 1,
                    BitCount = 32,
                    Compression = 0,
                },
            };
            var pixels = new byte[checked(region.Width * region.Height * 4)];
            if (GetDIBits(memory, bitmap, 0, (uint)region.Height, pixels, ref info, DibRgbColors) == 0)
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    localization.Text("Failed to read the captured image pixels.", "画像ピクセルを読み出せませんでした。"));

            using var image = Image.LoadPixelData<Bgra32>(pixels, region.Width, region.Height);
            using var output = new MemoryStream();
            image.Save(output, new PngEncoder { CompressionLevel = PngCompressionLevel.Level6 });
            return output.ToArray();
        }
        finally
        {
            SelectObject(memory, previous);
            DeleteObject(bitmap);
            DeleteDC(memory);
            ReleaseDC(IntPtr.Zero, screen);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ClrUsed;
        public uint ClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public BitmapInfoHeader Header;
        public uint Colors;
    }

    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int width, int height);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool BitBlt(IntPtr dst, int x, int y, int width, int height, IntPtr src, int srcX, int srcY, int rop);
    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern int GetDIBits(IntPtr dc, IntPtr bitmap, uint start, uint lines, byte[] bits, ref BitmapInfo info, uint usage);
}
