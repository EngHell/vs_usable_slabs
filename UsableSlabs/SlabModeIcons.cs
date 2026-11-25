using System;
using Cairo;
using Vintagestory.API.Client;

namespace UsableSlabs;

/// <summary>
/// Cairo icons for the F menu. Horizontal-side icons are a top-down view of the block; Up/Down and the
/// vanilla Horizontal/Vertical icons are a side view standing on a ground line.
/// </summary>
public static class SlabModeIcons
{
    /// <param name="quarterTurns">
    /// Compass mode only: which way the player faces (0 north, 1 east, 2 south, 3 west). The top-down icons
    /// are turned so that "up" in the icon is the player's view direction.
    /// </param>
    public static DrawSkillIconDelegate For(int mode, bool relative, int quarterTurns = 0) => (ctx, x, y, w, h, rgba) =>
    {
        double cx = x + w / 2, cy = y + h / 2;
        int q = relative ? 0 : quarterTurns;
        // Leave room around the block square for the ground line / markers.
        double s = Math.Min(w, h) * 0.62;
        double bx = x + (w - s) / 2, by = y + (h - s) / 2;
        ctx.LineWidth = 2;
        ctx.LineJoin = LineJoin.Miter;

        switch (mode)
        {
            case SlabModes.Auto:
                Outline(ctx, bx, by, s, rgba);
                Ground(ctx, bx, by, s, rgba);
                Dashed(ctx, bx, by + s / 2, bx + s, by + s / 2, rgba);
                Dashed(ctx, bx + s / 2, by, bx + s / 2, by + s, rgba);
                break;
            case SlabModes.Horizontal:
                Outline(ctx, bx, by, s, rgba);
                Ground(ctx, bx, by, s, rgba);
                Line(ctx, bx, by + s / 2, bx + s, by + s / 2, rgba);
                break;
            case SlabModes.Vertical:
                Outline(ctx, bx, by, s, rgba);
                Ground(ctx, bx, by, s, rgba);
                Line(ctx, bx + s / 2, by, bx + s / 2, by + s, rgba);
                break;
            case >= SlabModes.FirstSide and < SlabModes.ToggleRelative when SlabModes.SideAtMenu(mode) == SlabModes.Side.Down:
                Half(ctx, bx, by + s / 2, s, s / 2, rgba);
                Outline(ctx, bx, by, s, rgba);
                Ground(ctx, bx, by, s, rgba);
                break;
            case >= SlabModes.FirstSide and < SlabModes.ToggleRelative when SlabModes.SideAtMenu(mode) == SlabModes.Side.Up:
                Half(ctx, bx, by, s, s / 2, rgba);
                Outline(ctx, bx, by, s, rgba);
                Ground(ctx, bx, by, s, rgba);
                break;
            case >= SlabModes.FirstSide and < SlabModes.ToggleRelative:
            {
                // k: 0 north/far, 1 east/right, 2 south/near, 3 west/left. Drawn on screen at k - q quarter turns from the top.
                int k = SlabModes.SideAtMenu(mode) - SlabModes.Side.H0;
                Turned(ctx, cx, cy, k - q, () => Half(ctx, bx, by, s, s / 2, rgba));
                Outline(ctx, bx, by, s, rgba);
                double margin = by - y;
                if (relative) Player(ctx, cx, by + s + margin / 2, margin * 0.8, rgba);
                else Turned(ctx, cx, cy, -q, () => North(ctx, cx, y + margin / 2, margin * 0.8, rgba));
                break;
            }
            case SlabModes.ToggleRelative:
            {
                // Shows the current setting: a player marker (relative) or a compass needle (compass).
                double r = Math.Min(w, h) / 2 - 2;
                ctx.SetSourceRGBA(rgba[0], rgba[1], rgba[2], rgba[3]);
                ctx.Arc(cx, cy, r, 0, 2 * Math.PI);
                ctx.Stroke();
                if (relative) Player(ctx, cx, cy, r, rgba);
                else Turned(ctx, cx, cy, -q, () => North(ctx, cx, cy, r * 1.1, rgba));
                break;
            }
        }
    };

    /// <summary>Runs <paramref name="draw"/> rotated clockwise by <paramref name="quarters"/> × 90° around (cx, cy).</summary>
    private static void Turned(Context ctx, double cx, double cy, int quarters, Action draw)
    {
        ctx.Save();
        ctx.Translate(cx, cy);
        ctx.Rotate(((quarters % 4) + 4) % 4 * Math.PI / 2);
        ctx.Translate(-cx, -cy);
        draw();
        ctx.Restore();
    }

    private static void Outline(Context ctx, double x, double y, double s, double[] c)
    {
        ctx.SetSourceRGBA(c[0], c[1], c[2], c[3]);
        ctx.Rectangle(x, y, s, s);
        ctx.Stroke();
    }

    private static void Half(Context ctx, double x, double y, double w, double h, double[] c)
    {
        ctx.SetSourceRGBA(c[0], c[1], c[2], c[3] * 0.85);
        ctx.Rectangle(x, y, w, h);
        ctx.Fill();
    }

    private static void Ground(Context ctx, double x, double y, double s, double[] c)
    {
        double gy = y + s + 4;
        ctx.SetSourceRGBA(c[0], c[1], c[2], c[3] * 0.6);
        Line(ctx, x - 5, gy, x + s + 5, gy, null);
    }

    private static void Line(Context ctx, double x1, double y1, double x2, double y2, double[]? c)
    {
        if (c != null) ctx.SetSourceRGBA(c[0], c[1], c[2], c[3]);
        ctx.MoveTo(x1, y1);
        ctx.LineTo(x2, y2);
        ctx.Stroke();
    }

    private static void Dashed(Context ctx, double x1, double y1, double x2, double y2, double[] c)
    {
        ctx.SetDash(new double[] { 3, 3 }, 0);
        Line(ctx, x1, y1, x2, y2, c);
        ctx.SetDash(Array.Empty<double>(), 0);
    }

    /// <summary>Upward triangle: "you, looking this way".</summary>
    private static void Player(Context ctx, double cx, double cy, double size, double[] c)
    {
        ctx.SetSourceRGBA(c[0], c[1], c[2], c[3]);
        ctx.MoveTo(cx, cy - size / 2);
        ctx.LineTo(cx + size / 2, cy + size / 2);
        ctx.LineTo(cx - size / 2, cy + size / 2);
        ctx.ClosePath();
        ctx.Fill();
    }

    /// <summary>Compass needle with the north half filled.</summary>
    private static void North(Context ctx, double cx, double cy, double size, double[] c)
    {
        double hw = size * 0.3, hh = size / 2;
        ctx.SetSourceRGBA(c[0], c[1], c[2], c[3]);
        ctx.MoveTo(cx, cy - hh);
        ctx.LineTo(cx + hw, cy);
        ctx.LineTo(cx - hw, cy);
        ctx.ClosePath();
        ctx.Fill();
        ctx.LineWidth = 1.5;
        ctx.MoveTo(cx + hw, cy);
        ctx.LineTo(cx, cy + hh);
        ctx.LineTo(cx - hw, cy);
        ctx.Stroke();
        ctx.LineWidth = 2;
    }
}
