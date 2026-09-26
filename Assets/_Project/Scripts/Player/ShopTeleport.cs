using UnityEngine;
using UnityEngine.InputSystem;

namespace OrangeWorld
{
    // Click B to warp to the upgrades shop and back, so you don't have to walk there every time you want to
    // spend blob bucks. Toggles: press once to go, press again from the shop to return where you were.
    public class ShopTeleport : MonoBehaviour
    {
        public PlanetWalker walker;
        public Vector3 shopPosition;
        public Quaternion shopRotation = Quaternion.identity;

        public bool AtShop { get; private set; }

        InputAction teleportAction;
        Vector3 returnPosition;
        Quaternion returnRotation;

        void Awake()
        {
            teleportAction = new InputAction("ShopTeleport", InputActionType.Button, "<XRController>{RightHand}/secondaryButton");
            teleportAction.AddBinding("<Keyboard>/b");
        }

        void OnEnable() => teleportAction.Enable();
        void OnDisable() => teleportAction.Disable();
        void OnDestroy() => teleportAction.Dispose();

        void Update()
        {
            if (!teleportAction.WasPressedThisFrame()) return;

            if (AtShop)
            {
                walker.Teleport(returnPosition, returnRotation);
            }
            else
            {
                returnPosition = walker.Body.position;
                returnRotation = walker.Body.rotation;
                walker.Teleport(shopPosition, shopRotation);
            }
            AtShop = !AtShop;
        }
    }
}
