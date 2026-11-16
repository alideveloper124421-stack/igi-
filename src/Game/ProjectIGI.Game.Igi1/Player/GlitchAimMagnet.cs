using ProjectIGI.Core.Numerics;
using ProjectIGI.Engine.Tasks;
using ProjectIGI.Game.Ai;
using ProjectIGI.Game.Player;

namespace ProjectIGI.Game.Igi1.Player;

public static class GlitchAimMagnet
{
    public static bool Enabled = true;
    public static float MagnetFovDeg = 18f;
    public static float SnapDeg = 5f;
    public static float SnapSpeed = 0.85f;
    public static float TrackSpeed = 0.18f;
    public static bool AutoFire = false;

    private const float Rad = MathF.PI / 180f;
    private static int _cal;
    private static float _ly, _lp, _ky, _kp;
    private const float Pulse = 0.02f;

    public static void Tick(ref PlayerInputFrame frame, PlayerCamera cam)
    {
        if (!Enabled) return;
        var player = HumanPlayerTask.GlitchCurrent;
        if (player is null || !player.HasTree) return;

        if (_cal == 0) { _ly = cam.FrameYaw; frame.LookX += Pulse; _cal = 1; return; }
        if (_cal == 1) { _ky = (cam.FrameYaw - _ly) / Pulse; _lp = cam.FramePitch; frame.LookY += Pulse; _cal = 2; return; }
        if (_cal == 2) { _kp = (cam.FramePitch - _lp) / Pulse; _cal = 3; }
        if (MathF.Abs(_ky) < 1e-6f || MathF.Abs(_kp) < 1e-6f) { _cal = 0; return; }

        var aim = cam.FrameAim;
        if (aim is null) return;
        Vector3f fv = aim.Value.Item2;
        float fl = Len(fv.X, fv.Y, fv.Z);
        if (fl < 1e-6f) return;
        float fx = fv.X / fl, fy = fv.Y / fl, fz = fv.Z / fl;

        Vector3d eye = player.OriginalViewPosition;
        float best = MagnetFovDeg * Rad;
        Vector3d target = default;
        bool found = false;

        for (GameTask? t = player.Tree.Root; t is not null; t = Next(t))
        {
            if (t is HumanTask h && !ReferenceEquals(h, player)
                && !h.IsDeleted && !h.IsOutOfWorld
                && h.Team != player.Team && !((IAiHuman)h).IsDead)
            {
                Vector3d p = h.OriginalViewPosition;
                float dx = (float)(p.X - eye.X), dy = (float)(p.Y - eye.Y), dz = (float)(p.Z - eye.Z);
                float dl = Len(dx, dy, dz);
                if (dl < 1f) continue;
                float dot = Math.Clamp((fx * dx + fy * dy + fz * dz) / dl, -1f, 1f);
                float ang = MathF.Acos(dot);
                if (ang < best) { best = ang; target = p; found = true; }
            }
        }
        if (!found) return;

        float wx = (float)(target.X - eye.X), wy = (float)(target.Y - eye.Y), wz = (float)(target.Z - eye.Z);
        float wl = Len(wx, wy, wz);
        wx /= wl; wy /= wl; wz /= wl;

        float yawErr = MathF.Atan2(fx * wy - fy * wx, fx * wx + fy * wy);
        float pitchErr = MathF.Asin(Math.Clamp(wz, -1f, 1f)) - MathF.Asin(Math.Clamp(fz, -1f, 1f));
        float speed = best > SnapDeg * Rad ? SnapSpeed : TrackSpeed;

        frame.LookX += (yawErr / _ky) * speed;
        frame.LookY += (pitchErr / _kp) * speed;

        if (AutoFire && best < 2.5f * Rad)
            frame[(int)PlayerInputSlot.Fire] = 256f;
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
