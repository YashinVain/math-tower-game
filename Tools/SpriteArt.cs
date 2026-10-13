// Рисует персонажей, двери и блоки башни в том же плоском мультяшном стиле, что
// фоны (Tools/WallArt.cs, Tools/SceneryArt.cs): чистые формы, мягкий градиент
// света сверху-слева, тёмный контур для читаемости на любом фоне.
//
// Запуск: Tools/MakeSpriteArt.ps1. Написано на C# 5 (компилятор Windows
// PowerShell 5.1), без внешних программ.
//
// Правила для картинок (см. ArtSpecs): "ноги" касаются нижнего края картинки
// (pivot внизу по центру), высота картинки = высота в мире, поэтому весь
// персонаж занимает картинку по высоте. У трёх состояний двери одинаковый
// размер картинки. Блоки башни: каменная площадка внизу блока, на которой
// стоит гоблин, поднимается до 14.1% высоты (у верхнего блока — 11.7%).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public static class SpriteArt
{
    static readonly Color Outline = Color.FromArgb(255, 48, 32, 28);
    const float OW = 10f;

    public static void RenderAll(string charactersDir, string doorsDir, string towersDir)
    {
        Directory.CreateDirectory(charactersDir);
        Directory.CreateDirectory(doorsDir);
        Directory.CreateDirectory(towersDir);
        Save(Hero(), Path.Combine(charactersDir, "hero.png"));
        Save(Goblin(0), Path.Combine(charactersDir, "goblin.png"));
        Save(Goblin(1), Path.Combine(charactersDir, "goblin_2.png"));
        Save(Goblin(2), Path.Combine(charactersDir, "goblin_3.png"));
        Save(Door(0), Path.Combine(doorsDir, "door_closed.png"));
        Save(Door(1), Path.Combine(doorsDir, "door_open.png"));
        Save(Door(2), Path.Combine(doorsDir, "door_locked.png"));
        Save(Tower(0), Path.Combine(towersDir, "tower_base.png"));
        Save(Tower(1), Path.Combine(towersDir, "tower_mid.png"));
        Save(Tower(2), Path.Combine(towersDir, "tower_top.png"));
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

    static Graphics Begin(Bitmap bmp)
    {
        Graphics g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(Color.Transparent);
        return g;
    }

    // Заливка с градиентом (свет сверху-слева) и контуром.
    static void Shape(Graphics g, GraphicsPath p, Color fill, float contrast, bool outline)
    {
        RectangleF r = p.GetBounds();
        if (r.Width < 1) r.Width = 1;
        if (r.Height < 1) r.Height = 1;
        using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(r.X - 1, r.Y - 1, r.Width + 2, r.Height + 2), Mul(fill, 1f + contrast), Mul(fill, 1f - contrast), 55f))
            g.FillPath(b, p);
        if (outline)
            using (Pen pen = new Pen(Outline, OW)) { pen.LineJoin = LineJoin.Round; g.DrawPath(pen, p); }
    }

    static GraphicsPath Poly(params float[] xy)
    {
        GraphicsPath p = new GraphicsPath();
        PointF[] pts = new PointF[xy.Length / 2];
        for (int i = 0; i < pts.Length; i++) pts[i] = new PointF(xy[i * 2], xy[i * 2 + 1]);
        p.AddPolygon(pts);
        return p;
    }

    static GraphicsPath Ell(float x, float y, float w, float h)
    {
        GraphicsPath p = new GraphicsPath();
        p.AddEllipse(x, y, w, h);
        return p;
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

    static void Fill(Graphics g, GraphicsPath p, Color c)
    {
        using (SolidBrush b = new SolidBrush(c)) g.FillPath(b, p);
    }

    static void Limb(Graphics g, float x1, float y1, float x2, float y2, float w, Color c)
    {
        using (Pen o = new Pen(Outline, w + OW * 2f)) { o.StartCap = LineCap.Round; o.EndCap = LineCap.Round; g.DrawLine(o, x1, y1, x2, y2); }
        using (Pen p = new Pen(c, w)) { p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; g.DrawLine(p, x1, y1, x2, y2); }
        using (Pen h = new Pen(A(Color.White, 40), w * 0.3f)) { h.StartCap = LineCap.Round; h.EndCap = LineCap.Round; g.DrawLine(h, x1 - w * 0.15f, y1, x2 - w * 0.15f, y2); }
    }

    static void Dot(Graphics g, float cx, float cy, float r, Color c, bool outline)
    {
        using (GraphicsPath p = Ell(cx - r, cy - r, r * 2, r * 2)) Shape(g, p, c, 0.12f, outline);
    }

    // ---------- ГЕРОЙ (смотрит вправо) ----------
    static Bitmap Hero()
    {
        Bitmap bmp = new Bitmap(640, 1200, PixelFormat.Format32bppArgb);
        using (Graphics g = Begin(bmp))
        {
            Color skin = Rgb(250, 206, 160), skinDark = Rgb(226, 168, 124);
            Color hair = Rgb(122, 70, 36), tunic = Rgb(78, 156, 84), cape = Rgb(52, 104, 206);
            Color pants = Rgb(206, 178, 128), boot = Rgb(126, 76, 42), leather = Rgb(112, 70, 40), gold = Rgb(244, 196, 64);

            // тень на земле
            using (SolidBrush b = new SolidBrush(Color.FromArgb(60, 0, 0, 0))) g.FillEllipse(b, 130, 1168, 380, 30);

            // плащ (за спиной, развевается влево)
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddBezier(235, 430, 150, 540, 70, 850, 50, 1040);
                p.AddLine(50, 1040, 120, 1010);
                p.AddLine(120, 1010, 160, 1060);
                p.AddLine(160, 1060, 215, 1010);
                p.AddLine(215, 1010, 270, 1050);
                p.AddLine(270, 1050, 335, 960);
                p.AddBezier(335, 960, 390, 820, 400, 580, 410, 430);
                p.CloseFigure();
                Shape(g, p, cape, 0.2f, true);
                using (Pen hl = new Pen(A(Color.White, 45), 14f)) g.DrawBezier(hl, 215, 470, 150, 600, 110, 860, 100, 980);
            }

            // дальняя рука (у левого края)
            Limb(g, 232, 470, 188, 700, 54, tunic);
            Dot(g, 186, 730, 36, skin, true);

            // ноги и сапоги
            using (GraphicsPath p = Poly(218, 840, 306, 840, 300, 1110, 224, 1110)) Shape(g, p, pants, 0.12f, true);
            using (GraphicsPath p = Poly(342, 840, 436, 840, 432, 1110, 348, 1110)) Shape(g, p, pants, 0.12f, true);
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddLine(212, 1080, 308, 1080); p.AddLine(308, 1080, 312, 1158);
                p.AddBezier(312, 1158, 360, 1166, 380, 1180, 372, 1190);
                p.AddLine(372, 1190, 210, 1190); p.CloseFigure();
                Shape(g, p, boot, 0.14f, true);
            }
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddLine(340, 1080, 440, 1080); p.AddLine(440, 1080, 444, 1158);
                p.AddBezier(444, 1158, 500, 1166, 520, 1180, 512, 1190);
                p.AddLine(512, 1190, 336, 1190); p.CloseFigure();
                Shape(g, p, boot, 0.14f, true);
            }
            using (Pen cuff = new Pen(Mul(boot, 0.7f), 14f)) { g.DrawLine(cuff, 216, 1090, 306, 1090); g.DrawLine(cuff, 344, 1090, 438, 1090); }

            // туника с зубчатым подолом
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddBezier(222, 430, 330, 410, 340, 410, 440, 430);
                p.AddBezier(440, 430, 450, 600, 470, 780, 474, 890);
                p.AddLine(474, 890, 440, 860); p.AddLine(440, 860, 400, 902); p.AddLine(400, 902, 360, 862);
                p.AddLine(360, 862, 320, 904); p.AddLine(320, 904, 280, 862); p.AddLine(280, 862, 240, 902);
                p.AddLine(240, 902, 192, 880);
                p.AddBezier(192, 880, 202, 760, 214, 580, 222, 430);
                p.CloseFigure();
                Shape(g, p, tunic, 0.16f, true);
            }
            // ремень и пряжка
            using (GraphicsPath p = RRect(204, 716, 270, 50, 10)) Shape(g, p, leather, 0.15f, true);
            using (GraphicsPath p = RRect(318, 710, 58, 62, 10)) Shape(g, p, gold, 0.18f, true);
            using (GraphicsPath p = RRect(332, 725, 30, 32, 6)) Fill(g, p, Mul(leather, 0.8f));
            // воротник
            using (GraphicsPath p = Poly(280, 420, 380, 420, 346, 470, 312, 470)) Shape(g, p, Mul(tunic, 1.2f), 0.1f, true);

            // голова
            using (GraphicsPath p = RRect(302, 378, 56, 56, 10)) Shape(g, p, skinDark, 0.1f, true);
            using (GraphicsPath p = Ell(150, 215, 62, 84)) Shape(g, p, skin, 0.12f, true);           // ухо
            using (GraphicsPath p = Ell(165, 48, 340, 346)) Shape(g, p, skin, 0.14f, true);
            // румянец, нос, рот
            using (SolidBrush b = new SolidBrush(Color.FromArgb(90, 240, 120, 110))) g.FillEllipse(b, 398, 268, 58, 34);
            using (Pen p = new Pen(Outline, 7f)) { p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; g.DrawBezier(p, 492, 248, 508, 262, 500, 282, 482, 284); }
            using (Pen p = new Pen(Outline, 8f)) { p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; g.DrawBezier(p, 392, 316, 410, 336, 440, 334, 458, 312); }
            // глаз
            using (GraphicsPath p = Ell(352, 196, 78, 98)) { Fill(g, p, Color.White); using (Pen pen = new Pen(Outline, 7f)) g.DrawPath(pen, p); }
            using (GraphicsPath p = Ell(380, 216, 46, 66)) Fill(g, p, Rgb(54, 40, 30));
            using (GraphicsPath p = Ell(396, 226, 16, 18)) Fill(g, p, Color.White);
            // бровь
            using (GraphicsPath p = Poly(340, 186, 440, 150, 446, 176, 350, 214)) { Fill(g, p, Mul(hair, 0.8f)); using (Pen pen = new Pen(Outline, 6f)) { pen.LineJoin = LineJoin.Round; g.DrawPath(pen, p); } }

            // волосы: шапка и чёлка с зубцами
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddBezier(150, 270, 130, 120, 220, 18, 340, 14);
                p.AddBezier(340, 14, 450, 12, 520, 90, 510, 190);
                p.AddLine(510, 190, 470, 150); p.AddLine(470, 150, 452, 206); p.AddLine(452, 206, 418, 140);
                p.AddLine(418, 140, 392, 196); p.AddLine(392, 196, 356, 120); p.AddLine(356, 120, 326, 184);
                p.AddLine(326, 184, 286, 120); p.AddLine(286, 120, 252, 190); p.AddLine(252, 190, 214, 156);
                p.AddLine(214, 156, 196, 232);
                p.AddBezier(196, 232, 176, 262, 168, 300, 176, 330);
                p.AddLine(176, 330, 150, 270);
                p.CloseFigure();
                Shape(g, p, hair, 0.22f, true);
                using (Pen hl = new Pen(A(Color.White, 50), 12f)) g.DrawBezier(hl, 230, 90, 280, 40, 350, 34, 420, 56);
            }

            // меч в ближней руке (поднят вверх)
            using (GraphicsPath p = Poly(508, 692, 542, 692, 538, 380, 524, 322, 512, 380))
            {
                using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(505, 322, 40, 370), Rgb(236, 242, 250), Rgb(150, 164, 186), 0f)) g.FillPath(b, p);
                using (Pen pen = new Pen(Outline, OW)) { pen.LineJoin = LineJoin.Round; g.DrawPath(pen, p); }
                using (Pen hl = new Pen(A(Color.White, 150), 5f)) g.DrawLine(hl, 520, 670, 522, 400);
            }
            using (GraphicsPath p = RRect(478, 682, 94, 26, 8)) Shape(g, p, gold, 0.2f, true);
            using (GraphicsPath p = RRect(517, 706, 16, 74, 5)) Shape(g, p, leather, 0.15f, true);
            using (GraphicsPath p = Ell(512, 772, 26, 26)) Shape(g, p, gold, 0.2f, true);
            // ближняя рука
            Limb(g, 440, 462, 490, 600, 58, tunic);
            Limb(g, 490, 600, 524, 726, 52, tunic);
            Dot(g, 525, 732, 38, skin, true);
        }
        return bmp;
    }

    // ---------- ГОБЛИНЫ (смотрят влево) ----------
    // variant: 0 — обычный зелёный с дубиной, 1 — тёмный в красном с топором,
    //          2 — вожак в фиолетовом плаще с рогатым шлемом.
    static Bitmap Goblin(int variant)
    {
        int W = variant == 2 ? 760 : 700, Hh = 1180;
        Bitmap bmp = new Bitmap(W, Hh, PixelFormat.Format32bppArgb);
        float ox = (W - 700) / 2f; // центрируем общий рисунок
        using (Graphics g = Begin(bmp))
        {
            g.TranslateTransform(ox, 0);
            Color skin = variant == 0 ? Rgb(120, 182, 72) : (variant == 1 ? Rgb(92, 126, 78) : Rgb(104, 150, 92));
            Color skinDark = Mul(skin, 0.78f);
            Color vest = variant == 0 ? Rgb(150, 98, 54) : (variant == 1 ? Rgb(178, 52, 52) : Rgb(130, 134, 150));
            Color pants = variant == 0 ? Rgb(104, 76, 52) : (variant == 1 ? Rgb(66, 54, 70) : Rgb(86, 60, 110));
            Color eye = variant == 1 ? Rgb(255, 80, 60) : Rgb(255, 214, 60);
            Color wood = Rgb(138, 90, 52), metal = Rgb(182, 190, 206);

            using (SolidBrush b = new SolidBrush(Color.FromArgb(60, 0, 0, 0))) g.FillEllipse(b, 120, 1150, 460, 30);

            // плащ вожака (за спиной)
            if (variant == 2)
            {
                using (GraphicsPath p = new GraphicsPath())
                {
                    p.AddBezier(250, 470, 130, 600, 80, 900, 90, 1110);
                    p.AddLine(90, 1110, 160, 1080); p.AddLine(160, 1080, 220, 1130); p.AddLine(220, 1130, 290, 1080);
                    p.AddLine(290, 1080, 360, 1130); p.AddLine(360, 1130, 440, 1080); p.AddLine(440, 1080, 520, 1120);
                    p.AddBezier(520, 1120, 600, 900, 580, 640, 470, 470);
                    p.CloseFigure();
                    Shape(g, p, Rgb(110, 50, 150), 0.2f, true);
                }
            }

            // дальняя рука (справа)
            Limb(g, 470, 560, 560, 720, 56, skinDark);
            Dot(g, 566, 748, 38, skinDark, true);

            // ноги и ступни
            using (GraphicsPath p = Poly(246, 920, 340, 920, 336, 1110, 240, 1110)) Shape(g, p, skinDark, 0.1f, true);
            using (GraphicsPath p = Poly(380, 920, 474, 920, 480, 1110, 384, 1110)) Shape(g, p, skinDark, 0.1f, true);
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddBezier(236, 1100, 160, 1110, 140, 1150, 168, 1170);
                p.AddLine(168, 1170, 350, 1170); p.AddLine(350, 1170, 346, 1100); p.CloseFigure();
                Shape(g, p, skin, 0.12f, true);
            }
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddBezier(376, 1100, 300, 1110, 280, 1150, 308, 1170);
                p.AddLine(308, 1170, 500, 1170); p.AddLine(500, 1170, 490, 1100); p.CloseFigure();
                Shape(g, p, skin, 0.12f, true);
            }

            // набедренная повязка
            using (GraphicsPath p = Poly(236, 860, 484, 860, 492, 1010, 450, 980, 410, 1030, 360, 984, 310, 1034, 270, 982, 230, 1012))
                Shape(g, p, pants, 0.14f, true);

            // туловище с пузом
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddBezier(250, 470, 190, 560, 190, 800, 250, 920);
                p.AddBezier(250, 920, 330, 960, 410, 960, 480, 920);
                p.AddBezier(480, 920, 540, 800, 540, 560, 470, 470);
                p.CloseFigure();
                Shape(g, p, skin, 0.16f, true);
            }
            // жилет / броня
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddBezier(262, 480, 230, 580, 232, 760, 262, 840);
                p.AddLine(262, 840, 460, 840);
                p.AddBezier(460, 840, 500, 760, 500, 580, 466, 480);
                p.AddLine(466, 480, 420, 520); p.AddLine(420, 520, 360, 470); p.AddLine(360, 470, 300, 520);
                p.CloseFigure();
                Shape(g, p, vest, 0.18f, true);
                using (Pen st = new Pen(A(Color.Black, 70), 5f)) { st.DashStyle = DashStyle.Dash; g.DrawLine(st, 360, 480, 360, 835); }
            }
            using (GraphicsPath p = RRect(236, 830, 252, 46, 10)) Shape(g, p, Rgb(88, 62, 40), 0.15f, true);
            using (GraphicsPath p = RRect(334, 826, 60, 54, 8)) Shape(g, p, Rgb(220, 180, 70), 0.18f, true);
            if (variant == 1)
                for (int i = 0; i < 4; i++) { using (GraphicsPath p = Poly(272 + i * 52, 484, 292 + i * 52, 484, 282 + i * 52, 530)) Shape(g, p, metal, 0.12f, true); } // шипы

            // оружие в ближней руке (слева)
            if (variant == 0)
            {
                // дубина
                using (GraphicsPath p = new GraphicsPath())
                {
                    p.AddPolygon(new PointF[] { new PointF(104, 790), new PointF(150, 790), new PointF(176, 520), new PointF(210, 400), new PointF(160, 300), new PointF(84, 320), new PointF(60, 420), new PointF(96, 540) });
                    Shape(g, p, wood, 0.2f, true);
                }
                using (Pen w = new Pen(Mul(wood, 0.6f), 10f)) { g.DrawLine(w, 106, 680, 154, 690); g.DrawLine(w, 112, 740, 150, 746); }
                foreach (float[] s in new float[][] { new float[] { 70, 340, 30, 320, 76, 380 }, new float[] { 196, 360, 236, 340, 190, 410 }, new float[] { 130, 300, 140, 252, 170, 306 } })
                    using (GraphicsPath p = Poly(s)) Shape(g, p, metal, 0.12f, true);
            }
            else if (variant == 1)
            {
                // топор
                using (GraphicsPath p = Poly(112, 800, 144, 800, 150, 330, 118, 330)) Shape(g, p, wood, 0.2f, true);
                using (GraphicsPath p = new GraphicsPath())
                {
                    p.AddBezier(140, 340, 230, 330, 270, 400, 262, 470);
                    p.AddBezier(262, 470, 220, 450, 190, 470, 146, 480);
                    p.AddLine(146, 480, 140, 340);
                    p.CloseFigure();
                    Shape(g, p, metal, 0.22f, true);
                }
                using (GraphicsPath p = new GraphicsPath())
                {
                    p.AddBezier(122, 340, 40, 330, 0, 400, 14, 470);
                    p.AddBezier(14, 470, 56, 450, 90, 470, 124, 480);
                    p.CloseFigure();
                    Shape(g, p, metal, 0.22f, true);
                }
            }
            else
            {
                // большой топор вожака с шипом
                using (GraphicsPath p = Poly(104, 820, 140, 820, 148, 250, 112, 250)) Shape(g, p, Rgb(80, 56, 44), 0.2f, true);
                using (GraphicsPath p = new GraphicsPath())
                {
                    p.AddBezier(146, 270, 270, 220, 330, 330, 306, 450);
                    p.AddBezier(306, 450, 250, 410, 190, 430, 146, 450);
                    p.CloseFigure();
                    Shape(g, p, metal, 0.24f, true);
                }
                using (GraphicsPath p = Poly(112, 270, 124, 190, 140, 270)) Shape(g, p, metal, 0.2f, true);
            }
            // ближняя рука
            Limb(g, 256, 540, 160, 650, 62, skin);
            Limb(g, 160, 650, 128, 770, 56, skin);
            Dot(g, 126, 780, 40, skin, true);

            // голова: уши, лицо
            using (GraphicsPath p = Poly(236, 250, 40, 150, 30, 214, 70, 262, 238, 338)) Shape(g, p, skin, 0.14f, true);
            using (GraphicsPath p = Poly(484, 250, 680, 160, 672, 226, 632, 268, 482, 338)) Shape(g, p, skin, 0.14f, true);
            using (GraphicsPath p = Poly(92, 196, 60, 178, 80, 244)) Fill(g, p, A(Color.Black, 40));
            using (GraphicsPath p = Ell(210, 120, 300, 290)) Shape(g, p, skin, 0.16f, true);
            // бородавка и тёмные щёки
            Dot(g, 430, 330, 10, skinDark, false);
            // глаза
            foreach (float ex in new float[] { 268, 366 })
            {
                using (GraphicsPath p = Ell(ex, 236, 76, 62)) { Fill(g, p, eye); using (Pen pen = new Pen(Outline, 6f)) g.DrawPath(pen, p); }
                using (GraphicsPath p = Ell(ex + 18, 242, 22, 50)) Fill(g, p, Rgb(40, 20, 20));
                using (GraphicsPath p = Ell(ex + 40, 248, 12, 12)) Fill(g, p, Color.White);
            }
            // злые брови
            using (GraphicsPath p = Poly(252, 216, 350, 250, 346, 276, 250, 246)) { Fill(g, p, Mul(skin, 0.6f)); using (Pen pen = new Pen(Outline, 6f)) { pen.LineJoin = LineJoin.Round; g.DrawPath(pen, p); } }
            using (GraphicsPath p = Poly(468, 216, 372, 250, 376, 276, 470, 246)) { Fill(g, p, Mul(skin, 0.6f)); using (Pen pen = new Pen(Outline, 6f)) { pen.LineJoin = LineJoin.Round; g.DrawPath(pen, p); } }
            // нос
            using (GraphicsPath p = Ell(320, 286, 80, 68)) Shape(g, p, skinDark, 0.14f, true);
            // пасть с клыками
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddBezier(272, 366, 320, 410, 410, 410, 452, 366);
                p.AddBezier(452, 366, 410, 384, 320, 384, 272, 366);
                p.CloseFigure();
                Fill(g, p, Rgb(60, 24, 28)); using (Pen pen = new Pen(Outline, 6f)) g.DrawPath(pen, p);
            }
            using (GraphicsPath p = Poly(300, 376, 322, 376, 311, 408)) { Fill(g, p, Color.White); }
            using (GraphicsPath p = Poly(402, 376, 424, 376, 413, 408)) { Fill(g, p, Color.White); }

            // головные уборы
            if (variant == 1)
            {
                // красная повязка с узлом
                using (GraphicsPath p = Poly(214, 168, 506, 168, 508, 214, 212, 214)) Shape(g, p, Rgb(200, 48, 52), 0.16f, true);
                using (GraphicsPath p = Poly(212, 190, 140, 168, 160, 214, 130, 250, 200, 224)) Shape(g, p, Rgb(200, 48, 52), 0.16f, true);
            }
            else if (variant == 2)
            {
                // рогатый шлем
                using (Pen horn = new Pen(Rgb(240, 232, 210), 30f)) { horn.StartCap = LineCap.Round; horn.EndCap = LineCap.Round; g.DrawBezier(horn, 230, 160, 150, 140, 104, 100, 118, 40); g.DrawBezier(horn, 490, 160, 570, 140, 616, 100, 602, 40); }
                using (GraphicsPath p = new GraphicsPath())
                {
                    p.AddBezier(212, 230, 200, 100, 520, 100, 508, 230);
                    p.AddLine(508, 230, 212, 230);
                    p.CloseFigure();
                    Shape(g, p, Rgb(120, 126, 146), 0.22f, true);
                }
                using (GraphicsPath p = RRect(208, 196, 304, 36, 10)) Shape(g, p, Rgb(224, 184, 70), 0.2f, true);
            }
        }
        return bmp;
    }

    // ---------- ДВЕРИ ----------
    // state: 0 закрыта, 1 открыта (за ней тёплый свет), 2 заперта (цепи и замок)
    static Bitmap Door(int state)
    {
        const int W = 1085, Hh = 1233;
        Bitmap bmp = new Bitmap(W, Hh, PixelFormat.Format32bppArgb);
        using (Graphics g = Begin(bmp))
        {
            Color stone = Rgb(150, 150, 160), wood = Rgb(132, 84, 48), iron = Rgb(60, 58, 66);
            float cx = W / 2f;
            float outerR = 500, innerR = 380, archCy = 540;
            float baseY = Hh - 6;

            // тень на земле
            using (SolidBrush b = new SolidBrush(Color.FromArgb(60, 0, 0, 0))) g.FillEllipse(b, 40, baseY - 18, W - 80, 26);

            // каменная арка: внешний контур
            using (GraphicsPath outer = new GraphicsPath())
            {
                outer.AddArc(cx - outerR, archCy - outerR, outerR * 2, outerR * 2, 180, 180);
                outer.AddLine(cx + outerR, archCy, cx + outerR, baseY);
                outer.AddLine(cx + outerR, baseY, cx - outerR, baseY);
                outer.CloseFigure();
                Shape(g, outer, stone, 0.14f, true);

                // кладка: радиальные швы по арке и блоки по бокам
                g.SetClip(outer);
                using (Pen seam = new Pen(A(Outline, 200), 7f))
                {
                    for (int a = 0; a <= 180; a += 18)
                    {
                        double r = a * Math.PI / 180.0;
                        g.DrawLine(seam, cx - (float)Math.Cos(r) * innerR, archCy - (float)Math.Sin(r) * innerR, cx - (float)Math.Cos(r) * outerR, archCy - (float)Math.Sin(r) * outerR);
                    }
                    for (float y = archCy; y < baseY; y += 112)
                    {
                        g.DrawLine(seam, cx - outerR, y, cx - innerR, y);
                        g.DrawLine(seam, cx + innerR, y, cx + outerR, y);
                    }
                    for (float y = archCy + 56; y < baseY; y += 112)
                    {
                        g.DrawLine(seam, cx - outerR + 70, y, cx - outerR + 70, y + 0);
                    }
                }
                using (Pen hl = new Pen(A(Color.White, 60), 10f)) g.DrawArc(hl, cx - outerR + 14, archCy - outerR + 14, outerR * 2 - 28, outerR * 2 - 28, 190, 120);
                g.ResetClip();
                using (Pen pen = new Pen(Outline, OW)) { pen.LineJoin = LineJoin.Round; g.DrawPath(pen, outer); }
            }
            // замковый камень сверху
            using (GraphicsPath p = Poly(cx - 44, archCy - outerR - 4, cx + 44, archCy - outerR - 4, cx + 36, archCy - innerR + 8, cx - 36, archCy - innerR + 8))
                Shape(g, p, Mul(stone, 1.18f), 0.12f, true);

            // проём
            using (GraphicsPath opening = new GraphicsPath())
            {
                opening.AddArc(cx - innerR, archCy - innerR, innerR * 2, innerR * 2, 180, 180);
                opening.AddLine(cx + innerR, archCy, cx + innerR, baseY);
                opening.AddLine(cx + innerR, baseY, cx - innerR, baseY);
                opening.CloseFigure();

                if (state == 1)
                {
                    // за открытой дверью — тёплый свет
                    using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(cx - innerR, archCy - innerR, innerR * 2, baseY - archCy + innerR), Rgb(255, 238, 170), Rgb(236, 150, 60), 90f))
                        g.FillPath(b, opening);
                    g.SetClip(opening);
                    using (GraphicsPath gp = new GraphicsPath())
                    {
                        gp.AddEllipse(cx - 320, archCy - 120, 640, 760);
                        using (PathGradientBrush b = new PathGradientBrush(gp)) { b.CenterColor = Color.FromArgb(200, 255, 255, 230); b.SurroundColors = new Color[] { Color.FromArgb(0, 255, 255, 230) }; g.FillPath(b, gp); }
                    }
                    g.ResetClip();
                    // ступени порога
                    using (GraphicsPath p = Poly(cx - innerR, baseY - 40, cx + innerR, baseY - 40, cx + innerR, baseY, cx - innerR, baseY)) Fill(g, p, Mul(stone, 0.9f));
                    // створка открыта: узкая трапеция слева
                    using (GraphicsPath leaf = Poly(cx - innerR, archCy - 150, cx - innerR + 170, archCy - 90, cx - innerR + 170, baseY - 30, cx - innerR, baseY))
                    {
                        Shape(g, leaf, wood, 0.18f, true);
                        using (Pen pl = new Pen(A(Outline, 160), 5f)) { g.DrawLine(pl, cx - innerR + 56, archCy - 130, cx - innerR + 56, baseY - 14); g.DrawLine(pl, cx - innerR + 112, archCy - 110, cx - innerR + 112, baseY - 22); }
                        using (GraphicsPath s = Poly(cx - innerR, archCy + 120, cx - innerR + 170, archCy + 130, cx - innerR + 170, archCy + 180, cx - innerR, archCy + 168)) Shape(g, s, iron, 0.2f, true);
                    }
                }
                else
                {
                    // закрытая дверь: доски
                    Shape(g, opening, wood, 0.16f, false);
                    g.SetClip(opening);
                    float plank = innerR * 2 / 6f;
                    for (int i = 0; i < 6; i++)
                    {
                        float x = cx - innerR + i * plank;
                        using (SolidBrush b = new SolidBrush(Color.FromArgb(i % 2 == 0 ? 0 : 26, 0, 0, 0))) g.FillRectangle(b, x, 0, plank, Hh);
                        using (Pen seam = new Pen(A(Outline, 220), 6f)) g.DrawLine(seam, x, 0, x, Hh);
                        using (Pen grain = new Pen(A(Color.Black, 36), 3f)) { g.DrawLine(grain, x + plank * 0.3f, archCy - 200, x + plank * 0.3f, baseY - 20); g.DrawLine(grain, x + plank * 0.7f, archCy - 120, x + plank * 0.7f, baseY - 60); }
                    }
                    using (Pen hl = new Pen(A(Color.White, 40), 16f)) g.DrawLine(hl, cx - innerR + 30, archCy - 100, cx - innerR + 30, baseY - 30);
                    // железные полосы с заклёпками
                    foreach (float sy in new float[] { archCy + 90, archCy + 460 })
                    {
                        using (GraphicsPath s = RRect(cx - innerR - 4, sy, innerR * 2 + 8, 62, 8)) Shape(g, s, iron, 0.24f, true);
                        for (float rx = cx - innerR + 40; rx < cx + innerR - 20; rx += 90)
                            using (SolidBrush b = new SolidBrush(Rgb(120, 118, 128))) g.FillEllipse(b, rx - 9, sy + 22, 18, 18);
                    }
                    g.ResetClip();
                    // кольцо-ручка
                    using (GraphicsPath p = Ell(cx + 160, archCy + 250, 74, 74)) Shape(g, p, iron, 0.2f, true);
                    using (Pen ring = new Pen(Outline, 26f)) g.DrawEllipse(ring, cx + 170, archCy + 300, 84, 84);
                    using (Pen ring = new Pen(Rgb(190, 192, 204), 12f)) g.DrawEllipse(ring, cx + 170, archCy + 300, 84, 84);
                    if (state == 2)
                    {
                        // цепи крест-накрест и замок
                        for (int dir = -1; dir <= 1; dir += 2)
                        {
                            float x1 = cx - innerR * dir, y1 = archCy + 80, x2 = cx + innerR * dir, y2 = baseY - 90;
                            float len = (float)Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                            int n = (int)(len / 54);
                            for (int i = 0; i <= n; i++)
                            {
                                float t = i / (float)n, lx = x1 + (x2 - x1) * t, ly = y1 + (y2 - y1) * t;
                                float ang = (float)(Math.Atan2(y2 - y1, x2 - x1) * 180.0 / Math.PI) + (i % 2 == 0 ? 0 : 90);
                                GraphicsState st = g.Save();
                                g.TranslateTransform(lx, ly);
                                g.RotateTransform(ang);
                                using (GraphicsPath link = RRect(-34, -17, 68, 34, 17)) { using (Pen o = new Pen(Outline, 12f)) g.DrawPath(o, link); using (Pen m = new Pen(Rgb(170, 172, 184), 7f)) g.DrawPath(m, link); }
                                g.Restore(st);
                            }
                        }
                        using (Pen sh = new Pen(Outline, 34f)) { sh.StartCap = LineCap.Round; sh.EndCap = LineCap.Round; g.DrawArc(sh, cx - 62, archCy + 340, 124, 150, 180, 180); }
                        using (Pen sh = new Pen(Rgb(190, 192, 204), 18f)) { sh.StartCap = LineCap.Round; sh.EndCap = LineCap.Round; g.DrawArc(sh, cx - 62, archCy + 340, 124, 150, 180, 180); }
                        using (GraphicsPath body = RRect(cx - 90, archCy + 410, 180, 150, 22)) Shape(g, body, Rgb(244, 196, 64), 0.2f, true);
                        using (GraphicsPath hole = Ell(cx - 18, archCy + 450, 36, 36)) Fill(g, hole, Outline);
                        using (GraphicsPath hole = Poly(cx - 10, archCy + 478, cx + 10, archCy + 478, cx + 16, archCy + 530, cx - 16, archCy + 530)) Fill(g, hole, Outline);
                        using (SolidBrush b = new SolidBrush(Color.FromArgb(70, 120, 0, 0))) g.FillPath(b, opening);
                    }
                }
                using (Pen pen = new Pen(Outline, OW)) { pen.LineJoin = LineJoin.Round; g.DrawPath(pen, opening); }
            }
        }
        return bmp;
    }

    // ---------- БЛОКИ БАШНИ ----------
    // kind: 0 нижний этаж, 1 средний, 2 верхний (с зубцами и флагом).
    // Картинка 1254×1254 (верхний блок в игре увеличивается в 1.15 раза).
    static Bitmap Tower(int kind)
    {
        const int S = 1254;
        Bitmap bmp = new Bitmap(S, S, PixelFormat.Format32bppArgb);
        using (Graphics g = Begin(bmp))
        {
            Color stone = Rgb(150, 150, 164), stoneDark = Rgb(98, 98, 116), wood = Rgb(126, 82, 48), room = Rgb(70, 66, 84);
            float cx = S / 2f;
            float wallHalf = 464;                       // половина ширины стены (0.87 мира)
            float ledgeHalf = 608;                      // половина ширины площадки (1.14 мира)
            float ledgeTop = kind == 2 ? S * (1 - 0.117f) : S * (1 - 0.141f); // верх площадки (y)
            float top = kind == 2 ? 300 : 0;            // верх стены (у верхнего блока выше — зубцы)

            // комната (задняя стена) и каменная рама стены
            using (GraphicsPath wall = Poly(cx - wallHalf, top, cx + wallHalf, top, cx + wallHalf, ledgeTop, cx - wallHalf, ledgeTop))
            {
                Shape(g, wall, stone, 0.12f, false);
                g.SetClip(wall);
                using (Pen seam = new Pen(A(Outline, 150), 5f))
                {
                    int row = 0;
                    for (float y = top; y < ledgeTop; y += 74, row++)
                    {
                        g.DrawLine(seam, cx - wallHalf, y, cx + wallHalf, y);
                        for (float x = cx - wallHalf + (row % 2) * 60; x < cx + wallHalf; x += 120) g.DrawLine(seam, x, y, x, y + 74);
                    }
                }
                g.ResetClip();
            }
            // проём комнаты (тёмный) между каменными столбами
            float openL = cx - wallHalf + 92, openR = cx + wallHalf - 92, openT = top + (kind == 2 ? 80 : 70);
            using (GraphicsPath inner = Poly(openL, openT, openR, openT, openR, ledgeTop, openL, ledgeTop))
            {
                using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(openL, openT - 1, openR - openL, ledgeTop - openT + 2), Mul(room, 1.25f), Mul(room, 0.7f), 90f)) g.FillPath(b, inner);
                g.SetClip(inner);
                using (Pen seam = new Pen(A(Color.Black, 70), 4f))
                {
                    int row = 0;
                    for (float y = openT; y < ledgeTop; y += 62, row++)
                    {
                        g.DrawLine(seam, openL, y, openR, y);
                        for (float x = openL + (row % 2) * 50; x < openR; x += 100) g.DrawLine(seam, x, y, x, y + 62);
                    }
                }
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(openL - 160, openT - 120, 520, 520);
                    using (PathGradientBrush b = new PathGradientBrush(gp)) { b.CenterColor = Color.FromArgb(120, 255, 190, 100); b.SurroundColors = new Color[] { Color.FromArgb(0, 255, 190, 100) }; g.FillPath(b, gp); }
                }
                g.ResetClip();
                using (Pen pen = new Pen(Outline, OW)) g.DrawPath(pen, inner);
            }
            // деревянная балка над проёмом и стойки
            using (GraphicsPath p = RRect(openL - 40, openT - 54, openR - openL + 80, 54, 8)) Shape(g, p, wood, 0.22f, true);
            using (GraphicsPath p = RRect(openL - 36, openT, 36, ledgeTop - openT, 6)) Shape(g, p, wood, 0.22f, true);
            using (GraphicsPath p = RRect(openR, openT, 36, ledgeTop - openT, 6)) Shape(g, p, wood, 0.22f, true);
            // внешний контур стены
            using (Pen pen = new Pen(Outline, OW)) g.DrawRectangle(pen, cx - wallHalf, top, wallHalf * 2, ledgeTop - top);

            // боковая тень (объём): правая сторона темнее
            using (SolidBrush b = new SolidBrush(Color.FromArgb(46, 0, 0, 0))) g.FillRectangle(b, cx + wallHalf - 90, top + 4, 86, ledgeTop - top - 4);

            if (kind == 2)
            {
                // зубцы на крыше и флаг
                float tY = top;
                using (GraphicsPath p = RRect(cx - wallHalf - 40, tY - 26, wallHalf * 2 + 80, 56, 10)) Shape(g, p, Mul(stone, 1.1f), 0.12f, true);
                for (int i = 0; i < 5; i++)
                {
                    float bx = cx - wallHalf - 40 + i * ((wallHalf * 2 + 80 - 90) / 4f);
                    using (GraphicsPath p = RRect(bx, tY - 130, 90, 110, 8)) Shape(g, p, Mul(stone, 1.1f), 0.12f, true);
                }
                using (Pen pole = new Pen(Outline, 22f)) g.DrawLine(pole, cx + 250, tY - 126, cx + 250, tY - 290);
                using (Pen pole = new Pen(Rgb(150, 100, 60), 10f)) g.DrawLine(pole, cx + 250, tY - 126, cx + 250, tY - 290);
                using (GraphicsPath p = Poly(cx + 256, tY - 288, cx + 410, tY - 262, cx + 366, tY - 236, cx + 410, tY - 204, cx + 256, tY - 184)) Shape(g, p, Rgb(206, 48, 56), 0.2f, true);
            }

            // площадка (выступающий этаж)
            using (GraphicsPath ledge = RRect(cx - ledgeHalf, ledgeTop, ledgeHalf * 2, S - ledgeTop, 18))
            {
                Shape(g, ledge, Mul(stone, 1.06f), 0.14f, false);
                g.SetClip(ledge);
                using (Pen seam = new Pen(A(Outline, 160), 5f))
                {
                    float rowH = (S - ledgeTop) / 2f;
                    g.DrawLine(seam, cx - ledgeHalf, ledgeTop + rowH, cx + ledgeHalf, ledgeTop + rowH);
                    for (float x = cx - ledgeHalf + 70; x < cx + ledgeHalf; x += 150) g.DrawLine(seam, x, ledgeTop, x, ledgeTop + rowH);
                    for (float x = cx - ledgeHalf + 140; x < cx + ledgeHalf; x += 150) g.DrawLine(seam, x, ledgeTop + rowH, x, S);
                }
                using (Pen hl = new Pen(A(Color.White, 90), 10f)) g.DrawLine(hl, cx - ledgeHalf + 18, ledgeTop + 8, cx + ledgeHalf - 18, ledgeTop + 8);
                g.ResetClip();
                using (Pen pen = new Pen(Outline, OW)) { pen.LineJoin = LineJoin.Round; g.DrawPath(pen, ledge); }
            }
        }
        return bmp;
    }
}
