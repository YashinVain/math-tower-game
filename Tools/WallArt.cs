// Рисует фоны для уровней с ДВЕРЯМИ: плоская (вид строго спереди) кирпичная
// стена, у основания которой стоят двери, и полоса пола внизу. Стиль —
// мультяшная раскраска, как у героя, дверей и фонов уровней с башнями:
// крупные кирпичи с подсветкой сверху и тенью снизу, деревянная балка,
// арочные окна, знамёна, факелы.
//
// Запуск: Tools/MakeWallArt.ps1 (компилирует этот файл и рисует 4 картинки в
// Assets/_Project/Art/Backgrounds). Написано на C# 5 — этого достаточно для
// компилятора Windows PowerShell 5.1 (Add-Type), без внешних программ.
//
// Геометрия важна: линия, где стена встречается с полом, лежит на FloorLine
// (доля высоты картинки снизу) — на неё в игре ставятся "ноги" героя и дверей
// (см. BackgroundFitter и LevelVisualTheme.floorLine).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

public class WallStyle
{
    public string File;
    public int Seed;
    public Color Stone, Mortar, Wood, Accent, Glow, Floor, Sky1, Sky2;
    public float[] Tint = new float[] { 1f, 1f, 1f };
    public float Vignette = 0.45f;
    public float BrickScale = 1f;
    public int[] Windows;   // центры по ширине, в процентах
    public int[] Banners;
    public int[] Torches;
    public int Emblem;      // 0 корона, 1 череп, 2 череп с рогами
    public bool Tattered, Moss, Cracks, Carpet, BigBanner, Skulls;
}

public static class WallArt
{
    public const int W = 2048;
    public const int H = 1152;
    // Где стена переходит в пол (сверху вниз) и где стоят "ноги" персонажей.
    public const float WallBaseFraction = 0.81f;
    public const int FeetBelowBase = 22;

    public static float FeetLineFromBottom
    {
        get { return (H - (H * WallBaseFraction + FeetBelowBase)) / H; }
    }

    public static void RenderAll(string outDir)
    {
        WallStyle[] styles = new WallStyle[]
        {
            // 1. Светлый парадный зал: тёплый камень, окна, синие знамёна с короной.
            new WallStyle {
                File = "bg_castle_1_hall.png", Seed = 11,
                Stone = Color.FromArgb(196, 176, 146), Mortar = Color.FromArgb(104, 86, 70),
                Wood = Color.FromArgb(120, 78, 44), Accent = Color.FromArgb(46, 92, 178),
                Glow = Color.FromArgb(255, 190, 90), Floor = Color.FromArgb(168, 148, 124),
                Sky1 = Color.FromArgb(120, 190, 245), Sky2 = Color.FromArgb(215, 238, 252),
                Vignette = 0.26f, Windows = new int[] { 22, 50, 78 }, Banners = new int[] { 36, 64 },
                Torches = new int[] { 8, 92 }, Emblem = 0, Carpet = true
            },
            // 2. Заброшенный зал: серо-зелёный камень, мох, трещины, закатное окно.
            new WallStyle {
                File = "bg_castle_2_neglected.png", Seed = 23,
                Stone = Color.FromArgb(182, 184, 164), Mortar = Color.FromArgb(98, 100, 86),
                Wood = Color.FromArgb(96, 70, 48), Accent = Color.FromArgb(120, 60, 60),
                Glow = Color.FromArgb(255, 170, 80), Floor = Color.FromArgb(166, 164, 146),
                Sky1 = Color.FromArgb(150, 196, 226), Sky2 = Color.FromArgb(226, 240, 248),
                Vignette = 0.3f, Windows = new int[] { 22, 50, 78 }, Banners = new int[] { 36, 64 },
                Torches = new int[] { 8, 92 }, Emblem = 1, Tattered = true, Moss = true, Cracks = true
            },
            // 3. Логово гоблинов: тёмный сине-серый камень, красные знамёна с черепами, факелы.
            new WallStyle {
                File = "bg_castle_3_lair.png", Seed = 37,
                Stone = Color.FromArgb(178, 186, 208), Mortar = Color.FromArgb(92, 98, 124),
                Wood = Color.FromArgb(84, 58, 40), Accent = Color.FromArgb(190, 44, 52),
                Glow = Color.FromArgb(255, 150, 60), Floor = Color.FromArgb(156, 160, 182),
                Sky1 = Color.FromArgb(150, 196, 226), Sky2 = Color.FromArgb(226, 240, 248),
                Vignette = 0.3f, Banners = new int[] { 24, 50, 76 }, Torches = new int[] { 12, 37, 63, 88 },
                Emblem = 1, Cracks = true, Skulls = true, BrickScale = 1.15f
            },
            // 4. Тронный зал вожака: тёмный пурпурный камень, зелёное свечение, большое знамя.
            new WallStyle {
                File = "bg_castle_4_throne.png", Seed = 51,
                Stone = Color.FromArgb(182, 160, 188), Mortar = Color.FromArgb(100, 80, 112),
                Wood = Color.FromArgb(96, 64, 46), Accent = Color.FromArgb(134, 64, 176),
                Glow = Color.FromArgb(120, 255, 90), Floor = Color.FromArgb(156, 138, 162),
                Sky1 = Color.FromArgb(150, 196, 226), Sky2 = Color.FromArgb(226, 240, 248),
                Vignette = 0.32f, Banners = new int[] { 30, 70 }, Torches = new int[] { 12, 40, 60, 88 },
                Emblem = 2, Carpet = true, BigBanner = true, Cracks = true
            }
        };

        Directory.CreateDirectory(outDir);
        foreach (WallStyle s in styles)
        {
            Bitmap bmp = Render(s);
            string path = Path.Combine(outDir, s.File);
            bmp.Save(path, ImageFormat.Png);
            bmp.Dispose();
            Console.WriteLine("saved " + path);
        }
    }

    // ---------- вспомогательное ----------
    static int Cl(float v) { return v < 0 ? 0 : (v > 255 ? 255 : (int)v); }
    static Color Mul(Color c, float f) { return Color.FromArgb(c.A, Cl(c.R * f), Cl(c.G * f), Cl(c.B * f)); }
    static Color A(Color c, int alpha) { return Color.FromArgb(alpha, c.R, c.G, c.B); }
    static Color Mix(Color a, Color b, float t)
    {
        return Color.FromArgb(Cl(a.A + (b.A - a.A) * t), Cl(a.R + (b.R - a.R) * t), Cl(a.G + (b.G - a.G) * t), Cl(a.B + (b.B - a.B) * t));
    }
    static float Rf(Random r, float a, float b) { return a + (float)r.NextDouble() * (b - a); }

    static GraphicsPath RoundRect(float x, float y, float w, float h, float r)
    {
        GraphicsPath p = new GraphicsPath();
        float d = r * 2f;
        if (d > w) d = w;
        if (d > h) d = h;
        p.AddArc(x, y, d, d, 180, 90);
        p.AddArc(x + w - d, y, d, d, 270, 90);
        p.AddArc(x + w - d, y + h - d, d, d, 0, 90);
        p.AddArc(x, y + h - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    static GraphicsPath ArchPath(float x, float y, float w, float h)
    {
        GraphicsPath p = new GraphicsPath();
        p.AddArc(x, y, w, w, 180, 180);
        p.AddLine(x + w, y + w / 2f, x + w, y + h);
        p.AddLine(x + w, y + h, x, y + h);
        p.CloseFigure();
        return p;
    }

    static void VGrad(Graphics g, RectangleF r, Color top, Color bottom)
    {
        if (r.Height < 1 || r.Width < 1) return;
        using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(r.X, r.Y - 1, r.Width, r.Height + 2), top, bottom, 90f))
            g.FillRectangle(b, r);
    }

    // ---------- рисование ----------
    public static Bitmap Render(WallStyle s)
    {
        Bitmap bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb);
        Random rnd = new Random(s.Seed);
        int baseY = (int)(H * WallBaseFraction);

        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(s.Mortar);

            DrawBricks(g, s, rnd, baseY);
            if (s.Cracks) DrawCracks(g, rnd, baseY);
            DrawBeam(g, s, rnd);

            if (s.Windows != null) foreach (int p in s.Windows) DrawWindow(g, s, rnd, W * p / 100f);
            if (s.Banners != null)
            {
                foreach (int p in s.Banners)
                {
                    bool big = s.BigBanner && p == 50;
                    DrawBanner(g, s, rnd, W * p / 100f, 176f, 160f, 320f);
                }
            }
            if (s.BigBanner) DrawBanner(g, s, rnd, W * 0.5f, 176f, 250f, 420f);
            if (s.Skulls) DrawSkulls(g, rnd, baseY);
            if (s.Torches != null) foreach (int p in s.Torches) DrawTorch(g, s, rnd, W * p / 100f, H * 0.43f);

            DrawFloor(g, s, rnd, baseY);
            if (s.Moss) DrawMoss(g, rnd, baseY);
        }

        FinalPass(bmp, s);
        return bmp;
    }

    static void DrawBricks(Graphics g, WallStyle s, Random rnd, int baseY)
    {
        int rowH = (int)(76 * s.BrickScale);
        int rows = baseY / rowH + 2;
        for (int r = 0; r < rows; r++)
        {
            int y = r * rowH;
            int x = -rnd.Next(0, 170);
            while (x < W)
            {
                int bw = (int)((150 + rnd.Next(0, 100)) * s.BrickScale);
                float jitter = Rf(rnd, 0.9f, 1.1f);
                Color c = Mul(s.Stone, jitter);
                // лёгкий тёплый/холодный сдвиг, чтобы кладка не была однотонной
                float warm = Rf(rnd, -8f, 8f);
                c = Color.FromArgb(255, Cl(c.R + warm), c.G, Cl(c.B - warm));

                float bx = x + 4, by = y + 4, bwid = bw - 8, bh = rowH - 8;
                using (GraphicsPath p = RoundRect(bx, by, bwid, bh, 12))
                {
                    using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(bx, by - 1, bwid, bh + 2), Mul(c, 1.12f), Mul(c, 0.86f), 90f))
                        g.FillPath(b, p);
                }
                // подсветка сверху и тень снизу — "объём" кирпича
                using (Pen hp = new Pen(Color.FromArgb(80, 255, 255, 255), 3f))
                    g.DrawLine(hp, bx + 14, by + 3, bx + bwid - 14, by + 3);
                using (Pen sp = new Pen(Color.FromArgb(70, 0, 0, 0), 3f))
                    g.DrawLine(sp, bx + 14, by + bh - 2, bx + bwid - 14, by + bh - 2);

                // крапины на камне
                for (int k = 0; k < 12; k++)
                {
                    float sx = bx + Rf(rnd, 10, bwid - 10), sy = by + Rf(rnd, 8, bh - 8), sz = Rf(rnd, 3, 9);
                    Color sc = rnd.Next(2) == 0 ? Color.FromArgb(22, 0, 0, 0) : Color.FromArgb(24, 255, 255, 255);
                    using (SolidBrush sb = new SolidBrush(sc)) g.FillEllipse(sb, sx, sy, sz, sz * 0.8f);
                }
                x += bw;
            }
        }
    }

    static void DrawCracks(Graphics g, Random rnd, int baseY)
    {
        using (Pen p = new Pen(Color.FromArgb(120, 20, 16, 14), 3f))
        {
            p.LineJoin = LineJoin.Round;
            for (int i = 0; i < 9; i++)
            {
                float x = Rf(rnd, 80, W - 80), y = Rf(rnd, 200, baseY - 100);
                List<PointF> pts = new List<PointF>();
                pts.Add(new PointF(x, y));
                int n = rnd.Next(4, 8);
                for (int k = 0; k < n; k++)
                {
                    x += Rf(rnd, -26, 26);
                    y += Rf(rnd, 14, 40);
                    pts.Add(new PointF(x, y));
                }
                g.DrawLines(p, pts.ToArray());
            }
        }
    }

    static void DrawBeam(Graphics g, WallStyle s, Random rnd)
    {
        int y0 = (int)(H * 0.095f), bh = 60;
        // каменный карниз над балкой
        VGrad(g, new RectangleF(0, y0 - 30, W, 30), Mul(s.Stone, 0.7f), Mul(s.Stone, 0.5f));
        using (Pen hp = new Pen(Color.FromArgb(70, 255, 255, 255), 3f)) g.DrawLine(hp, 0, y0 - 28, W, y0 - 28);
        // сама деревянная балка
        VGrad(g, new RectangleF(0, y0, W, bh), Mul(s.Wood, 1.3f), Mul(s.Wood, 0.72f));
        for (int i = 0; i < 70; i++)
        {
            float gy = y0 + Rf(rnd, 4, bh - 4), gx = Rf(rnd, 0, W), gl = Rf(rnd, 60, 260);
            using (Pen gp = new Pen(Color.FromArgb(40, 30, 14, 4), 2f)) g.DrawLine(gp, gx, gy, gx + gl, gy);
        }
        using (Pen hp = new Pen(Color.FromArgb(90, 255, 220, 170), 3f)) g.DrawLine(hp, 0, y0 + 3, W, y0 + 3);
        // железные заклёпки
        for (int x = 70; x < W; x += 210)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 58, 54, 54))) g.FillEllipse(b, x - 9, y0 + bh / 2 - 9, 18, 18);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(150, 255, 255, 255))) g.FillEllipse(b, x - 5, y0 + bh / 2 - 6, 7, 6);
        }
        // тень под балкой
        VGrad(g, new RectangleF(0, y0 + bh, W, 44), Color.FromArgb(120, 0, 0, 0), Color.FromArgb(0, 0, 0, 0));
    }

    static void DrawWindow(Graphics g, WallStyle s, Random rnd, float cx)
    {
        float w = 200, h = 330, x = cx - w / 2f, y = H * 0.17f;
        // каменная рама
        using (GraphicsPath frame = ArchPath(x - 24, y - 24, w + 48, h + 24))
        {
            using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(x - 24, y - 24, w + 48, h + 48), Mul(s.Stone, 1.25f), Mul(s.Stone, 0.85f), 90f))
                g.FillPath(b, frame);
            using (Pen p = new Pen(A(s.Mortar, 200), 4f)) g.DrawPath(p, frame);
        }
        using (GraphicsPath win = ArchPath(x, y, w, h))
        {
            using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(x, y - 1, w, h + 2), s.Sky1, s.Sky2, 90f))
                g.FillPath(b, win);
            g.SetClip(win);
            // далёкие горы и облака
            using (SolidBrush m1 = new SolidBrush(Mix(s.Sky1, Color.FromArgb(70, 100, 130), 0.55f)))
                g.FillPolygon(m1, new PointF[] { new PointF(x - 10, y + h), new PointF(x + 50, y + h * 0.62f), new PointF(x + 100, y + h * 0.78f), new PointF(x + 150, y + h * 0.58f), new PointF(x + w + 10, y + h) });
            using (SolidBrush m2 = new SolidBrush(Mix(s.Sky1, Color.FromArgb(40, 70, 90), 0.7f)))
                g.FillPolygon(m2, new PointF[] { new PointF(x - 10, y + h), new PointF(x + 30, y + h * 0.82f), new PointF(x + 90, y + h * 0.7f), new PointF(x + 140, y + h * 0.86f), new PointF(x + w + 10, y + h * 0.74f), new PointF(x + w + 10, y + h) });
            using (SolidBrush cb = new SolidBrush(Color.FromArgb(150, 255, 255, 255)))
            {
                g.FillEllipse(cb, x + 20, y + 80, 90, 34);
                g.FillEllipse(cb, x + 70, y + 100, 100, 30);
            }
            // оконный переплёт
            using (Pen bp = new Pen(Mul(s.Wood, 0.6f), 9f))
            {
                g.DrawLine(bp, cx, y, cx, y + h);
                g.DrawLine(bp, x, y + h * 0.45f, x + w, y + h * 0.45f);
                g.DrawLine(bp, x, y + h * 0.78f, x + w, y + h * 0.78f);
            }
            g.ResetClip();
            using (Pen p = new Pen(A(s.Mortar, 230), 5f)) g.DrawPath(p, win);
        }
        // подоконник
        using (GraphicsPath sill = RoundRect(x - 40, y + h, w + 80, 32, 8))
        {
            using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(x - 40, y + h - 1, w + 80, 34), Mul(s.Stone, 1.3f), Mul(s.Stone, 0.8f), 90f))
                g.FillPath(b, sill);
            using (Pen p = new Pen(A(s.Mortar, 220), 3f)) g.DrawPath(p, sill);
        }
        VGrad(g, new RectangleF(x - 40, y + h + 32, w + 80, 26), Color.FromArgb(90, 0, 0, 0), Color.FromArgb(0, 0, 0, 0));
    }

    static void DrawBanner(Graphics g, WallStyle s, Random rnd, float cx, float top, float w, float h)
    {
        bool torn = s.Tattered;
        Color c = s.Accent, dark = Mul(s.Accent, 0.55f);
        // нижний край: ласточкин хвост (рваный — зигзагом)
        List<PointF> pts = new List<PointF>();
        pts.Add(new PointF(cx - w / 2, top));
        pts.Add(new PointF(cx + w / 2, top));
        if (torn)
        {
            int n = 7;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                pts.Add(new PointF(cx + w / 2 - t * w, top + h - (i % 2 == 0 ? 0 : 22) - (float)rnd.NextDouble() * 18f));
            }
        }
        else
        {
            pts.Add(new PointF(cx + w / 2, top + h));
            pts.Add(new PointF(cx, top + h - h * 0.13f));
            pts.Add(new PointF(cx - w / 2, top + h));
        }
        PointF[] arr = pts.ToArray();

        // тень полотна на стене
        PointF[] sh = new PointF[arr.Length];
        for (int i = 0; i < arr.Length; i++) sh[i] = new PointF(arr[i].X + 12, arr[i].Y + 10);
        using (SolidBrush b = new SolidBrush(Color.FromArgb(80, 0, 0, 0))) g.FillPolygon(b, sh);

        // полотно с продольной складкой (светлее в центре)
        using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(cx - w / 2, top, w, h), dark, dark, 0f))
        {
            ColorBlend cb = new ColorBlend(5);
            cb.Colors = new Color[] { dark, c, Mul(c, 1.18f), c, dark };
            cb.Positions = new float[] { 0f, 0.28f, 0.5f, 0.72f, 1f };
            b.InterpolationColors = cb;
            g.FillPolygon(b, arr);
        }
        using (Pen p = new Pen(Mul(c, 0.42f), 4f)) { p.LineJoin = LineJoin.Round; g.DrawPolygon(p, arr); }
        // золотая кайма сверху и внутренняя рамка
        Color gold = Color.FromArgb(236, 188, 60);
        using (SolidBrush b = new SolidBrush(gold)) g.FillRectangle(b, cx - w / 2 + 3, top + 4, w - 6, 16);
        using (Pen p = new Pen(A(gold, 200), 3f)) g.DrawLine(p, cx - w / 2 + 14, top + 34, cx + w / 2 - 14, top + 34);
        // перекладина с шариками
        using (SolidBrush b = new SolidBrush(Mul(s.Wood, 0.8f))) g.FillRectangle(b, cx - w / 2 - 18, top - 10, w + 36, 14);
        using (SolidBrush b = new SolidBrush(gold))
        {
            g.FillEllipse(b, cx - w / 2 - 28, top - 15, 24, 24);
            g.FillEllipse(b, cx + w / 2 + 4, top - 15, 24, 24);
        }

        float ey = top + h * 0.44f;
        float scale = w / 160f;
        if (s.Emblem == 0) DrawCrown(g, cx, ey, scale);
        else if (s.Emblem == 1) DrawSkull(g, cx, ey, scale, false);
        else DrawSkull(g, cx, ey, scale, true);

        if (torn)
        {
            // дыры в ткани
            using (SolidBrush b = new SolidBrush(Color.FromArgb(160, 30, 22, 20)))
                for (int i = 0; i < 3; i++)
                    g.FillEllipse(b, cx + Rf(rnd, -w * 0.3f, w * 0.3f), top + Rf(rnd, h * 0.5f, h * 0.85f), Rf(rnd, 10, 22), Rf(rnd, 14, 30));
        }
    }

    static void DrawCrown(Graphics g, float cx, float cy, float k)
    {
        Color gold = Color.FromArgb(244, 196, 64);
        PointF[] pts = new PointF[]
        {
            new PointF(cx - 40 * k, cy + 22 * k), new PointF(cx - 46 * k, cy - 22 * k), new PointF(cx - 22 * k, cy - 2 * k),
            new PointF(cx, cy - 34 * k), new PointF(cx + 22 * k, cy - 2 * k), new PointF(cx + 46 * k, cy - 22 * k), new PointF(cx + 40 * k, cy + 22 * k)
        };
        using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(cx - 46 * k, cy - 34 * k, 92 * k, 58 * k), Mul(gold, 1.15f), Mul(gold, 0.8f), 90f))
            g.FillPolygon(b, pts);
        using (Pen p = new Pen(Color.FromArgb(150, 90, 50, 10), 3f)) { p.LineJoin = LineJoin.Round; g.DrawPolygon(p, pts); }
        using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 120, 30, 40))) g.FillEllipse(b, cx - 7 * k, cy + 4 * k, 14 * k, 14 * k);
        using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 40, 100, 200)))
        {
            g.FillEllipse(b, cx - 30 * k, cy + 6 * k, 10 * k, 10 * k);
            g.FillEllipse(b, cx + 20 * k, cy + 6 * k, 10 * k, 10 * k);
        }
    }

    static void DrawSkull(Graphics g, float cx, float cy, float k, bool horned)
    {
        Color bone = Color.FromArgb(240, 232, 214);
        Color edge = Color.FromArgb(150, 40, 30, 24);
        if (horned)
        {
            using (Pen hp = new Pen(bone, 11f * k))
            {
                hp.StartCap = LineCap.Round; hp.EndCap = LineCap.Round;
                g.DrawBezier(hp, cx - 30 * k, cy - 26 * k, cx - 78 * k, cy - 26 * k, cx - 80 * k, cy - 70 * k, cx - 52 * k, cy - 96 * k);
                g.DrawBezier(hp, cx + 30 * k, cy - 26 * k, cx + 78 * k, cy - 26 * k, cx + 80 * k, cy - 70 * k, cx + 52 * k, cy - 96 * k);
            }
        }
        using (SolidBrush b = new SolidBrush(bone)) g.FillEllipse(b, cx - 34 * k, cy - 38 * k, 68 * k, 60 * k);
        using (GraphicsPath jaw = RoundRect(cx - 22 * k, cy + 8 * k, 44 * k, 26 * k, 8 * k))
        {
            using (SolidBrush b = new SolidBrush(bone)) g.FillPath(b, jaw);
            using (Pen p = new Pen(edge, 2.5f)) g.DrawPath(p, jaw);
        }
        using (Pen p = new Pen(edge, 2.5f)) g.DrawEllipse(p, cx - 34 * k, cy - 38 * k, 68 * k, 60 * k);
        using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 30, 20, 20)))
        {
            g.FillEllipse(b, cx - 24 * k, cy - 16 * k, 19 * k, 22 * k);
            g.FillEllipse(b, cx + 5 * k, cy - 16 * k, 19 * k, 22 * k);
            g.FillPolygon(b, new PointF[] { new PointF(cx, cy + 2 * k), new PointF(cx - 6 * k, cy + 14 * k), new PointF(cx + 6 * k, cy + 14 * k) });
        }
        using (Pen p = new Pen(Color.FromArgb(170, 60, 44, 36), 2f))
            for (int i = -2; i <= 2; i++) g.DrawLine(p, cx + i * 8 * k, cy + 18 * k, cx + i * 8 * k, cy + 32 * k);
    }

    // Небольшие черепа на стене ("логово"): кучками над плинтусом и у потолка.
    static void DrawSkulls(Graphics g, Random rnd, int baseY)
    {
        float[] xs = new float[] { 0.05f, 0.12f, 0.9f, 0.96f }; // у краёв: за дверями черепов нет
        foreach (float fx in xs)
        {
            float x = W * fx;
            DrawSkull(g, x, H * 0.64f + Rf(rnd, -14, 14), 0.9f, false);
        }
    }

    static void DrawTorch(Graphics g, WallStyle s, Random rnd, float cx, float fy)
    {
        // Факел чуть крупнее (масштаб вокруг точки пламени).
        GraphicsState saved = g.Save();
        g.TranslateTransform(cx, fy);
        g.ScaleTransform(1.3f, 1.3f);
        g.TranslateTransform(-cx, -fy);
        // свечение на стене
        using (GraphicsPath gp = new GraphicsPath())
        {
            gp.AddEllipse(cx - 320, fy - 300, 640, 600);
            using (PathGradientBrush b = new PathGradientBrush(gp))
            {
                b.CenterColor = A(s.Glow, 120);
                b.SurroundColors = new Color[] { A(s.Glow, 0) };
                g.FillPath(b, gp);
            }
        }
        // железный кронштейн и чаша
        Color iron = Color.FromArgb(255, 54, 50, 56);
        using (SolidBrush b = new SolidBrush(iron))
        {
            g.FillRectangle(b, cx - 11, fy + 40, 22, 90);
            g.FillPolygon(b, new PointF[] { new PointF(cx - 30, fy + 14), new PointF(cx + 30, fy + 14), new PointF(cx + 17, fy + 58), new PointF(cx - 17, fy + 58) });
        }
        using (Pen p = new Pen(Color.FromArgb(120, 255, 255, 255), 3f)) g.DrawLine(p, cx - 26, fy + 18, cx + 26, fy + 18);
        using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 110, 70, 38))) g.FillEllipse(b, cx - 15, fy - 4, 30, 30);
        // пламя тремя слоями: оранжевое, жёлтое, светлая сердцевина
        float baseY = fy + 14;
        FillFlame(g, cx, baseY, 34, 100, Mix(s.Glow, Color.FromArgb(230, 90, 20), 0.55f));
        FillFlame(g, cx, baseY - 4, 24, 78, Mix(s.Glow, Color.FromArgb(255, 220, 90), 0.6f));
        FillFlame(g, cx, baseY - 8, 12, 48, Color.FromArgb(255, 255, 235, 170));
        g.Restore(saved);
    }

    static void FillFlame(Graphics g, float cx, float baseY, float w, float h, Color c)
    {
        using (GraphicsPath p = new GraphicsPath())
        {
            p.AddBezier(cx, baseY, cx - w * 1.15f, baseY, cx - w * 0.85f, baseY - h * 0.55f, cx + w * 0.12f, baseY - h);
            p.AddBezier(cx + w * 0.12f, baseY - h, cx + w * 0.9f, baseY - h * 0.55f, cx + w * 1.15f, baseY, cx, baseY);
            p.CloseFigure();
            using (SolidBrush b = new SolidBrush(c)) g.FillPath(b, p);
        }
    }

    static void DrawFloor(Graphics g, WallStyle s, Random rnd, int baseY)
    {
        // плинтус у основания стены
        VGrad(g, new RectangleF(0, baseY - 36, W, 36), Mul(s.Stone, 0.62f), Mul(s.Stone, 0.42f));
        using (Pen p = new Pen(Color.FromArgb(90, 255, 255, 255), 3f)) g.DrawLine(p, 0, baseY - 34, W, baseY - 34);

        // плиты пола: ряды становятся выше книзу (лёгкая глубина без перспективы)
        float y = baseY;
        float rowH = 38;
        int row = 0;
        while (y < H)
        {
            float h = rowH;
            float w = 190 * (1f + row * 0.14f);
            float x = -rnd.Next(0, (int)w);
            while (x < W)
            {
                float sw = w * Rf(rnd, 0.8f, 1.2f);
                Color c = Mul(s.Floor, Rf(rnd, 0.92f, 1.08f) * (1f + row * 0.012f));
                using (GraphicsPath p = RoundRect(x + 3, y + 3, sw - 6, h - 6, 8))
                {
                    using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(x, y - 1, sw, h + 2), Mul(c, 1.1f), Mul(c, 0.84f), 90f))
                        g.FillPath(b, p);
                }
                using (Pen hp = new Pen(Color.FromArgb(70, 255, 255, 255), 2.5f)) g.DrawLine(hp, x + 12, y + 5, x + sw - 12, y + 5);
                x += sw;
            }
            y += h;
            rowH *= 1.2f;
            row++;
        }
        // каменные швы между рядами
        using (Pen jp = new Pen(A(s.Mortar, 150), 3f))
        {
            float yy = baseY; float rh = 38;
            while (yy < H) { g.DrawLine(jp, 0, yy, W, yy); yy += rh; rh *= 1.2f; }
        }

        // ковровая дорожка (вдоль всего зала, под ногами)
        if (s.Carpet)
        {
            float cy = baseY + FeetBelowBase + 26, ch = 104;
            Color red = Color.FromArgb(255, 168, 30, 40);
            VGrad(g, new RectangleF(0, cy, W, ch), Mul(red, 1.2f), Mul(red, 0.78f));
            Color gold = Color.FromArgb(255, 232, 184, 58);
            using (SolidBrush b = new SolidBrush(gold))
            {
                g.FillRectangle(b, 0, cy, W, 9);
                g.FillRectangle(b, 0, cy + ch - 9, W, 9);
            }
            using (Pen p = new Pen(A(gold, 190), 3f)) { g.DrawLine(p, 0, cy + 22, W, cy + 22); g.DrawLine(p, 0, cy + ch - 22, W, cy + ch - 22); }
            using (SolidBrush b = new SolidBrush(A(gold, 200)))
                for (float x = 40; x < W; x += 90)
                    g.FillPolygon(b, new PointF[] { new PointF(x, cy + ch / 2 - 16), new PointF(x + 16, cy + ch / 2), new PointF(x, cy + ch / 2 + 16), new PointF(x - 16, cy + ch / 2) });
            VGrad(g, new RectangleF(0, cy + ch, W, 18), Color.FromArgb(90, 0, 0, 0), Color.FromArgb(0, 0, 0, 0));
        }

        // тень стены на полу (контакт стены с землёй)
        VGrad(g, new RectangleF(0, baseY, W, 64), Color.FromArgb(150, 0, 0, 0), Color.FromArgb(0, 0, 0, 0));
    }

    static void DrawMoss(Graphics g, Random rnd, int baseY)
    {
        Color m1 = Color.FromArgb(190, 70, 120, 44), m2 = Color.FromArgb(200, 118, 168, 62);
        for (int i = 0; i < 90; i++)
        {
            float x = Rf(rnd, -20, W), y = baseY - Rf(rnd, 0, 70);
            float w = Rf(rnd, 40, 130), h = Rf(rnd, 20, 60);
            using (SolidBrush b = new SolidBrush(m1)) g.FillEllipse(b, x, y - h * 0.5f, w, h);
            using (SolidBrush b = new SolidBrush(m2)) g.FillEllipse(b, x + w * 0.15f, y - h * 0.45f, w * 0.55f, h * 0.45f);
        }
        // плети мха и плюща, свисающие с балки
        using (Pen p = new Pen(Color.FromArgb(200, 70, 120, 44), 6f))
        {
            p.StartCap = LineCap.Round; p.EndCap = LineCap.Round;
            for (int i = 0; i < 16; i++)
            {
                float x = Rf(rnd, 0, W), len = Rf(rnd, 40, 170);
                g.DrawBezier(p, x, 170, x + Rf(rnd, -20, 20), 170 + len * 0.4f, x + Rf(rnd, -26, 26), 170 + len * 0.7f, x + Rf(rnd, -16, 16), 170 + len);
            }
        }
    }

    // Общая "печать": тёмные углы и потолок, лёгкая цветовая тонировка и
    // мелкое зерно (картинка выглядит нарисованной, а не цифровой плоской).
    static void FinalPass(Bitmap bmp, WallStyle s)
    {
        BitmapData bd = bmp.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        int stride = bd.Stride;
        byte[] buf = new byte[stride * H];
        Marshal.Copy(bd.Scan0, buf, 0, buf.Length);

        for (int y = 0; y < H; y++)
        {
            float ny = (y / (float)H - 0.5f) * 2f;
            float topT = y / (float)H;
            for (int x = 0; x < W; x++)
            {
                float nx = (x / (float)W - 0.5f) * 2f;
                float d = nx * nx * 0.7f + ny * ny * 0.55f;
                float f = 1f - s.Vignette * d;
                if (topT < 0.3f) f *= 0.92f + 0.08f * (topT / 0.3f);

                int h = x * 374761393 + y * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                float noise = ((h & 255) / 255f - 0.5f) * 7f;

                int i = y * stride + x * 4;
                buf[i + 0] = (byte)Cl(buf[i + 0] * f * s.Tint[2] + noise);
                buf[i + 1] = (byte)Cl(buf[i + 1] * f * s.Tint[1] + noise);
                buf[i + 2] = (byte)Cl(buf[i + 2] * f * s.Tint[0] + noise);
                buf[i + 3] = 255;
            }
        }
        Marshal.Copy(buf, 0, bd.Scan0, buf.Length);
        bmp.UnlockBits(bd);
    }
}
