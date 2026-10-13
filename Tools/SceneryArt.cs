// Рисует уличные фоны: 4 фона для уровней с БАШНЯМИ (луг -> пустошь -> руины ->
// крепость гоблинов) и фон главного меню (башня на скале на рассвете).
// Тот же мультяшный плоский стиль, что у стен для дверей (Tools/WallArt.cs):
// слои гор и холмов, деревья, облака, лёгкая виньетка и зерно.
//
// Запуск: Tools/MakeSceneryArt.ps1. Написано на C# 5 (компилятор Windows
// PowerShell 5.1), без внешних программ.
//
// Геометрия: линия "земли" (там стоят ноги героя и основание башни) — на
// FeetLineFromBottom от низа картинки (та же, что у стен). Правая часть кадра
// за башней и область слева от неё (куда встают примеры) сделаны спокойными.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

public static class SceneryArt
{
    public const int W = 2048;
    public const int H = 1152;
    public const float FeetLineFromBottom = 0.1709f;
    static readonly float FeetY = H * (1f - FeetLineFromBottom);

    public static void RenderAll(string backgroundsDir, string uiDir)
    {
        Directory.CreateDirectory(backgroundsDir);
        Directory.CreateDirectory(uiDir);
        Save(Render(0), Path.Combine(backgroundsDir, "bg_1_meadow.png"));
        Save(Render(1), Path.Combine(backgroundsDir, "bg_2_wasteland.png"));
        Save(Render(2), Path.Combine(backgroundsDir, "bg_3_ruins.png"));
        Save(Render(3), Path.Combine(backgroundsDir, "bg_4_fortress.png"));
        Save(Render(4), Path.Combine(uiDir, "menu_background.png"));
    }

    static void Save(Bitmap b, string path)
    {
        b.Save(path, ImageFormat.Png);
        b.Dispose();
        Console.WriteLine("saved " + path);
    }

    // ---------- утилиты ----------
    static int Cl(float v) { return v < 0 ? 0 : (v > 255 ? 255 : (int)v); }
    static Color Rgb(int r, int g, int b) { return Color.FromArgb(255, r, g, b); }
    static Color Mul(Color c, float f) { return Color.FromArgb(c.A, Cl(c.R * f), Cl(c.G * f), Cl(c.B * f)); }
    static Color A(Color c, int a) { return Color.FromArgb(a, c.R, c.G, c.B); }
    static Color Mix(Color a, Color b, float t)
    {
        return Color.FromArgb(Cl(a.A + (b.A - a.A) * t), Cl(a.R + (b.R - a.R) * t), Cl(a.G + (b.G - a.G) * t), Cl(a.B + (b.B - a.B) * t));
    }
    static float Rf(Random r, float a, float b) { return a + (float)r.NextDouble() * (b - a); }

    static float Hash(int n)
    {
        n = (n << 13) ^ n;
        return 1.0f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824.0f;
    }
    static float Smooth(float t) { return t * t * (3f - 2f * t); }
    static float VNoise(float x, int seed)
    {
        int i = (int)Math.Floor(x);
        float f = x - i;
        float a = Hash(i * 7 + seed * 131), b = Hash((i + 1) * 7 + seed * 131);
        return (a + (b - a) * Smooth(f)) * 0.5f + 0.5f;
    }
    static float Fbm(float x, int seed, int oct)
    {
        float v = 0, amp = 0.5f, fr = 1, sum = 0;
        for (int o = 0; o < oct; o++) { v += VNoise(x * fr, seed + o * 17) * amp; sum += amp; amp *= 0.5f; fr *= 2f; }
        return v / sum;
    }

    static void VGrad(Graphics g, RectangleF r, Color top, Color bottom)
    {
        if (r.Height < 1 || r.Width < 1) return;
        using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(r.X, r.Y - 1, r.Width, r.Height + 2), top, bottom, 90f))
            g.FillRectangle(b, r);
    }

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

    static void Glow(Graphics g, float cx, float cy, float rx, float ry, Color c, int alpha)
    {
        using (GraphicsPath gp = new GraphicsPath())
        {
            gp.AddEllipse(cx - rx, cy - ry, rx * 2, ry * 2);
            using (PathGradientBrush b = new PathGradientBrush(gp))
            {
                b.CenterColor = A(c, alpha);
                b.SurroundColors = new Color[] { A(c, 0) };
                g.FillPath(b, gp);
            }
        }
    }

    // Ломаная-хребет: y = base - amp * шум(x)
    static PointF[] Ridge(float baseY, float amp, float freq, int seed, bool ridged, int oct)
    {
        List<PointF> pts = new List<PointF>();
        for (float x = -20; x <= W + 20; x += 16f)
        {
            float n = Fbm(x * freq, seed, oct);
            if (ridged) n = 1f - Math.Abs(2f * n - 1f);
            pts.Add(new PointF(x, baseY - amp * n));
        }
        return pts.ToArray();
    }

    static float RidgeY(PointF[] ridge, float x)
    {
        int i = (int)((x + 20f) / 16f);
        if (i < 0) i = 0;
        if (i >= ridge.Length - 1) i = ridge.Length - 2;
        float t = (x - ridge[i].X) / 16f;
        return ridge[i].Y + (ridge[i + 1].Y - ridge[i].Y) * t;
    }

    // Холм/гора: многоугольник от хребта вниз с градиентом и светлой кромкой.
    static GraphicsPath FillLand(Graphics g, PointF[] ridge, Color top, Color bottom, Color rim, float bottomY)
    {
        List<PointF> pts = new List<PointF>(ridge);
        pts.Add(new PointF(W + 20, bottomY));
        pts.Add(new PointF(-20, bottomY));
        GraphicsPath path = new GraphicsPath();
        path.AddPolygon(pts.ToArray());
        float minY = float.MaxValue;
        foreach (PointF p in ridge) if (p.Y < minY) minY = p.Y;
        using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(0, minY - 1, W, bottomY - minY + 2), top, bottom, 90f))
            g.FillPath(b, path);
        if (rim.A > 0)
            using (Pen p = new Pen(rim, 4f)) { p.LineJoin = LineJoin.Round; g.DrawLines(p, ridge); }
        return path;
    }

    static void Cloud(Graphics g, float cx, float cy, float s, Color c, Color shade)
    {
        float[,] parts = new float[,] { { -90, 8, 120, 56 }, { -40, -22, 120, 76 }, { 20, -34, 130, 86 }, { 70, -6, 120, 62 }, { -10, 10, 190, 52 } };
        for (int pass = 0; pass < 2; pass++)
        {
            Color col = pass == 0 ? shade : c;
            float dy = pass == 0 ? 10 * s : 0;
            using (SolidBrush b = new SolidBrush(col))
                for (int i = 0; i < parts.GetLength(0); i++)
                    g.FillEllipse(b, cx + parts[i, 0] * s - parts[i, 2] * s / 2, cy + parts[i, 1] * s - parts[i, 3] * s / 2 + dy, parts[i, 2] * s, parts[i, 3] * s);
        }
    }

    // ---------- деревья и предметы ----------
    static void RoundTree(Graphics g, float x, float baseY, float size, Color trunk, Color leaf, Random rnd)
    {
        float tw = size * 0.14f, th = size * 0.55f;
        using (SolidBrush b = new SolidBrush(trunk)) g.FillRectangle(b, x - tw / 2, baseY - th, tw, th);
        float cr = size * 0.5f;
        float cy = baseY - th - cr * 0.35f;
        float[,] blobs = new float[,] { { 0, 0, 1.0f }, { -0.62f, 0.2f, 0.7f }, { 0.62f, 0.2f, 0.7f }, { -0.3f, -0.42f, 0.66f }, { 0.34f, -0.4f, 0.64f } };
        for (int pass = 0; pass < 3; pass++)
        {
            for (int i = 0; i < blobs.GetLength(0); i++)
            {
                float r = cr * blobs[i, 2];
                float bx = x + blobs[i, 0] * cr, by = cy + blobs[i, 1] * cr;
                Color c = pass == 0 ? Mul(leaf, 0.62f) : (pass == 1 ? leaf : Mul(leaf, 1.22f));
                float off = pass == 0 ? r * 0.12f : (pass == 2 ? -r * 0.22f : 0);
                float shrink = pass == 2 ? 0.55f : 1f;
                using (SolidBrush b = new SolidBrush(c))
                    g.FillEllipse(b, bx - r * shrink, by - r * shrink + off, r * 2 * shrink, r * 2 * shrink);
            }
        }
    }

    static void PineTree(Graphics g, float x, float baseY, float size, Color trunk, Color leaf)
    {
        using (SolidBrush b = new SolidBrush(trunk)) g.FillRectangle(b, x - size * 0.05f, baseY - size * 0.2f, size * 0.1f, size * 0.2f);
        for (int i = 0; i < 3; i++)
        {
            float w = size * (0.62f - i * 0.14f), top = baseY - size * (0.2f + (i + 1) * 0.3f), bot = baseY - size * (0.2f + i * 0.26f);
            PointF[] tri = new PointF[] { new PointF(x - w / 2, bot), new PointF(x + w / 2, bot), new PointF(x, top) };
            using (SolidBrush b = new SolidBrush(Mul(leaf, 0.8f + i * 0.1f))) g.FillPolygon(b, tri);
            PointF[] lit = new PointF[] { new PointF(x - w / 2, bot), new PointF(x, bot), new PointF(x, top) };
            using (SolidBrush b = new SolidBrush(A(Color.White, 28))) g.FillPolygon(b, lit);
        }
    }

    static void Bush(Graphics g, float x, float baseY, float size, Color c)
    {
        using (SolidBrush b = new SolidBrush(Mul(c, 0.7f))) g.FillEllipse(b, x - size * 0.6f, baseY - size * 0.5f, size * 1.2f, size * 0.62f);
        using (SolidBrush b = new SolidBrush(c)) { g.FillEllipse(b, x - size * 0.5f, baseY - size * 0.62f, size * 0.7f, size * 0.62f); g.FillEllipse(b, x - size * 0.05f, baseY - size * 0.55f, size * 0.62f, size * 0.55f); }
        using (SolidBrush b = new SolidBrush(Mul(c, 1.25f))) g.FillEllipse(b, x - size * 0.3f, baseY - size * 0.6f, size * 0.32f, size * 0.22f);
    }

    static void DeadTree(Graphics g, float x, float baseY, float size, Color c, Random rnd)
    {
        using (Pen p = new Pen(c, Math.Max(3f, size * 0.08f)))
        {
            p.StartCap = LineCap.Round; p.EndCap = LineCap.Round;
            float top = baseY - size;
            g.DrawBezier(p, x, baseY, x + size * 0.06f, baseY - size * 0.35f, x - size * 0.05f, baseY - size * 0.7f, x + size * 0.03f, top);
            for (int i = 0; i < 4; i++)
            {
                float by = baseY - size * (0.35f + i * 0.17f);
                float dir = (i % 2 == 0) ? 1 : -1;
                using (Pen bp = new Pen(c, Math.Max(2f, size * 0.045f)))
                {
                    bp.StartCap = LineCap.Round; bp.EndCap = LineCap.Round;
                    g.DrawBezier(bp, x, by, x + dir * size * 0.12f, by - size * 0.06f, x + dir * size * 0.22f, by - size * 0.1f, x + dir * size * (0.3f - i * 0.03f), by - size * 0.2f);
                }
            }
        }
    }

    static void Rock(Graphics g, float x, float baseY, float w, float h, Color c)
    {
        PointF[] pts = new PointF[] {
            new PointF(x - w / 2, baseY), new PointF(x - w * 0.42f, baseY - h * 0.55f), new PointF(x - w * 0.15f, baseY - h),
            new PointF(x + w * 0.25f, baseY - h * 0.85f), new PointF(x + w * 0.5f, baseY - h * 0.3f), new PointF(x + w / 2, baseY) };
        using (SolidBrush b = new SolidBrush(c)) g.FillPolygon(b, pts);
        PointF[] lit = new PointF[] { pts[0], pts[1], pts[2], new PointF(x - w * 0.02f, baseY - h * 0.5f), new PointF(x - w * 0.1f, baseY) };
        using (SolidBrush b = new SolidBrush(A(Color.White, 34))) g.FillPolygon(b, lit);
        PointF[] dark = new PointF[] { new PointF(x + w * 0.25f, baseY - h * 0.85f), pts[4], pts[5], new PointF(x + w * 0.05f, baseY), new PointF(x + w * 0.1f, baseY - h * 0.4f) };
        using (SolidBrush b = new SolidBrush(A(Color.Black, 44))) g.FillPolygon(b, dark);
        using (Pen p = new Pen(A(Color.Black, 70), 3f)) { p.LineJoin = LineJoin.Round; g.DrawPolygon(p, pts); }
    }

    static void GrassTufts(Graphics g, Random rnd, float y0, float y1, int count, Color c, float size)
    {
        for (int i = 0; i < count; i++)
        {
            float x = Rf(rnd, 0, W), y = Rf(rnd, y0, y1);
            float s = size * Rf(rnd, 0.7f, 1.3f);
            using (Pen p = new Pen(Mul(c, Rf(rnd, 0.85f, 1.2f)), Math.Max(2f, s * 0.12f)))
            {
                p.StartCap = LineCap.Round; p.EndCap = LineCap.Round;
                for (int k = -1; k <= 1; k++)
                    g.DrawLine(p, x + k * s * 0.18f, y, x + k * s * 0.4f + Rf(rnd, -3, 3), y - s * (k == 0 ? 1f : 0.7f));
            }
        }
    }

    static void Flowers(Graphics g, Random rnd, float y0, float y1, int count, Color[] colors)
    {
        for (int i = 0; i < count; i++)
        {
            float x = Rf(rnd, 0, W), y = Rf(rnd, y0, y1), s = Rf(rnd, 7, 12);
            Color c = colors[rnd.Next(colors.Length)];
            using (SolidBrush b = new SolidBrush(c))
                for (int k = 0; k < 5; k++)
                {
                    double a = k * Math.PI * 2 / 5;
                    g.FillEllipse(b, x + (float)Math.Cos(a) * s * 0.7f - s * 0.45f, y + (float)Math.Sin(a) * s * 0.7f - s * 0.45f, s * 0.9f, s * 0.9f);
                }
            using (SolidBrush b = new SolidBrush(Rgb(255, 214, 70))) g.FillEllipse(b, x - s * 0.35f, y - s * 0.35f, s * 0.7f, s * 0.7f);
        }
    }

    // Дорога под ногами: волнистая полоса с кромками и камешками.
    static void Road(Graphics g, Random rnd, Color top, Color bottom, Color edge, float y0, float y1, int seed, bool pebbles)
    {
        List<PointF> pts = new List<PointF>();
        for (float x = -20; x <= W + 20; x += 20f) pts.Add(new PointF(x, y0 + (Fbm(x * 0.004f, seed, 2) - 0.5f) * 44f));
        for (float x = W + 20; x >= -20; x -= 20f) pts.Add(new PointF(x, y1 + (Fbm(x * 0.004f, seed + 5, 2) - 0.5f) * 44f));
        using (GraphicsPath p = new GraphicsPath())
        {
            p.AddPolygon(pts.ToArray());
            using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(0, y0 - 40, W, y1 - y0 + 80), top, bottom, 90f)) g.FillPath(b, p);
            using (Pen pen = new Pen(edge, 5f)) { pen.LineJoin = LineJoin.Round; g.DrawPath(pen, p); }
        }
        if (pebbles)
            for (int i = 0; i < 70; i++)
            {
                float x = Rf(rnd, 0, W), y = Rf(rnd, y0 + 10, y1 - 10), s = Rf(rnd, 6, 16);
                using (SolidBrush b = new SolidBrush(A(Mul(edge, Rf(rnd, 0.9f, 1.3f)), 150))) g.FillEllipse(b, x, y, s * 1.4f, s);
            }
    }

    // ---------- сцены ----------
    static Bitmap Render(int kind)
    {
        Bitmap bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb);
        Random rnd = new Random(100 + kind * 31);
        float vignette = 0.34f;
        float[] tint = new float[] { 1f, 1f, 1f };

        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            switch (kind)
            {
                case 0: Meadow(g, rnd); vignette = 0.26f; break;
                case 1: Wasteland(g, rnd); vignette = 0.28f; break;
                case 2: Ruins(g, rnd); vignette = 0.28f; break;
                case 3: Fortress(g, rnd); vignette = 0.3f; break;
                default: Menu(g, rnd); vignette = 0.3f; break;
            }
        }
        FinalPass(bmp, vignette, tint);
        return bmp;
    }

    static void Meadow(Graphics g, Random rnd)
    {
        float horizon = H * 0.58f;
        VGrad(g, new RectangleF(0, 0, W, horizon + 40), Rgb(92, 172, 240), Rgb(214, 238, 252));
        Glow(g, W * 0.8f, H * 0.2f, 360, 260, Rgb(255, 250, 220), 150);
        Cloud(g, 300, 190, 1.4f, Rgb(255, 255, 255), Rgb(196, 214, 238));
        Cloud(g, 900, 120, 1.0f, Rgb(255, 255, 255), Rgb(196, 214, 238));
        Cloud(g, 1500, 240, 1.6f, Rgb(255, 255, 255), Rgb(196, 214, 238));
        Cloud(g, 1880, 120, 0.9f, Rgb(255, 255, 255), Rgb(196, 214, 238));

        // горы со снежными шапками
        PointF[] m1 = Ridge(horizon + 10, 330, 0.0022f, 3, true, 3);
        GraphicsPath mp = FillLand(g, m1, Rgb(150, 178, 214), Rgb(188, 212, 236), A(Color.White, 90), horizon + 60);
        {
            // снег лежит на вершинах выше линии снега, нижний край — зубчатый
            float snowLine = horizon - 190;
            List<PointF> topEdge = new List<PointF>(), lowEdge = new List<PointF>();
            foreach (PointF p in m1)
            {
                float depth = p.Y < snowLine ? (snowLine - p.Y) * 0.75f + 10f : 0f;
                if (depth > 0f) depth *= 0.82f + 0.18f * (float)Math.Sin(p.X * 0.09f);
                topEdge.Add(p);
                lowEdge.Add(new PointF(p.X, p.Y + depth));
            }
            lowEdge.Reverse();
            List<PointF> poly = new List<PointF>(topEdge);
            poly.AddRange(lowEdge);
            g.SetClip(mp);
            using (SolidBrush b = new SolidBrush(A(Color.White, 242))) g.FillPolygon(b, poly.ToArray());
            g.ResetClip();
        }
        PointF[] m2 = Ridge(horizon + 30, 200, 0.0034f, 9, true, 3);
        FillLand(g, m2, Rgb(108, 150, 194), Rgb(150, 190, 222), A(Color.White, 70), horizon + 90);

        // холмы с деревьями
        PointF[] h1 = Ridge(horizon + 80, 120, 0.0026f, 21, false, 3);
        FillLand(g, h1, Rgb(124, 186, 92), Rgb(96, 160, 72), A(Rgb(190, 232, 120), 160), H * 0.82f);
        for (int i = 0; i < 14; i++)
        {
            float x = Rf(rnd, 20, W - 20);
            if (x > 1200 && x < 1800) continue; // за башней и примерами — пусто
            if (rnd.Next(3) == 0) PineTree(g, x, RidgeY(h1, x) + 16, Rf(rnd, 70, 110), Rgb(90, 64, 44), Rgb(52, 120, 70));
            else RoundTree(g, x, RidgeY(h1, x) + 14, Rf(rnd, 70, 110), Rgb(110, 76, 46), Rgb(74, 160, 70), rnd);
        }
        PointF[] h2 = Ridge(horizon + 150, 90, 0.003f, 33, false, 3);
        FillLand(g, h2, Rgb(110, 178, 76), Rgb(84, 150, 62), A(Rgb(176, 226, 104), 170), H * 0.9f);
        for (int i = 0; i < 9; i++)
        {
            float x = Rf(rnd, 10, 700);
            if (rnd.Next(2) == 0) Bush(g, x, RidgeY(h2, x) + 18, Rf(rnd, 60, 100), Rgb(64, 150, 64));
            else RoundTree(g, x, RidgeY(h2, x) + 16, Rf(rnd, 120, 190), Rgb(104, 72, 44), Rgb(70, 156, 66), rnd);
        }
        for (int i = 0; i < 5; i++) Bush(g, Rf(rnd, 1250, 2020), RidgeY(h2, 1500) + 20, Rf(rnd, 50, 80), Rgb(64, 150, 64));

        // трава и дорога под ногами
        float gy = H * 0.78f;
        VGrad(g, new RectangleF(0, gy, W, H - gy), Rgb(96, 168, 66), Rgb(62, 128, 52));
        GrassTufts(g, rnd, gy + 10, FeetY - 70, 70, Rgb(86, 160, 60), 20);
        Road(g, rnd, Rgb(226, 184, 120), Rgb(196, 148, 90), Rgb(150, 104, 62), FeetY - 90, FeetY + 120, 4, true);
        GrassTufts(g, rnd, FeetY + 150, H, 60, Rgb(52, 120, 50), 36);
        Flowers(g, rnd, gy + 20, FeetY - 80, 28, new Color[] { Rgb(255, 255, 255), Rgb(255, 214, 70), Rgb(240, 110, 150) });
        Flowers(g, rnd, FeetY + 140, H - 10, 22, new Color[] { Rgb(255, 255, 255), Rgb(255, 214, 70), Rgb(240, 110, 150) });
    }

    static void Wasteland(Graphics g, Random rnd)
    {
        float horizon = H * 0.58f;
        VGrad(g, new RectangleF(0, 0, W, horizon + 40), Rgb(228, 140, 92), Rgb(252, 214, 158));
        Glow(g, W * 0.72f, H * 0.42f, 520, 300, Rgb(255, 238, 190), 190);
        using (SolidBrush b = new SolidBrush(Rgb(255, 238, 200))) g.FillEllipse(b, W * 0.72f - 90, H * 0.42f - 90, 180, 180);
        Cloud(g, 360, 210, 1.5f, Rgb(250, 190, 150), Rgb(214, 140, 112));
        Cloud(g, 1300, 150, 1.2f, Rgb(250, 190, 150), Rgb(214, 140, 112));
        Cloud(g, 1800, 260, 0.9f, Rgb(250, 196, 160), Rgb(214, 140, 112));

        // плоские скалы-мезы
        PointF[] far = Ridge(horizon + 20, 150, 0.0025f, 5, false, 2);
        for (int i = 0; i < far.Length; i++) far[i].Y = (float)Math.Round(far[i].Y / 36f) * 36f;
        FillLand(g, far, Rgb(196, 124, 92), Rgb(220, 160, 118), A(Color.White, 40), horizon + 70);
        PointF[] mid = Ridge(horizon + 60, 120, 0.003f, 15, false, 2);
        for (int i = 0; i < mid.Length; i++) mid[i].Y = (float)Math.Round(mid[i].Y / 28f) * 28f;
        FillLand(g, mid, Rgb(170, 100, 72), Rgb(200, 138, 98), A(Color.White, 36), horizon + 130);

        PointF[] h1 = Ridge(horizon + 110, 90, 0.0028f, 25, false, 3);
        FillLand(g, h1, Rgb(214, 168, 108), Rgb(196, 144, 90), A(Rgb(250, 214, 150), 150), H * 0.84f);
        for (int i = 0; i < 6; i++)
        {
            float x = Rf(rnd, 30, 900);
            DeadTree(g, x, RidgeY(h1, x) + 10, Rf(rnd, 120, 200), Rgb(86, 56, 40), rnd);
        }
        for (int i = 0; i < 6; i++) Rock(g, Rf(rnd, 1200, 2000), RidgeY(h1, 1500) + 30, Rf(rnd, 60, 120), Rf(rnd, 40, 80), Rgb(168, 122, 92));

        float gy = H * 0.76f;
        VGrad(g, new RectangleF(0, gy, W, H - gy), Rgb(206, 158, 100), Rgb(160, 112, 72));
        // трещины сухой земли
        using (Pen p = new Pen(Color.FromArgb(120, 100, 60, 40), 3f))
            for (int i = 0; i < 26; i++)
            {
                float x = Rf(rnd, 0, W), y = Rf(rnd, gy + 40, H - 20);
                List<PointF> pts = new List<PointF>();
                pts.Add(new PointF(x, y));
                for (int k = 0; k < 5; k++) { x += Rf(rnd, -30, 40); y += Rf(rnd, -12, 14); pts.Add(new PointF(x, y)); }
                g.DrawLines(p, pts.ToArray());
            }
        Road(g, rnd, Rgb(186, 138, 88), Rgb(160, 114, 72), Rgb(120, 82, 52), FeetY - 90, FeetY + 120, 7, true);
        for (int i = 0; i < 9; i++) Rock(g, Rf(rnd, 0, W), Rf(rnd, FeetY + 130, H - 10), Rf(rnd, 50, 110), Rf(rnd, 34, 70), Rgb(150, 108, 82));
        GrassTufts(g, rnd, FeetY + 150, H, 30, Rgb(150, 130, 70), 28);
    }

    static void Ruins(Graphics g, Random rnd)
    {
        float horizon = H * 0.58f;
        VGrad(g, new RectangleF(0, 0, W, horizon + 40), Rgb(118, 168, 224), Rgb(232, 240, 252));
        Glow(g, W * 0.3f, horizon - 30, 640, 260, Rgb(255, 240, 200), 140);
        Cloud(g, 500, 190, 1.4f, Rgb(255, 255, 255), Rgb(200, 212, 234));
        Cloud(g, 1250, 120, 1.1f, Rgb(255, 255, 255), Rgb(200, 212, 234));
        Cloud(g, 1750, 230, 1.5f, Rgb(255, 255, 255), Rgb(200, 212, 234));

        PointF[] m1 = Ridge(horizon + 10, 280, 0.0022f, 41, true, 3);
        FillLand(g, m1, Rgb(132, 148, 196), Rgb(180, 192, 222), A(Color.White, 70), horizon + 70);
        PointF[] h1 = Ridge(horizon + 90, 100, 0.0028f, 43, false, 3);
        FillLand(g, h1, Rgb(136, 164, 112), Rgb(108, 138, 94), A(Rgb(210, 232, 150), 150), H * 0.84f);

        // руины на холме: колонны и арка
        Color stone = Rgb(188, 186, 196), shade = Rgb(142, 140, 156);
        for (int i = 0; i < 5; i++)
        {
            float x = 120 + i * 190 + Rf(rnd, -30, 30), by = RidgeY(h1, x) + 20, h = Rf(rnd, 120, 230);
            using (SolidBrush b = new SolidBrush(stone)) g.FillRectangle(b, x - 22, by - h, 44, h);
            using (SolidBrush b = new SolidBrush(shade)) g.FillRectangle(b, x + 4, by - h, 18, h);
            using (SolidBrush b = new SolidBrush(Mul(stone, 1.1f))) g.FillRectangle(b, x - 34, by - h - 12, 68, 16);
            // сломанный верх
            using (SolidBrush b = new SolidBrush(Rgb(208, 206, 214))) g.FillPolygon(b, new PointF[] { new PointF(x - 30, by - h - 14), new PointF(x - 6, by - h - 36), new PointF(x + 12, by - h - 6), new PointF(x + 34, by - h - 28), new PointF(x + 36, by - h - 14) });
        }
        using (Pen p = new Pen(stone, 34f))
        {
            float ax = 640, ay = RidgeY(h1, 640) + 20;
            g.DrawArc(p, ax - 130, ay - 330, 260, 260, 180, 180);
            g.DrawLine(p, ax - 130, ay - 200, ax - 130, ay);
            g.DrawLine(p, ax + 130, ay - 200, ax + 130, ay);
        }

        float gy = H * 0.76f;
        VGrad(g, new RectangleF(0, gy, W, H - gy), Rgb(128, 156, 100), Rgb(92, 124, 80));
        GrassTufts(g, rnd, gy + 10, FeetY - 70, 60, Rgb(132, 164, 84), 20);
        // дорога из каменных плит
        Road(g, rnd, Rgb(206, 204, 210), Rgb(168, 166, 178), Rgb(112, 110, 124), FeetY - 90, FeetY + 120, 11, false);
        using (Pen p = new Pen(A(Rgb(80, 78, 92), 170), 3f))
            for (float x = -20; x < W; x += 130 + rnd.Next(40))
                g.DrawLine(p, x, FeetY - 70, x + Rf(rnd, -12, 12), FeetY + 100);
        // обломки
        for (int i = 0; i < 8; i++) Rock(g, Rf(rnd, 0, W), Rf(rnd, FeetY + 130, H - 10), Rf(rnd, 50, 100), Rf(rnd, 30, 56), Rgb(170, 168, 182));
        GrassTufts(g, rnd, FeetY + 150, H, 34, Rgb(96, 134, 76), 30);
    }

    static void FortressSilhouette(Graphics g, float cx, float baseY, float scale, Color body, Color window, Random rnd)
    {
        // центральная башня, боковые башни и стена с зубцами
        using (SolidBrush b = new SolidBrush(body))
        {
            g.FillRectangle(b, cx - 360 * scale, baseY - 130 * scale, 720 * scale, 130 * scale);
            for (int i = 0; i < 24; i++) g.FillRectangle(b, cx - 360 * scale + i * 30 * scale, baseY - 160 * scale, 18 * scale, 32 * scale);
            float[] tx = new float[] { -280, -130, 0, 140, 290 };
            float[] th = new float[] { 230, 300, 430, 320, 250 };
            for (int i = 0; i < tx.Length; i++)
            {
                float w = (i == 2 ? 150 : 100) * scale, x = cx + tx[i] * scale, h = th[i] * scale;
                g.FillRectangle(b, x - w / 2, baseY - h, w, h);
                g.FillPolygon(b, new PointF[] { new PointF(x - w / 2 - 10 * scale, baseY - h), new PointF(x + w / 2 + 10 * scale, baseY - h), new PointF(x, baseY - h - w * 0.9f) });
            }
        }
        using (SolidBrush b = new SolidBrush(window))
            for (int i = 0; i < 14; i++)
            {
                float x = cx + Rf(rnd, -320, 320) * scale, y = baseY - Rf(rnd, 60, 380) * scale;
                g.FillRectangle(b, x, y, 10 * scale, 18 * scale);
            }
    }

    static void Fortress(Graphics g, Random rnd)
    {
        float horizon = H * 0.58f;
        VGrad(g, new RectangleF(0, 0, W, horizon + 40), Rgb(120, 150, 208), Rgb(232, 228, 234));
        Glow(g, W * 0.55f, horizon, 760, 280, Rgb(255, 200, 140), 90);
        // тяжёлые облака (день, но тревожный)
        for (int i = 0; i < 6; i++) Cloud(g, Rf(rnd, 100, 1950), Rf(rnd, 90, 330), Rf(rnd, 1.2f, 2.0f), Rgb(176, 172, 200), Rgb(130, 124, 160));

        PointF[] m1 = Ridge(horizon + 20, 300, 0.0026f, 61, true, 3);
        FillLand(g, m1, Rgb(118, 100, 142), Rgb(166, 130, 150), A(Rgb(255, 190, 120), 90), horizon + 80);
        FortressSilhouette(g, W * 0.58f, horizon + 30, 1.2f, Rgb(98, 82, 118), Rgb(255, 214, 110), rnd);
        PointF[] h1 = Ridge(horizon + 100, 110, 0.003f, 67, true, 3);
        FillLand(g, h1, Rgb(132, 108, 124), Rgb(104, 86, 104), A(Rgb(255, 170, 110), 130), H * 0.84f);
        // шипы
        for (int i = 0; i < 12; i++)
        {
            float x = Rf(rnd, 10, 1000), s = Rf(rnd, 50, 110), y = RidgeY(h1, x) + 20;
            using (SolidBrush b = new SolidBrush(Rgb(78, 62, 88)))
                g.FillPolygon(b, new PointF[] { new PointF(x - s * 0.18f, y), new PointF(x + s * 0.18f, y), new PointF(x, y - s) });
        }

        float gy = H * 0.76f;
        VGrad(g, new RectangleF(0, gy, W, H - gy), Rgb(160, 130, 134), Rgb(120, 96, 106));
        // светящаяся лава в трещинах
        using (Pen p = new Pen(Rgb(255, 130, 40), 4f))
            for (int i = 0; i < 22; i++)
            {
                float x = Rf(rnd, 0, W), y = Rf(rnd, gy + 50, H - 20);
                List<PointF> pts = new List<PointF>();
                pts.Add(new PointF(x, y));
                for (int k = 0; k < 5; k++) { x += Rf(rnd, -34, 44); y += Rf(rnd, -12, 16); pts.Add(new PointF(x, y)); }
                using (Pen glow = new Pen(Color.FromArgb(70, 255, 120, 30), 12f)) g.DrawLines(glow, pts.ToArray());
                g.DrawLines(p, pts.ToArray());
            }
        Road(g, rnd, Rgb(190, 158, 154), Rgb(156, 128, 128), Rgb(92, 68, 74), FeetY - 90, FeetY + 120, 17, true);
        for (int i = 0; i < 8; i++) Rock(g, Rf(rnd, 0, W), Rf(rnd, FeetY + 130, H - 10), Rf(rnd, 50, 110), Rf(rnd, 34, 70), Rgb(136, 114, 130));
    }

    static void Menu(Graphics g, Random rnd)
    {
        float horizon = H * 0.56f;
        VGrad(g, new RectangleF(0, 0, W, horizon + 60), Rgb(88, 150, 226), Rgb(255, 214, 152));
        Glow(g, W * 0.6f, H * 0.3f, 760, 520, Rgb(255, 232, 160), 200);
        using (SolidBrush b = new SolidBrush(Rgb(255, 246, 210))) g.FillEllipse(b, W * 0.6f - 100, H * 0.3f - 100, 200, 200);
        Cloud(g, 330, 190, 1.7f, Rgb(255, 232, 214), Rgb(228, 170, 160));
        Cloud(g, 980, 130, 1.2f, Rgb(255, 240, 224), Rgb(232, 180, 170));
        Cloud(g, 1500, 330, 1.5f, Rgb(255, 226, 190), Rgb(232, 160, 140));
        Cloud(g, 1000, 360, 0.9f, Rgb(255, 232, 200), Rgb(232, 170, 150));

        PointF[] m1 = Ridge(horizon + 20, 300, 0.0023f, 71, true, 3);
        GraphicsPath mp = FillLand(g, m1, Rgb(124, 140, 196), Rgb(196, 176, 190), A(Color.White, 90), horizon + 90);
        PointF[] m2 = Ridge(horizon + 40, 180, 0.0034f, 75, true, 3);
        FillLand(g, m2, Rgb(98, 122, 170), Rgb(160, 160, 184), A(Color.White, 70), horizon + 110);

        // озеро
        VGrad(g, new RectangleF(0, horizon + 50, W, 150), Rgb(120, 176, 214), Rgb(190, 220, 232));
        using (Pen p = new Pen(A(Color.White, 90), 3f))
            for (int i = 0; i < 18; i++) { float x = Rf(rnd, 0, W), y = Rf(rnd, horizon + 70, horizon + 190), w = Rf(rnd, 60, 160); g.DrawLine(p, x, y, x + w, y); }

        // холмы с лесом
        PointF[] h1 = Ridge(horizon + 150, 130, 0.0027f, 81, false, 3);
        FillLand(g, h1, Rgb(124, 176, 84), Rgb(88, 142, 64), A(Rgb(220, 232, 120), 150), H);
        for (int i = 0; i < 16; i++)
        {
            float x = Rf(rnd, 10, 1500);
            if (x > 560 && x < 1260) continue;
            if (rnd.Next(3) == 0) PineTree(g, x, RidgeY(h1, x) + 16, Rf(rnd, 80, 130), Rgb(90, 64, 44), Rgb(48, 112, 68));
            else RoundTree(g, x, RidgeY(h1, x) + 14, Rf(rnd, 80, 130), Rgb(110, 76, 46), Rgb(86, 156, 66), rnd);
        }

        // скала с башней справа
        float tx = W * 0.84f, cliffTop = H * 0.62f;
        List<PointF> cliff = new List<PointF>();
        cliff.Add(new PointF(tx - 320, H));
        cliff.Add(new PointF(tx - 280, cliffTop + 230));
        cliff.Add(new PointF(tx - 190, cliffTop + 70));
        cliff.Add(new PointF(tx - 120, cliffTop));
        cliff.Add(new PointF(tx + 140, cliffTop - 8));
        cliff.Add(new PointF(tx + 230, cliffTop + 90));
        cliff.Add(new PointF(tx + 300, cliffTop + 260));
        cliff.Add(new PointF(tx + 340, H));
        using (GraphicsPath cp = new GraphicsPath())
        {
            cp.AddPolygon(cliff.ToArray());
            using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(tx - 320, cliffTop - 10, 660, H - cliffTop + 10), Rgb(150, 140, 142), Rgb(96, 88, 96), 90f)) g.FillPath(b, cp);
            using (Pen p = new Pen(A(Color.Black, 70), 4f)) { p.LineJoin = LineJoin.Round; g.DrawPath(p, cp); }
        }
        using (SolidBrush b = new SolidBrush(Rgb(110, 170, 70))) g.FillEllipse(b, tx - 150, cliffTop - 30, 330, 60);
        using (SolidBrush b = new SolidBrush(Rgb(140, 196, 84))) g.FillEllipse(b, tx - 120, cliffTop - 28, 220, 30);

        // каменная башня
        float tw = 190, th = 520, by = cliffTop;
        PointF[] body = new PointF[] { new PointF(tx - tw / 2, by), new PointF(tx - tw / 2 + 14, by - th), new PointF(tx + tw / 2 - 14, by - th), new PointF(tx + tw / 2, by) };
        using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(tx - tw / 2, by - th, tw, th), Rgb(204, 190, 172), Rgb(150, 138, 132), 0f))
            g.FillPolygon(b, body);
        using (Pen p = new Pen(A(Rgb(90, 76, 70), 150), 3f))
        {
            for (float y = by - 40; y > by - th + 20; y -= 44)
            {
                g.DrawLine(p, tx - tw / 2 + 4, y, tx + tw / 2 - 4, y);
                for (float x = tx - tw / 2 + 30 + ((int)(y / 44) % 2) * 22; x < tx + tw / 2 - 10; x += 44) g.DrawLine(p, x, y, x, y - 44);
            }
        }
        // зубцы, окна, дверь
        using (SolidBrush b = new SolidBrush(Rgb(190, 176, 160)))
        {
            g.FillRectangle(b, tx - tw / 2 - 12, by - th - 18, tw + 24, 24);
            for (int i = 0; i < 5; i++) g.FillRectangle(b, tx - tw / 2 - 12 + i * 44, by - th - 56, 30, 44);
        }
        using (SolidBrush b = new SolidBrush(Rgb(60, 40, 40)))
        {
            g.FillRectangle(b, tx - 12, by - th + 100, 24, 60);
            g.FillRectangle(b, tx - 12, by - th + 240, 24, 60);
            using (GraphicsPath dp = new GraphicsPath()) { dp.AddArc(tx - 34, by - 118, 68, 68, 180, 180); dp.AddLine(tx + 34, by - 84, tx + 34, by); dp.AddLine(tx + 34, by, tx - 34, by); dp.CloseFigure(); g.FillPath(b, dp); }
        }
        // флаг
        using (Pen p = new Pen(Rgb(80, 60, 50), 6f)) g.DrawLine(p, tx, by - th - 56, tx, by - th - 190);
        using (SolidBrush b = new SolidBrush(Rgb(200, 44, 52)))
            g.FillPolygon(b, new PointF[] { new PointF(tx, by - th - 186), new PointF(tx + 120, by - th - 170), new PointF(tx + 80, by - th - 146), new PointF(tx + 126, by - th - 124), new PointF(tx, by - th - 126) });
        // плющ на башне
        using (SolidBrush b = new SolidBrush(Rgb(70, 130, 60)))
            for (int i = 0; i < 14; i++) g.FillEllipse(b, tx - tw / 2 + Rf(rnd, 0, 40), by - Rf(rnd, 20, th - 100), Rf(rnd, 16, 34), Rf(rnd, 20, 40));

        // передний план
        GrassTufts(g, rnd, H * 0.84f, H - 10, 90, Rgb(70, 140, 56), 36);
        Flowers(g, rnd, H * 0.84f, H - 10, 34, new Color[] { Rgb(255, 255, 255), Rgb(255, 214, 70), Rgb(240, 110, 150) });

        // большое дерево слева, рамка кадра
        float lx = 130;
        using (SolidBrush b = new SolidBrush(Rgb(96, 66, 44))) g.FillPolygon(b, new PointF[] { new PointF(lx - 60, H), new PointF(lx - 36, H * 0.5f), new PointF(lx + 30, H * 0.5f), new PointF(lx + 70, H) });
        using (SolidBrush b = new SolidBrush(A(Color.Black, 40))) g.FillPolygon(b, new PointF[] { new PointF(lx + 4, H), new PointF(lx + 8, H * 0.5f), new PointF(lx + 30, H * 0.5f), new PointF(lx + 70, H) });
        for (int i = 0; i < 16; i++)
        {
            float bx = lx + Rf(rnd, -260, 280), byy = H * 0.3f + Rf(rnd, -120, 170), r = Rf(rnd, 90, 170);
            using (SolidBrush b = new SolidBrush(Rgb(54, 116, 52))) g.FillEllipse(b, bx - r, byy - r + 12, r * 2, r * 2);
            using (SolidBrush b = new SolidBrush(Rgb(74, 146, 62))) g.FillEllipse(b, bx - r, byy - r, r * 2, r * 2);
            using (SolidBrush b = new SolidBrush(Rgb(112, 178, 80))) g.FillEllipse(b, bx - r * 0.6f, byy - r * 0.8f, r * 1.1f, r * 0.8f);
        }
    }

    static void FinalPass(Bitmap bmp, float vignette, float[] tint)
    {
        BitmapData bd = bmp.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        int stride = bd.Stride;
        byte[] buf = new byte[stride * H];
        Marshal.Copy(bd.Scan0, buf, 0, buf.Length);
        for (int y = 0; y < H; y++)
        {
            float ny = (y / (float)H - 0.5f) * 2f;
            for (int x = 0; x < W; x++)
            {
                float nx = (x / (float)W - 0.5f) * 2f;
                float d = nx * nx * 0.6f + (ny > 0f ? ny * ny * 0.6f : ny * ny * 0.12f);
                float f = 1f - vignette * d;
                int h = x * 374761393 + y * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                float noise = ((h & 255) / 255f - 0.5f) * 6f;
                int i = y * stride + x * 4;
                buf[i + 0] = (byte)Cl(buf[i + 0] * f * tint[2] + noise);
                buf[i + 1] = (byte)Cl(buf[i + 1] * f * tint[1] + noise);
                buf[i + 2] = (byte)Cl(buf[i + 2] * f * tint[0] + noise);
                buf[i + 3] = 255;
            }
        }
        Marshal.Copy(buf, 0, bd.Scan0, buf.Length);
        bmp.UnlockBits(bd);
    }
}
