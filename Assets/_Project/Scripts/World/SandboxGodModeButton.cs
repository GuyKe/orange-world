using UnityEngine;
using UnityEngine.UI;

namespace OrangeWorld
{
    // Toggles the player's Damageable.invulnerable flag - sandbox-only, and never persists: the player (and this
    // button) are torn down the moment you leave for the main menu, so god mode can't leak back into a real run.
    public class SandboxGodModeButton : SandboxButtonBase
    {
        public Damageable playerHealth;
        public Text label;

        protected override void OnPressed(HandGrabber hand)
        {
            if (playerHealth == null) return;
            playerHealth.invulnerable = !playerHealth.invulnerable;
            if (label != null) label.text = playerHealth.invulnerable ? "GOD MODE\nON" : "GOD MODE\nOFF";
        }
    }
}
