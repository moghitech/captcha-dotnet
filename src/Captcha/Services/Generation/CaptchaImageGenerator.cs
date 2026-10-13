using System.Runtime.InteropServices;
using Captcha.Options;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace Captcha.Helpers;

/// <summary>
/// Renders captcha text into a distorted PNG. Stateless after construction, so register it as a
/// singleton. Visual jitter uses Random.Shared on purpose: it carries no secret. The ANSWER itself
/// comes from a CSPRNG (see CaptchaManager).
/// </summary>
public sealed class CaptchaImageGenerator : IDisposable
{
    private readonly int _w;
    private readonly int _h;
    private readonly SKTypeface[] _typefaces;

    public CaptchaImageGenerator(IOptions<CaptchaOptions> options)
    {
        var o = options.Value;
        _w = o.Width;
        _h = o.Height;

        var dir = Path.IsPathRooted(o.FontsDirectory)
            ? o.FontsDirectory
            : Path.Combine(AppContext.BaseDirectory, o.FontsDirectory);

        // Load fonts from files you ship with the app. Do NOT rely on system fonts: slim
        // containers usually have none. Use permissively licensed bold faces (DejaVu Sans Bold,
        // Liberation Sans Bold, Noto Sans Bold, ...) and keep their licence files.
        _typefaces = Directory.Exists(dir)
            ? Directory.EnumerateFiles(dir, "*.ttf")
                .Select(p => SKTypeface.FromFile(p))
                .Where(t => t != null)
                .ToArray()
            : Array.Empty<SKTypeface>();

        if (_typefaces.Length == 0)
            throw new InvalidOperationException($"No .ttf fonts found in '{dir}'. Ship 2-4 bold fonts with the app.");
    }

    public byte[] Render(string text)
    {
        var info = new SKImageInfo(_w, _h, SKColorType.Rgba8888, SKAlphaType.Opaque);

        // Pass 1: background, some curves behind the text, then the glyphs.
        using var flat = new SKBitmap(info);
        using (var canvas = new SKCanvas(flat))
        {
            DrawBackground(canvas);
            DrawCurves(canvas, 2);
            DrawGlyphs(canvas, text);
        }

        // Pass 2: wave-warp the whole picture so glyph shapes bend, then draw noise ON TOP of
        // the warped text (strokes crossing glyphs make segmentation harder).
        using var warped = new SKBitmap(info);
        Warp(flat, warped);
        using (var canvas = new SKCanvas(warped))
        {
            DrawCurves(canvas, 3);
            DrawSpeckles(canvas, 150);
        }

        using var image = SKImage.FromBitmap(warped);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private void DrawBackground(SKCanvas canvas)
    {
        using var shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0), new SKPoint(_w, _h),
            new[] { Rgb(200, 245), Rgb(200, 245) },
            SKShaderTileMode.Clamp);
        using var paint = new SKPaint { Shader = shader };
        canvas.DrawRect(0, 0, _w, _h, paint);
    }

    private void DrawGlyphs(SKCanvas canvas, string text)
    {
        const float pad = 10f;
        var size = _h * 0.62f;
        var slot = (_w - 2 * pad) / text.Length;     // each glyph is centred in its slot, then jittered

        for (var i = 0; i < text.Length; i++)
        {
            var typeface = _typefaces[Random.Shared.Next(_typefaces.Length)];
            using var font = new SKFont(typeface, size * F(0.92f, 1.1f));
            using var paint = new SKPaint { Color = Rgb(10, 110), IsAntialias = true };

            var ch = text[i].ToString();
            var width = font.MeasureText(ch);
            var cx = pad + slot * (i + 0.5f) + F(-3f, 3f);
            var baseline = _h * 0.5f + size * 0.35f + F(-4f, 4f);   // cap height ~0.7 em, so centre ~0.35 em above baseline

            canvas.Save();
            canvas.Translate(cx, baseline);
            canvas.RotateDegrees(F(-22f, 22f));
            canvas.Scale(F(0.9f, 1.15f), F(0.9f, 1.15f));
            canvas.DrawText(ch, -width / 2f, 0, font, paint);
            canvas.Restore();
        }
    }

    private void DrawCurves(SKCanvas canvas, int count)
    {
        for (var i = 0; i < count; i++)
        {
            using var path = new SKPath();
            path.MoveTo(0, F(_h * 0.15f, _h * 0.85f));
            path.CubicTo(_w * 0.3f, F(0, _h), _w * 0.65f, F(0, _h), _w, F(_h * 0.15f, _h * 0.85f));

            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = F(1.2f, 2.4f),
                Color = Rgb(40, 160),
                IsAntialias = true,
            };
            canvas.DrawPath(path, paint);
        }
    }

    private void DrawSpeckles(SKCanvas canvas, int count)
    {
        using var paint = new SKPaint { IsAntialias = true };
        for (var i = 0; i < count; i++)
        {
            paint.Color = Rgb(20, 200);
            canvas.DrawCircle(F(0, _w), F(0, _h), F(0.6f, 1.4f), paint);
        }
    }

    /// Sine displacement: every row is shifted horizontally and every column vertically.
    /// Shifts are precomputed per row/column, so the pixel loop is a plain copy.
    private void Warp(SKBitmap src, SKBitmap dst)
    {
        var s = MemoryMarshal.Cast<byte, uint>(src.GetPixelSpan());
        var d = MemoryMarshal.Cast<byte, uint>(dst.GetPixelSpan());
        var stride = src.RowBytes / 4;

        double ax = F(2.0f, 3.5f), fy = F(0.08f, 0.14f), py = F(0f, 6.28f);
        double ay = F(1.5f, 3.0f), fx = F(0.05f, 0.09f), px = F(0f, 6.28f);

        var shiftX = new int[_h];    // horizontal shift for row y
        var shiftY = new int[_w];    // vertical shift for column x
        for (var y = 0; y < _h; y++) shiftX[y] = (int)Math.Round(ax * Math.Sin(fy * y + py));
        for (var x = 0; x < _w; x++) shiftY[x] = (int)Math.Round(ay * Math.Sin(fx * x + px));

        for (var y = 0; y < _h; y++)
        {
            for (var x = 0; x < _w; x++)
            {
                var sx = Math.Clamp(x + shiftX[y], 0, _w - 1);
                var sy = Math.Clamp(y + shiftY[x], 0, _h - 1);
                d[y * stride + x] = s[sy * stride + sx];
            }
        }
        dst.NotifyPixelsChanged();
    }

    private static float F(float min, float max) => min + (float)Random.Shared.NextDouble() * (max - min);

    private static SKColor Rgb(int min, int max) =>
        new((byte)Random.Shared.Next(min, max), (byte)Random.Shared.Next(min, max), (byte)Random.Shared.Next(min, max));

    public void Dispose()
    {
        foreach (var t in _typefaces) t.Dispose();
    }
}