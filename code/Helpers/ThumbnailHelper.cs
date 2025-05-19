using SkiaSharp;

namespace redis_aspnet_core_example.Helpers;

public class ThumbnailHelper
{
    public static byte[] CreateSquareThumbnail(byte[] imageData, int maxDimension = 280, int jpegQuality = 90)
    {
        // Decode original into SKBitmap
        using var original = SKBitmap.Decode(imageData);
        if (original == null)
            throw new ArgumentException("Invalid image data", nameof(imageData));

        int srcW = original.Width;
        int srcH = original.Height;

        // Compute source crop rect (centered square)
        SKRectI srcRect;
        if (srcW >= srcH)
        {
            int offsetX = (srcW - srcH) / 2;
            srcRect = new SKRectI(offsetX, 0, offsetX + srcH, srcH);
        }
        else
        {
            int offsetY = (srcH - srcW) / 2;
            srcRect = new SKRectI(0, offsetY, srcW, offsetY + srcW);
        }

        // Destination is always maxDimension × maxDimension
        var info = new SKImageInfo(maxDimension, maxDimension, SKColorType.Rgba8888, SKAlphaType.Premul);

        // Render onto a new surface
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        // Draw the cropped region, scaled into the square
        var destRect = new SKRect(0, 0, maxDimension, maxDimension);
        canvas.DrawBitmap(original, srcRect, destRect, new SKPaint
        {
            FilterQuality = SKFilterQuality.High,
            IsAntialias = true,
            IsDither = true
        });

        // Snapshot as an SKImage and encode to JPEG
        using var img = surface.Snapshot();
        using var data = img.Encode(SKEncodedImageFormat.Jpeg, jpegQuality);

        return data.ToArray();
    }
}