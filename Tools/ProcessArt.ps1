# Подготовка картинок для игры: убирает белый фон у персонажей, дверей и
# иконок и обрезает пустые поля вокруг. Запуск (из корня проекта):
#
#   powershell -ExecutionPolicy Bypass -File Tools\ProcessArt.ps1
#
# Что делает:
#  1. Копирует исходные картинки (как их выдала нейросеть) в папку ArtSource\
#     (только если там их ещё нет) - это "чистовик", из которого всегда можно
#     переделать обработку заново.
#  2. Берёт картинки из ArtSource\, убирает фон и кладёт готовые PNG в
#     Assets\_Project\Art\ (Unity подхватит их сам).
#
# Как убирается фон: от краёв картинки "заливкой" закрашивается почти-белая
# область (как инструмент "заливка"/"волшебная палочка" в графическом
# редакторе). Белые детали ВНУТРИ рисунка (например, белки глаз) не
# трогаются, потому что до них заливка не доходит - их отделяет тёмная
# обводка. Исключение - большие замкнутые белые области (например, просвет
# между рукой и телом, где виден фон): они убираются тоже, порог размера
# задан параметром pocketMin. На границе рисунка делается мягкий
# (полупрозрачный) край, чтобы вокруг персонажа не было белой каймы.
#
# Сообщения скрипта специально на английском: консоль Windows часто
# неправильно показывает русские буквы.

$ErrorActionPreference = 'Stop'

$root   = Split-Path -Parent $PSScriptRoot
$art    = Join-Path $root 'Assets\_Project\Art'
$source = Join-Path $root 'ArtSource'

Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

public class ArtImage
{
    public int W, H;
    public byte[] P; // BGRA, 4 bytes per pixel, rows without padding

    public static ArtImage Load(string path)
    {
        using (Bitmap src = new Bitmap(path))
        {
            ArtImage img = new ArtImage();
            img.W = src.Width;
            img.H = src.Height;
            img.P = new byte[img.W * img.H * 4];
            BitmapData bd = src.LockBits(new Rectangle(0, 0, img.W, img.H), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(bd.Scan0, img.P, 0, img.P.Length);
            src.UnlockBits(bd);
            return img;
        }
    }

    public void Save(string path)
    {
        using (Bitmap dst = new Bitmap(W, H, PixelFormat.Format32bppArgb))
        {
            BitmapData bd = dst.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(P, 0, bd.Scan0, P.Length);
            dst.UnlockBits(bd);
            dst.Save(path, ImageFormat.Png);
        }
    }

    bool IsLight(int i, int thr)
    {
        int o = i * 4;
        if (P[o + 3] == 0) return true;
        return Math.Min(P[o], Math.Min(P[o + 1], P[o + 2])) >= thr;
    }

    void Seed(bool[] bg, int[] q, ref int qt, int i, int thr)
    {
        if (bg[i] || !IsLight(i, thr)) return;
        bg[i] = true; q[qt++] = i;
    }

    // Removes the white background. Step 1: flood fill from the image border.
    // Step 2: enclosed near-white areas that are at least pocketMin pixels
    // big (gaps between an arm and a body etc.) are removed too; smaller
    // ones (eye whites) stay. Step 3: soft edge. Returns a text report.
    public string RemoveWhite(int thr, int pocketMin)
    {
        int n = W * H;
        bool[] bg = new bool[n];
        int[] q = new int[n];
        int qh = 0, qt = 0;

        for (int x = 0; x < W; x++) { Seed(bg, q, ref qt, x, thr); Seed(bg, q, ref qt, (H - 1) * W + x, thr); }
        for (int y = 0; y < H; y++) { Seed(bg, q, ref qt, y * W, thr); Seed(bg, q, ref qt, y * W + W - 1, thr); }

        while (qh < qt)
        {
            int i = q[qh++];
            int x = i % W, y = i / W;
            if (x > 0) Seed(bg, q, ref qt, i - 1, thr);
            if (x < W - 1) Seed(bg, q, ref qt, i + 1, thr);
            if (y > 0) Seed(bg, q, ref qt, i - W, thr);
            if (y < H - 1) Seed(bg, q, ref qt, i + W, thr);
        }

        // Enclosed near-white components.
        StringBuilder report = new StringBuilder();
        bool[] seen = new bool[n];
        for (int s = 0; s < n; s++)
        {
            if (bg[s] || seen[s] || !IsLight(s, thr)) continue;
            int head = 0, tail = 0, size = 0;
            long sx = 0, sy = 0;
            seen[s] = true; q[tail++] = s;
            while (head < tail)
            {
                int i = q[head++]; size++;
                int x = i % W, y = i / W;
                sx += x; sy += y;
                int[] nb = { x > 0 ? i - 1 : -1, x < W - 1 ? i + 1 : -1, y > 0 ? i - W : -1, y < H - 1 ? i + W : -1 };
                for (int k = 0; k < 4; k++)
                {
                    int j = nb[k];
                    if (j < 0 || bg[j] || seen[j] || !IsLight(j, thr)) continue;
                    seen[j] = true; q[tail++] = j;
                }
            }
            if (size < 150) continue;
            bool remove = size >= pocketMin;
            report.Append(string.Format(" [{0}px at {1},{2}: {3}]", size, sx / size, sy / size, remove ? "removed" : "kept"));
            if (remove)
                for (int k = 0; k < tail; k++) bg[q[k]] = true;
        }

        // Soft edge: light pixels of the drawing that touch the background
        // are anti-aliasing against white. Make them semi-transparent and
        // subtract the white that is mixed into their colour.
        byte[] newA = new byte[n];
        for (int i = 0; i < n; i++) newA[i] = bg[i] ? (byte)0 : P[i * 4 + 3];
        for (int i = 0; i < n; i++)
        {
            if (bg[i]) continue;
            int x = i % W, y = i / W;
            bool nearBg = false;
            for (int dy = -1; dy <= 1 && !nearBg; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
                    if (bg[ny * W + nx]) { nearBg = true; break; }
                }
            if (!nearBg) continue;

            int o = i * 4;
            int m = Math.Min(P[o], Math.Min(P[o + 1], P[o + 2]));
            if (m < 170) continue;
            double f = (double)(thr - m) / (thr - 170);
            if (f < 0.05) f = 0.05;
            if (f > 1) f = 1;
            for (int c = 0; c < 3; c++)
            {
                double v = (P[o + c] - 255.0 * (1 - f)) / f;
                P[o + c] = (byte)Math.Max(0, Math.Min(255, v));
            }
            newA[i] = (byte)(255 * f);
        }

        for (int i = 0; i < n; i++)
        {
            P[i * 4 + 3] = newA[i];
            if (bg[i]) { P[i * 4] = 0; P[i * 4 + 1] = 0; P[i * 4 + 2] = 0; }
        }
        return report.ToString();
    }

    // Bounding box of the visible part.
    public Rectangle Bounds(int alphaMin)
    {
        int x0 = W, y0 = H, x1 = -1, y1 = -1;
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                if (P[(y * W + x) * 4 + 3] > alphaMin)
                {
                    if (x < x0) x0 = x;
                    if (x > x1) x1 = x;
                    if (y < y0) y0 = y;
                    if (y > y1) y1 = y;
                }
        if (x1 < 0) return new Rectangle(0, 0, W, H);
        return new Rectangle(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
    }

    public ArtImage Crop(Rectangle r, int pad)
    {
        int x0 = Math.Max(0, r.X - pad), y0 = Math.Max(0, r.Y - pad);
        int x1 = Math.Min(W, r.Right + pad), y1 = Math.Min(H, r.Bottom + pad);
        ArtImage o = new ArtImage();
        o.W = x1 - x0; o.H = y1 - y0;
        o.P = new byte[o.W * o.H * 4];
        for (int y = 0; y < o.H; y++)
            Buffer.BlockCopy(P, ((y0 + y) * W + x0) * 4, o.P, y * o.W * 4, o.W * 4);
        return o;
    }

    public static Rectangle Union(Rectangle a, Rectangle b)
    {
        return Rectangle.Union(a, b);
    }
}
'@

function Backup-Original($relPath) {
    $dst = Join-Path $source $relPath
    if (-not (Test-Path $dst)) {
        New-Item -ItemType Directory -Force (Split-Path -Parent $dst) | Out-Null
        Copy-Item (Join-Path $art $relPath) $dst
        Write-Host "  original saved: ArtSource\$relPath"
    }
    return $dst
}

# One file: remove the background (if it is not transparent yet) and crop.
function Process-Single($relPath, [bool]$removeBg, [int]$pocketMin) {
    $src = Backup-Original $relPath
    $img = [ArtImage]::Load($src)
    $info = ''
    if ($removeBg) { $info = $img.RemoveWhite(235, $pocketMin) }
    $r = $img.Bounds(8)
    $out = $img.Crop($r, 3)
    $out.Save((Join-Path $art $relPath))
    Write-Host ("{0}: {1}x{2} -> {3}x{4} {5}" -f $relPath, $img.W, $img.H, $out.W, $out.H, $info)
}

# A group of files: all are cropped with one shared box (so that, for
# example, the three doors match in size and position when swapped).
function Process-Group($relPaths, [int]$pocketMin) {
    $imgs = @{}
    $union = $null
    foreach ($p in $relPaths) {
        $src = Backup-Original $p
        $img = [ArtImage]::Load($src)
        $info = $img.RemoveWhite(235, $pocketMin)
        Write-Host ("{0}: {1}" -f $p, $info)
        $imgs[$p] = $img
        $r = $img.Bounds(8)
        if ($union -eq $null) { $union = $r } else { $union = [ArtImage]::Union($union, $r) }
    }
    foreach ($p in $relPaths) {
        $out = $imgs[$p].Crop($union, 3)
        $out.Save((Join-Path $art $p))
        Write-Host ("{0}: -> {1}x{2} (shared crop box)" -f $p, $out.W, $out.H)
    }
}

Write-Host '== Characters =='
# Hero eye whites are ~560 px (must stay), so the hero uses a high threshold.
# Goblins have no white eyes, but have background gaps (the hole in the
# cleaver is ~390 px), so they use a low threshold.
Process-Single 'Characters\hero.png' $true 1000
foreach ($f in 'goblin', 'goblin_2', 'goblin_3') { Process-Single "Characters\$f.png" $true 300 }

Write-Host '== Doors (shared crop box) =='
Process-Group @('Doors\door_closed.png', 'Doors\door_open.png', 'Doors\door_locked.png') 1000

Write-Host '== UI =='
# The padlock has light highlights on the shackle (~1300 px) that must stay;
# only the big hole inside the shackle (~67000 px) is background.
Process-Single 'UI\icon_lock.png' $true 20000
Process-Single 'UI\icon_check.png' $false 0
Process-Single 'UI\level_button_frame.png' $false 0

Write-Host '== Backgrounds and menu background are left as they are =='
Write-Host 'Done.'
