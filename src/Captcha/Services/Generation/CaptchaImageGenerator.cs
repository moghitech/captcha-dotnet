using System.Runtime.InteropServices;
using Captcha.Options;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace Captcha.Services.Generation;

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

        if (Path.Exists(dir))
        {
            // Load fonts from files you ship with the app. Do NOT rely on system fonts: slim
            // containers usually have none. Use permissively licensed bold faces (DejaVu Sans Bold,
            // Liberation Sans Bold, Noto Sans Bold, ...) and keep their license files.
            var skTypefaces = Directory.EnumerateFiles(path: dir, searchPattern: "*.ttf")
                .Select(p => SKTypeface.FromFile(p)).Where(t => t is not null)
                .ToArray();

            _typefaces = Directory.Exists(dir) ? skTypefaces : [];
        }
        else
        {
            _typefaces = [];
        }

        if (_typefaces.Length is 0)
            throw new InvalidOperationException($"No .ttf fonts found in '{dir}'. Ship 2-4 bold fonts with the app.");
    }

    public byte[] Render(string text)
    {
        var info = new SKImageInfo(width: _w, height: _h, SKColorType.Rgba8888, SKAlphaType.Opaque);

        // Pass 1: background, some curves behind the text, then the glyphs.
        using var flat = new SKBitmap(info);
        using (var canvas = new SKCanvas(flat))
        {
            DrawBackground(canvas);
            DrawCurves(canvas, count: 2);
            DrawGlyphs(canvas, text);
        }

        // Pass 2: wave-warp the whole picture so glyph shapes bend, then draw noise ON TOP of
        // the warped text (strokes crossing glyphs make segmentation harder).
        using var warped = new SKBitmap(info);
        Warp(src: flat, dst: warped);
        using (var canvas = new SKCanvas(warped))
        {
            DrawCurves(canvas, count: 3);
            DrawSpeckles(canvas, count: 150);
        }

        using var image = SKImage.FromBitmap(warped);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private void DrawBackground(SKCanvas canvas)
    {
        using var shader = SKShader.CreateLinearGradient(
            start: new SKPoint(0, 0),
            end: new SKPoint(_w, _h),
            colors: [Rgb(200, 245), Rgb(200, 245)],
            mode: SKShaderTileMode.Clamp
        );
        using var paint = new SKPaint();
        paint.Shader = shader;
        canvas.DrawRect(0, 0, _w, _h, paint);
    }

    private void DrawGlyphs(SKCanvas canvas, string text)
    {
        const float pad = 10f;
        var size = _h * 0.62f;
        var slot = (_w - 2 * pad) / text.Length; // each glyph is centred in its slot, then jittered

        for (var i = 0; i < text.Length; i++)
        {
            var typeface = _typefaces[Random.Shared.Next(_typefaces.Length)];
            using var font = new SKFont(typeface, size * F(0.92f, 1.1f));
            using var paint = new SKPaint();
            paint.Color = Rgb(10, 110);
            paint.IsAntialias = true;

            var ch = text[i].ToString();
            var width = font.MeasureText(ch);
            var cx = pad + slot * (i + 0.5f) + F(-3f, 3f);
            var baseline =
                _h * 0.5f + size * 0.35f + F(-4f, 4f); // cap height ~0.7 em, so centre ~0.35 em above baseline

            canvas.Save();
            canvas.Translate(dx: cx, dy: baseline);
            canvas.RotateDegrees(F(-22f, 22f));
            canvas.Scale(sx: F(0.9f, 1.15f), sy: F(0.9f, 1.15f));
            
#pragma warning disable CS0618 // Type or member is obsolete
            canvas.DrawText(ch, x: -width / 2f, y: 0, font, paint);
#pragma warning restore CS0618 // Type or member is obsolete
            
            canvas.Restore();
        }
    }

    private void DrawCurves(SKCanvas canvas, int count)
    {
        for (var i = 0; i < count; i++)
        {
            using var path = new SKPath();
            
#pragma warning disable CS0618 // Type or member is obsolete
            path.MoveTo(x: 0, y: F(_h * 0.15f, _h * 0.85f));
            path.CubicTo(
                x0: _w * 0.3f,
                y0: F(0, _h),
                x1: _w * 0.65f,
                y1: F(0, _h),
                x2: _w,
                y2: F(_h * 0.15f, _h * 0.85f)
            );
#pragma warning restore CS0618 // Type or member is obsolete

            using var paint = new SKPaint();
            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = F(1.2f, 2.4f);
            paint.Color = Rgb(40, 160);
            paint.IsAntialias = true;
            
            canvas.DrawPath(path, paint);
        }
    }

    private void DrawSpeckles(SKCanvas canvas, int count)
    {
        using var paint = new SKPaint();
        paint.IsAntialias = true;
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

        var shiftX = new int[_h]; // horizontal shift for row y
        var shiftY = new int[_w]; // vertical shift for column x
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

    private static SKColor Rgb(int min, int max)
    {
        return new SKColor(
            red: (byte)Random.Shared.Next(min, max),
            green: (byte)Random.Shared.Next(min, max),
            blue: (byte)Random.Shared.Next(min, max)
        );
    }

    public void Dispose()
    {
        foreach (var t in _typefaces) t.Dispose();
    }
}