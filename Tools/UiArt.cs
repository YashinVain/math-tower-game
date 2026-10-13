// Рисует детали интерфейса в том же плоском мультяшном стиле, что персонажи и
// фоны (Tools/SpriteArt.cs, WallArt.cs, SceneryArt.cs): деревянная панель в
// стальной раме с золотой окантовкой и заклёпками, тёмный контур.
//
//   button_wide.png         растягиваемая кнопка (9-slice, уголки по 76 px)
//   panel_board.png         растягиваемая доска для окон (9-slice, уголки по 76 px)
//   level_button_frame.png  рамка кнопки уровня и коробочка галочки в настройках
//   icon_check.png          золотая медаль с зелёной галочкой
//   icon_lock.png           замок для закрытых уровней
//   slider_track/fill/knob  дорожка, заливка и бегунок слайдера
//
// Запуск: Tools/MakeUiArt.ps1. Написано на C# 5 (компилятор Windows
// PowerShell 5.1), без внешних программ.
//
// ВАЖНО: уголки растягиваемых картинок (CornerPx) должны совпадать с
// ArtSpecs.SlicedCornerPx в Unity, а в средней полосе нет деталей, которые
// нельзя растянуть по горизонтали (только линии вдоль) — иначе при растяжении
// будет видно искажение.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public static class UiArt
{
    static readonly Color Ink = Color.FromArgb(255, 40, 26, 22);
    static readonly Color SteelTop = Color.FromArgb(255, 186, 192, 208);
    static readonly Color SteelBottom = Color.FromArgb(255, 108, 114, 134);
    static readonly Color Gold = Color.FromArgb(255, 238, 190, 74);

    public static void RenderAll(string dir)
    {
        Directory.CreateDirectory(dir);
        Save(Plate(660, 168, 18, 7, 38, Color.FromArgb(255, 130, 84, 48), 9, 44), Path.Combine(dir, "button_wide.png"));
        Save(Plate(900, 570, 24, 9, 52, Color.FromArgb(255, 122, 78, 46), 12, 54), Path.Combine(dir, "panel_board.png"));
        Save(Plate(512, 512, 34, 10, 62, Color.FromArgb(255, 78, 50, 30), 13, 66), Path.Combine(dir, "level_button_frame.png"));
        Save(Medal(), Path.Combine(dir, "icon_check.png"));
        Save(Padlock(), Path.Combine(dir, "icon_lock.png"));
        Save(Tick(), Path.Combine(dir, "icon_tick.png"));
        Save(SliderKnob(), Path.Combine(dir, "slider_knob.png"));
        Save(SliderBar(false), Path.Combine(dir, "slider_track.png"));
        Save(SliderBar(true), Path.Combine(dir, "slider_fill.png"));
    }

    static void Save(Bitmap b, string path)
    {
        b.Save(path, ImageFormat.Png);
        b.Dispose();
        Console.WriteLine("saved " + path);
    }

    static int Cl(float v) { return v < 0 ? 0 : (v > 255 ? 255 : (int)v); }
    static Color Mul(Color c, float f) { return Color.FromArgb(c.A, Cl(c.R * f), Cl(c.G * f), Cl(c.B * f)); }
    static Color A(Color c, int a) { return Color.FromArgb(a, c.R, c.G, c.B); }

    static Graphics Begin(Bitmap bmp)
    {
        Graphics g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(Color.Transparent);
        return g;
    }

    static GraphicsPath RRect(float x, float y, float w, float h, float r)
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

    static void FillV(Graphics g, GraphicsPath p, Color top, Color bottom)
    {
        RectangleF r = p.GetBounds();
        using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(r.X - 1, r.Y - 1, r.Width + 2, r.Height + 2), top, bottom, 90f))
            g.FillPath(b, p);
    }

    static void Rivet(Graphics g, float cx, float cy, float r)
    {
        using (SolidBrush b = new SolidBrush(Ink)) g.FillEllipse(b, cx - r - 3, cy - r - 3, (r + 3) * 2, (r + 3) * 2);
        using (GraphicsPath p = new GraphicsPath())
        {
            p.AddEllipse(cx - r, cy - r, r * 2, r * 2);
            FillV(g, p, SteelTop, SteelBottom);
        }
        using (SolidBrush b = new SolidBrush(A(Color.White, 150))) g.FillEllipse(b, cx - r * 0.55f, cy - r * 0.7f, r * 0.7f, r * 0.55f);
    }

    // Деревянная панель в стальной раме. rim — ширина стальной рамы, trim —
    // золотой окантовки, radius — скругление углов; faceTop — цвет дерева
    // сверху (снизу темнее); rivetR/rivetInset — размер и отступ заклёпок.
    static Bitmap Plate(int W, int H, int rim, int trim, int radius, Color faceTop, float rivetR, int rivetInset)
    {
        Color faceBottom = Mul(faceTop, 0.66f);
        Bitmap bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb);
        using (Graphics g = Begin(bmp))
        {
            float o = 5f; // отступ от края картинки под контур

            // стальная рама
            using (GraphicsPath outer = RRect(o, o, W - 2 * o, H - 2 * o, radius))
            {
                FillV(g, outer, SteelTop, SteelBottom);
                g.SetClip(outer);
                using (Pen hl = new Pen(A(Color.White, 90), 4f)) g.DrawLine(hl, radius, o + 6, W - radius, o + 6);
                g.ResetClip();
            }

            // золотая окантовка и деревянная основа
            float tx = o + rim;
            using (GraphicsPath trimPath = RRect(tx, tx, W - 2 * tx, H - 2 * tx, Math.Max(8, radius - rim)))
            {
                using (SolidBrush b = new SolidBrush(Gold)) g.FillPath(b, trimPath);
                using (Pen p = new Pen(A(Ink, 170), 3f)) g.DrawPath(p, trimPath);
            }
            float fx = tx + trim;
            using (GraphicsPath face = RRect(fx, fx, W - 2 * fx, H - 2 * fx, Math.Max(6, radius - rim - trim)))
            {
                FillV(g, face, faceTop, faceBottom);
                g.SetClip(face);
                // волокна дерева: горизонтальные линии (тянутся без искажений)
                if (H < 300)
                {
                    float step = Math.Max(12f, (H - 2 * fx) / 9f);
                    using (Pen grain = new Pen(Color.FromArgb(34, 20, 10, 6), 2f))
                        for (float y = fx + step * 0.6f; y < H - fx; y += step) g.DrawLine(grain, 0, y, W, y);
                }
                using (Pen hl = new Pen(Color.FromArgb(70, 255, 230, 190), 4f)) g.DrawLine(hl, 0, fx + 4, W, fx + 4);
                using (Pen sh = new Pen(Color.FromArgb(70, 0, 0, 0), 5f)) g.DrawLine(sh, 0, H - fx - 3, W, H - fx - 3);
                g.ResetClip();
                using (Pen p = new Pen(A(Ink, 190), 4f)) g.DrawPath(p, face);
            }

            // заклёпки в углах
            float ri = rivetInset;
            Rivet(g, ri, ri, rivetR);
            Rivet(g, W - ri, ri, rivetR);
            Rivet(g, ri, H - ri, rivetR);
            Rivet(g, W - ri, H - ri, rivetR);

            // внешний контур
            using (GraphicsPath outline = RRect(o, o, W - 2 * o, H - 2 * o, radius))
            using (Pen p = new Pen(Ink, 9f)) { p.LineJoin = LineJoin.Round; g.DrawPath(p, outline); }
        }
        return bmp;
    }

    // Золотая медаль с зелёной галочкой.
    static Bitmap Medal()
    {
        const int S = 512;
        Bitmap bmp = new Bitmap(S, S, PixelFormat.Format32bppArgb);
        using (Graphics g = Begin(bmp))
        {
            using (GraphicsPath outer = new GraphicsPath())
            {
                outer.AddEllipse(14, 14, S - 28, S - 28);
                FillV(g, outer, Color.FromArgb(255, 252, 218, 100), Color.FromArgb(255, 212, 146, 36));
                using (Pen p = new Pen(Ink, 14f)) g.DrawPath(p, outer);
            }
            using (GraphicsPath ring = new GraphicsPath())
            {
                ring.AddEllipse(60, 60, S - 120, S - 120);
                FillV(g, ring, Color.FromArgb(255, 255, 230, 140), Color.FromArgb(255, 238, 178, 60));
                using (Pen p = new Pen(A(Ink, 150), 7f)) g.DrawPath(p, ring);
            }
            using (SolidBrush b = new SolidBrush(A(Color.White, 110))) g.FillEllipse(b, 96, 70, 190, 90);

            // галочка: тёмный контур, зелёная основа, светлый блик
            PointF[] pts = new PointF[] { new PointF(142, 268), new PointF(222, 350), new PointF(374, 170) };
            using (Pen o = new Pen(Ink, 78f)) { o.StartCap = LineCap.Round; o.EndCap = LineCap.Round; o.LineJoin = LineJoin.Round; g.DrawLines(o, pts); }
            using (Pen p = new Pen(Color.FromArgb(255, 58, 180, 74), 56f)) { p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; p.LineJoin = LineJoin.Round; g.DrawLines(p, pts); }
            using (Pen h = new Pen(Color.FromArgb(150, 190, 255, 170), 16f)) { h.StartCap = LineCap.Round; h.EndCap = LineCap.Round; g.DrawLine(h, 150, 258, 222, 332); }
        }
        return bmp;
    }

    // Зелёная галочка с тёмным контуром — для коробочек в настройках.
    static Bitmap Tick()
    {
        const int S = 256;
        Bitmap bmp = new Bitmap(S, S, PixelFormat.Format32bppArgb);
        using (Graphics g = Begin(bmp))
        {
            PointF[] pts = new PointF[] { new PointF(46, 136), new PointF(100, 190), new PointF(212, 66) };
            using (Pen o = new Pen(Ink, 60f)) { o.StartCap = LineCap.Round; o.EndCap = LineCap.Round; o.LineJoin = LineJoin.Round; g.DrawLines(o, pts); }
            using (Pen p = new Pen(Color.FromArgb(255, 70, 200, 84), 38f)) { p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; p.LineJoin = LineJoin.Round; g.DrawLines(p, pts); }
            using (Pen h = new Pen(Color.FromArgb(150, 200, 255, 180), 11f)) { h.StartCap = LineCap.Round; h.EndCap = LineCap.Round; g.DrawLine(h, 54, 128, 100, 176); }
        }
        return bmp;
    }

    // Замок: стальная дужка и золотой корпус.
    static Bitmap Padlock()
    {
        const int W = 384, H = 512;
        Bitmap bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb);
        using (Graphics g = Begin(bmp))
        {
            // дужка
            using (GraphicsPath sh = new GraphicsPath())
            {
                sh.AddLine(100, 250, 100, 150);
                sh.AddArc(100, 40, 184, 220, 180, 180);
                sh.AddLine(284, 150, 284, 250);
                using (Pen o = new Pen(Ink, 76f)) { o.LineJoin = LineJoin.Round; g.DrawPath(o, sh); }
                using (Pen p = new Pen(Color.FromArgb(255, 170, 176, 194), 52f)) { p.LineJoin = LineJoin.Round; g.DrawPath(p, sh); }
                using (Pen h = new Pen(Color.FromArgb(150, 255, 255, 255), 12f)) g.DrawArc(h, 116, 56, 150, 188, 190, 90);
            }
            // корпус
            using (GraphicsPath body = RRect(24, 226, W - 48, 262, 52))
            {
                FillV(g, body, Color.FromArgb(255, 252, 214, 96), Color.FromArgb(255, 206, 140, 34));
                using (Pen p = new Pen(Ink, 14f)) { p.LineJoin = LineJoin.Round; g.DrawPath(p, body); }
                g.SetClip(body);
                using (SolidBrush b = new SolidBrush(A(Color.White, 80))) g.FillRectangle(b, 24, 236, W - 48, 36);
                g.ResetClip();
            }
            // замочная скважина
            using (SolidBrush b = new SolidBrush(Ink))
            {
                g.FillEllipse(b, 192 - 40, 316, 80, 80);
                g.FillPolygon(b, new PointF[] { new PointF(192 - 22, 380), new PointF(192 + 22, 380), new PointF(192 + 34, 450), new PointF(192 - 34, 450) });
            }
            using (SolidBrush b = new SolidBrush(A(Color.White, 90))) g.FillEllipse(b, 192 - 22, 326, 22, 18);
        }
        return bmp;
    }

    static Bitmap SliderKnob()
    {
        const int S = 128;
        Bitmap bmp = new Bitmap(S, S, PixelFormat.Format32bppArgb);
        using (Graphics g = Begin(bmp))
        {
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddEllipse(6, 6, S - 12, S - 12);
                FillV(g, p, Color.FromArgb(255, 255, 224, 120), Color.FromArgb(255, 216, 150, 40));
                using (Pen o = new Pen(Ink, 9f)) g.DrawPath(o, p);
            }
            using (SolidBrush b = new SolidBrush(A(Color.White, 130))) g.FillEllipse(b, 28, 20, 56, 28);
            using (Pen r = new Pen(A(Ink, 120), 5f)) g.DrawEllipse(r, 34, 36, 60, 60);
        }
        return bmp;
    }

    // Дорожка (тёмная) и заливка (золотая) слайдера: "таблетка" для 9-slice.
    static Bitmap SliderBar(bool fill)
    {
        const int W = 128, H = 40;
        Bitmap bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb);
        using (Graphics g = Begin(bmp))
        {
            using (GraphicsPath p = RRect(3, 3, W - 6, H - 6, 17))
            {
                if (fill) FillV(g, p, Color.FromArgb(255, 255, 214, 96), Color.FromArgb(255, 222, 152, 40));
                else FillV(g, p, Color.FromArgb(255, 40, 24, 18), Color.FromArgb(255, 70, 46, 30));
                using (Pen o = new Pen(Ink, 5f)) g.DrawPath(o, p);
                g.SetClip(p);
                using (Pen h = new Pen(A(Color.White, fill ? 110 : 40), 4f)) g.DrawLine(h, 0, 10, W, 10);
                g.ResetClip();
            }
        }
        return bmp;
    }
}
