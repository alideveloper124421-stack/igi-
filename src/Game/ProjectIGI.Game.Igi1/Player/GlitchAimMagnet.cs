using ProjectIGI.Core.Numerics;
using ProjectIGI.Engine.Tasks;
using ProjectIGI.Game.Ai;
using ProjectIGI.Game.Player;
using System;

namespace ProjectIGI.Game.Igi1.Player;

public static class GlitchAimMagnet
{
    public static void Tick(ref PlayerInputFrame frame, PlayerCamera cam)
    {
        var player = HumanPlayerTask.GlitchCurrent;
        if (player == null || !player.HasTree) return;

        Vector3d eye = player.OriginalViewPosition;
        float bestDist = 100f; // Target enemies within 100 units
        Vector3d bestTarget = default;
        bool found = false;

        // Task tree walk to find nearest enemy
        for (GameTask t = player.Tree.Root; t != null; t = Next(t))
        {
            if (t is HumanTask h && h != player && !h.IsDeleted && !h.IsOutOfWorld && h.Team != player.Team)
            {
                if (h is IAiHuman aiHuman && aiHuman.IsDead) continue;

                Vector3d target = h.OriginalViewPosition;
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
            // Calculate angles to look at enemy
            float dx = (float)(bestTarget.X - eye.X);
            float dy = (float)(bestTarget.Y - eye.Y);
            float dz = (float)(bestTarget.Z - eye.Z);

            // Yaw (Gamma) - Horizontal rotation
            float targetYaw = (float)Math.Atan2(dy, dx);
            
            // Pitch - Vertical rotation
            float distXY = (float)Math.Sqrt(dx * dx + dy * dy);
            float targetPitch = (float)Math.Atan2(dz, distXY);

            // 1. DIRECT YAW CONTROL: Turn player's body to face enemy
            player.Gamma = targetYaw;

            // 2. PITCH CONTROL: Force camera to look up/down at enemy
            // We calculate the difference and inject a massive value into LookY
            // to force the camera to move, regardless of sensitivity.
            float pitchDiff = targetPitch - cam.Pitch;
            
            // Inject into input frame (Amplified to ensure movement)
            // If this doesn't move the camera, we will try direct field manipulation next.
            frame.LookY += pitchDiff * 500f; 
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
