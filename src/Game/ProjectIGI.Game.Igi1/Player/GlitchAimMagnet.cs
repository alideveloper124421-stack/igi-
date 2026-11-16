using ProjectIGI.Core.Numerics;
using ProjectIGI.Engine.Tasks;
using ProjectIGI.Game.Ai;
using ProjectIGI.Game.Player;
using System;

namespace ProjectIGI.Game.Igi1.Player;

public static class GlitchAimMagnet
{
    public static bool Enabled = true;
    public static float MagnetStrength = 500f; // Massive force to override sensitivity

    public static void Tick(ref PlayerInputFrame frame, PlayerCamera cam)
    {
        if (!Enabled) return;
        var player = HumanPlayerTask.GlitchCurrent;
        if (player == null || !player.HasTree) return;

        Vector3d eye = player.OriginalViewPosition;
        float bestDist = 50f; // Magnet range (units)
        Vector3d bestTarget = default;
        bool found = false;

        // Task tree walk to find nearest enemy
        for (GameTask t = player.Tree.Root; t != null; t = Next(t))
        {
            if (t is HumanTask h && h != player && !h.IsDeleted && !h.IsOutOfWorld && h.Team != player.Team)
            {
                if (h is IAiHuman aiHuman && aiHuman.IsDead) continue;

                Vector3d target = h.OriginalViewPosition; // Head/Eye position
                float dist = (float)Math.Sqrt(
                    Math.Pow(target.X - eye.X, 2) +
                    Math.Pow(target.Y - eye.Y, 2) +
                    Math.Pow(target.Z - eye.Z, 2));

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestTarget = target;
                    found = true;
                }
            }
        }

        if (found)
        {
            // Calculate angles to enemy head
            float dx = (float)(bestTarget.X - eye.X);
            float dy = (float)(bestTarget.Y - eye.Y);
            float dz = (float)(bestTarget.Z - eye.Z);

            float targetYaw = (float)Math.Atan2(dy, dx);
            float distXY = (float)Math.Sqrt(dx * dx + dy * dy);
            float targetPitch = (float)Math.Atan2(dz, distXY);

            // Current angles
            float currentYaw = cam.FrameYaw;
            float currentPitch = cam.FramePitch;

            // Calculate delta
            float deltaYaw = targetYaw - currentYaw;
            float deltaPitch = targetPitch - currentPitch;

            // Normalize Yaw delta to -PI to PI
            while (deltaYaw > MathF.PI) deltaYaw -= 2 * MathF.PI;
            while (deltaYaw < -MathF.PI) deltaYaw += 2 * MathF.PI;

            // INJECT MASSIVE FORCE (Snap to head)
            frame.LookX += deltaYaw * MagnetStrength;
            frame.LookY += deltaPitch * MagnetStrength;
        }
    }

    private static GameTask Next(GameTask t) => t.FirstChild ?? t.NextSibling ?? Up(t);
    private static GameTask Up(GameTask t)
    {
        var p = t.Parent;
        while (p != null && p.NextSibling == null) p = p.Parent;
        return p?.NextSibling;
    }
}
