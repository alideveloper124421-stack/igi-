using ProjectIGI.Game.Player;

namespace ProjectIGI.Game.Igi1.Player;

public static class GlitchAimMagnet
{
    public static void Tick(ref PlayerInputFrame frame, PlayerCamera cam)
    {
        // DEBUG MODE: Har frame mein camera ko zabardasti right aur upar ghuma do.
        // Agar game start hote hi camera khud ghoomne lage, toh mod 100% WORKING hai!
        frame.LookX += 50.0f; 
        frame.LookY += 50.0f;
    }
}
