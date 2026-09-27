using UnityEngine;

namespace OrangeWorld
{
    // Clones `template` at `spawnPoint` each time it's punched - used for every blob and weapon summon button
    // in the sandbox. Any prefab works, since it's a plain Instantiate with no extra setup.
    public class SandboxSpawnButton : SandboxButtonBase
    {
        public GameObject template;
        public Transform spawnPoint;

        protected override void OnPressed(HandGrabber hand)
        {
            if (template == null || spawnPoint == null) return;
            Instantiate(template, spawnPoint.position, spawnPoint.rotation);
        }
    }
}
