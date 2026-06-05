using SkiaSharp;

// Genera il logo/icona di TaxManager: badge arrotondato accent con simbolo € e accento tricolore.
// Refinements (judge panel): fill flat per dimensioni piccole (no banding), bordo hairline,
// centratura ottica del glifo, tricolore verticale size-gated, lieve ombra di profondità.
// Produce logo.ico (multi-size, PNG-embedded) e logo.png 256.

var repoRoot = FindRepoRoot();
var assets = Path.Combine(repoRoot, "src", "TaxManager.App", "Assets");
Directory.CreateDirectory(assets);

int[] sizes = { 16, 24, 32, 48, 64, 128, 256 };
var imgs = sizes.Select(s => (size: s, png: RenderPng(s))).ToList();

WriteIco(Path.Combine(assets, "logo.ico"), imgs);
File.WriteAllBytes(Path.Combine(assets, "logo.png"), RenderPng(256));
Console.WriteLine($"Scritti logo.ico ({sizes.Length} dimensioni) e logo.png in {assets}");
return;

byte[] RenderPng(int size)
{
    using var bmp = new SKBitmap(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
    using var canvas = new SKCanvas(bmp);
    DrawLogo(canvas, size);
    canvas.Flush();
    using var img = SKImage.FromBitmap(bmp);
    using var data = img.Encode(SKEncodedImageFormat.Png, 100);
    return data.ToArray();
}

void DrawLogo(SKCanvas c, int size)
{
    c.Clear(SKColors.Transparent);
    float s = size;
    var rect = new SKRect(s * 0.06f, s * 0.06f, s * 0.94f, s * 0.94f);
    float radius = s * 0.22f;
    var rrect = new SKRoundRect(rect, radius, radius);
    bool gradient = size >= 32;
    bool big = size >= 48;
    bool depth = size >= 32;

    // Badge: gradiente accent per dimensioni grandi, tinta piatta per le piccole (evita banding).
    using (var bg = new SKPaint { IsAntialias = true })
    {
        if (gradient)
            bg.Shader = SKShader.CreateLinearGradient(
                new SKPoint(rect.MidX, rect.Top), new SKPoint(rect.MidX, rect.Bottom),
                new[] { SKColor.Parse("#1f6feb"), SKColor.Parse("#58a6ff") }, null, SKShaderTileMode.Clamp);
        else
            bg.Color = SKColor.Parse("#1f6feb");
        c.DrawRoundRect(rrect, bg);
    }

    // Accento tricolore verticale sul bordo interno sinistro (solo dimensioni grandi), clippato al badge.
    if (big)
    {
        c.Save();
        c.ClipRoundRect(rrect, SKClipOperation.Intersect, true);
        float w = rect.Width * 0.06f;
        float x = rect.Left;
        float third = rect.Height / 3f;
        const byte a = 217;
        using var p = new SKPaint { IsAntialias = true };
        p.Color = SKColor.Parse("#008C45").WithAlpha(a); c.DrawRect(x, rect.Top, w, third, p);
        p.Color = SKColors.White.WithAlpha(a);           c.DrawRect(x, rect.Top + third, w, third, p);
        p.Color = SKColor.Parse("#CD212A").WithAlpha(a); c.DrawRect(x, rect.Top + 2 * third, w, third, p);
        c.Restore();
    }

    // Bordo hairline: separa il badge da page (#0d1117) e surface (#161b22).
    using (var border = new SKPaint { IsAntialias = true, IsStroke = true, StrokeWidth = Math.Max(1f, s * 0.02f), Color = SKColor.Parse("#30363d") })
        c.DrawRoundRect(rrect, border);

    // Glifo € (Segoe UI Bold, presente su Windows; fallback al default).
    var tf = SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
             ?? SKTypeface.Default;
    // SkiaSharp 3.x: lo stile del testo (typeface, dimensione, allineamento) vive su SKFont,
    // non più su SKPaint (TextSize/Typeface/TextAlign rimossi in 3.0).
    using var font = new SKFont(tf, s * 0.6f);
    if (depth)
        using (var shadow = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#1158c7") })
            DrawCentered(c, "€", rect, font, shadow, 0f, s * 0.02f);

    using (var glyph = new SKPaint { IsAntialias = true, Color = SKColors.White })
        DrawCentered(c, "€", rect, font, glyph, 0f, 0f);
}

void DrawCentered(SKCanvas c, string text, SKRect rect, SKFont font, SKPaint paint, float dx, float dy)
{
    // Skia 3.x: misura e disegno passano per SKFont; l'allineamento è argomento di DrawText.
    font.MeasureText(text, out var b, paint);
    c.DrawText(text, rect.MidX - b.MidX + dx, rect.MidY - b.MidY + dy, SKTextAlign.Left, font, paint);
}

void WriteIco(string path, IReadOnlyList<(int size, byte[] png)> images)
{
    using var fs = File.Create(path);
    using var w = new BinaryWriter(fs);
    w.Write((short)0);              // reserved
    w.Write((short)1);              // type: icon
    w.Write((short)images.Count);   // image count

    int offset = 6 + images.Count * 16;
    foreach (var (size, png) in images)
    {
        w.Write((byte)(size >= 256 ? 0 : size)); // width  (0 = 256)
        w.Write((byte)(size >= 256 ? 0 : size)); // height (0 = 256)
        w.Write((byte)0);   // palette count
        w.Write((byte)0);   // reserved
        w.Write((short)1);  // color planes
        w.Write((short)32); // bits per pixel
        w.Write(png.Length);
        w.Write(offset);
        offset += png.Length;
    }
    foreach (var (_, png) in images) w.Write(png);
}

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "TaxManager.slnx")))
        dir = dir.Parent;
    return dir?.FullName ?? throw new InvalidOperationException("Repo root (TaxManager.slnx) non trovato.");
}
