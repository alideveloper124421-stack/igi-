using ProjectIGI.Game.Player;
using System;

namespace ProjectIGI.Game.Igi1.Player;

public static class GlitchAimMagnet
{
    public static void Tick(ref PlayerInputFrame frame, PlayerCamera cam)
    {
        // JAAN BOOJH KAR CRASH: Agar yeh line execute hui, game foran crash karegi.
        throw new Exception("GLITCH MOD SUCCESSFULLY LOADED IN ENGINE!");
    }
}
