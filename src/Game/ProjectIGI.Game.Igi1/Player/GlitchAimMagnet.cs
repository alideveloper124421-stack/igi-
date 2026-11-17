using ProjectIGI.Core.Numerics;
using ProjectIGI.Engine.Tasks;
using ProjectIGI.Game.Ai;
using ProjectIGI.Game.Player;
using System;

namespace ProjectIGI.Game.Igi1.Player;

public static class GlitchAimMagnet
{
    public static bool Enabled = false;
    public static float ActivateFovDeg = 15f;
    public static float LockBreakDeg = 35f;
    public static float SnapFactor = 0.55f;

    private const float Rad = MathF.PI / 180f;
    private static float _pulse = 100f;
    private static int _st;
    private static float _y0, _p0, _sy, _sp;
    private static GameTask? _locked;

    public static void Tick(ref PlayerInputFrame frame, PlayerCamera cam)
    {
        if (!Enabled) return;
        var player = HumanPlayerTask.GlitchCurrent;
        if (player is null || !player.HasTree) return;

        switch (_st)
        {
            case 0: _y0 = cam.FrameYaw; frame.LookX += _pulse; _st = 1; return;
            case 1:
            {
                float d = cam.FrameYaw - _y0;
                if (MathF.Abs(d) > 1e-5f) { _sy = d / _pulse; _st = 2; }
                else { if (_pulse < 20000f) _pulse *= 5f; _st = 0; }
                return;
            }
            case 2: _p0 = cam.FramePitch; frame.LookY += _pulse; _st = 3; return;
            case 3:
            {
                float d = cam.FramePitch - _p0;
                if (MathF.Abs(d) > 1e-5f) { _sp = d / _pulse; _st = 4; }
                else { if (_pulse < 20000f) _pulse *= 5f; _st = 2; }
                return;
            }
        }
        if (MathF.Abs(_sy) < 1e-9f || MathF.Abs(_sp) < 1e-9f) { _st = 0; return; }

        Vector3d eye = player.OriginalViewPosition;
        var aim = cam.FrameAim;
        if (aim is null) return;
        Vector3f fv = aim.Value.Item2;
        float fl = Len(fv.X, fv.Y, fv.Z);
        if (fl < 1e-6f) return;
        float fx = fv.X / fl, fy = fv.Y / fl, fz = fv.Z / fl;

        HumanTask? target = null;
        if (_locked is HumanTask lh && !lh.IsDeleted && !lh.IsOutOfWorld && !((IAiHuman)lh).IsDead)
        {
            if (Angle(fx, fy, fz, lh.OriginalViewPosition, eye) < LockBreakDeg * Rad) target = lh;
            else _locked = null;
        }

        if (target is null)
        {
            float best = ActivateFovDeg * Rad;
            for (GameTask? t = player.Tree.Root; t is not null; t = Next(t))
            {
                if (t is HumanTask h && !ReferenceEquals(h, player) && !h.IsDeleted && !h.IsOutOfWorld
                    && h.Team != player.Team && !((IAiHuman)h).IsDead)
                {
                    float a = Angle(fx, fy, fz, h.OriginalViewPosition, eye);
                    if (a < best) { best = a; target = h; }
                }
            }
            _locked = target;
        }
        if (target is null) return;

        Vector3d tp = target.OriginalViewPosition;
        float dx = (float)(tp.X - eye.X), dy = (float)(tp.Y - eye.Y), dz = (float)(tp.Z - eye.Z);
        float dl = Len(dx, dy, dz);
        if (dl < 1f) return;
        dx /= dl; dy /= dl; dz /= dl;

        float deltaYaw = MathF.Atan2(fx * dy - fy * dx, fx * dx + fy * dy);
        float deltaPitch = MathF.Asin(Math.Clamp(dz, -1f, 1f)) - MathF.Asin(Math.Clamp(fz, -1f, 1f));

        frame.LookX += (deltaYaw / _sy) * SnapFactor;
        frame.LookY += (deltaPitch / _sp) * SnapFactor;
    }

    private static float Angle(float fx, float fy, float fz, Vector3d p, Vector3d eye)
    {
        float dx = (float)(p.X - eye.X), dy = (float)(p.Y - eye.Y), dz = (float)(p.Z - eye.Z);
        float dl = Len(dx, dy, dz);
        if (dl < 1f) return float.MaxValue;
        float dot = Math.Clamp((fx * dx + fy * dy + fz * dz) / dl, -1f, 1f);
        return MathF.Acos(dot);
    }

    private static float Len(float x, float y, float z) => MathF.Sqrt(x * x + y * y + z * z);
    private static GameTask? Next(GameTask t) => t.FirstChild ?? t.NextSibling ?? Up(t);
    private static GameTask? Up(GameTask t)
    {
        var p = t.Parent;
        while (p is not null && p.NextSibling is null) p = p.Parent;
        return p?.NextSibling;
    }
}
