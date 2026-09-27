using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace OrangeWorld
{
    // Charges by killing blobs, shown as a bar locked to the corner of your view. Once full, click both
    // thumbsticks to unleash bonus health and damage for a while, with a black vignette while it's active.
    [RequireComponent(typeof(Damageable))]
    public class PlayerUltimate : MonoBehaviour
    {
        public Transform head;
        public float maxCharge = 100f;
        public float chargePerKill = 12f;
        public float duration = 30f;
        public float damageMultiplier = 2f;
        public float bonusHealth = 50f;
        public float transitionSeconds = 0.3f;

        public float meterWidth = 0.12f;
        public float meterHeight = 0.03f;
        public Vector3 meterOffset = new(0.12f, -0.09f, 0.16f);

        public float vignetteQuadSize = 0.18f;
        public float vignetteCornerOffset = 0.13f;
        public float vignetteDistance = 0.15f;

        [Tooltip("Alpha controls how much the charge bar shows through into your view.")]
        public Color backgroundColor = new(0.3f, 0.14f, 0.02f, 0.35f);
        public Color chargingColor = new(1f, 0.55f, 0.1f, 0.6f);
        public Color readyColor = new(1f, 0.8f, 0.15f, 0.8f);
        public Color labelColor = new(1f, 0.65f, 0.2f, 0.7f);

        public float Charge { get; private set; }
        public bool Active { get; private set; }
        bool Ready => !Active && Charge >= maxCharge;

        public void FillCharge() => Charge = maxCharge;

        Damageable health;
        InputAction leftClick, rightClick, keyboardActivate;
        Transform meterFill;
        SpriteRenderer meterFillRenderer;
        Transform[] vignetteQuads;
        float activeUntil;
        float vignetteAmount;

        void Awake()
        {
            health = GetComponent<Damageable>();

            leftClick = new InputAction("UltimateLeft", InputActionType.Button, "<XRController>{LeftHand}/primary2DAxisClick");
            rightClick = new InputAction("UltimateRight", InputActionType.Button, "<XRController>{RightHand}/primary2DAxisClick");
            keyboardActivate = new InputAction("UltimateKeyboard", InputActionType.Button, "<Keyboard>/r");
        }

        void OnEnable()
        {
            leftClick.Enable();
            rightClick.Enable();
            keyboardActivate.Enable();
            Damageable.AnyDied += OnAnyDied;
        }

        void OnDisable()
        {
            leftClick.Disable();
            rightClick.Disable();
            keyboardActivate.Disable();
            Damageable.AnyDied -= OnAnyDied;
        }

        void OnDestroy()
        {
            leftClick.Dispose();
            rightClick.Dispose();
            keyboardActivate.Dispose();
            if (Active) ImpactDamager.GlobalDamageMultiplier = 1f;
        }

        void Start()
        {
            BuildMeter();
            BuildVignette();
        }

        void OnAnyDied(Damageable dead)
        {
            // Only blobs (Blobling/GunnerBlobling/MeleeBlobling) carry a CreatureBrain, so this can't fire from the player itself.
            if (dead.GetComponent<CreatureBrain>() == null) return;
            Charge = Mathf.Min(maxCharge, Charge + chargePerKill);
        }

        void Update()
        {
            if (Active && Time.time >= activeUntil) Deactivate();

            bool wantsActivate = (leftClick.IsPressed() && rightClick.IsPressed()) || keyboardActivate.WasPressedThisFrame();
            if (Ready && wantsActivate) Activate();

            UpdateMeter();
            UpdateVignette(Time.deltaTime);
        }

        void Activate()
        {
            Active = true;
            activeUntil = Time.time + duration;
            Charge = 0f;
            vignetteAmount = 1f;

            health.AddMaxHealth(bonusHealth);
            ImpactDamager.GlobalDamageMultiplier = damageMultiplier;
            Haptics.Pulse(XRNode.LeftHand, 1f, 0.3f);
            Haptics.Pulse(XRNode.RightHand, 1f, 0.3f);
            Juice.Boing(head != null ? head.position : transform.position, 1f);
        }

        void Deactivate()
        {
            Active = false;
            vignetteAmount = 0f;
            health.AddMaxHealth(-bonusHealth);
            ImpactDamager.GlobalDamageMultiplier = 1f;
            Juice.Boing(head != null ? head.position : transform.position, 0.5f);
        }

        // ---------- Head-locked HUD ----------

        void BuildMeter()
        {
            if (head == null) return;

            HudSprite.Create("Ultimate Meter Background", head, meterOffset + Vector3.forward * 0.001f,
                new Vector2(meterWidth, meterHeight), backgroundColor);

            meterFillRenderer = HudSprite.Create("Ultimate Meter Fill", head, meterOffset, new Vector2(0f, meterHeight), chargingColor);
            meterFill = meterFillRenderer.transform;

            var label = WorldText.Create("Ultimate Meter Label", head, meterOffset + Vector3.up * 0.035f, new Vector2(meterWidth, 0.025f), 0.018f);
            label.text = "ULTIMATE";
            label.color = labelColor;
        }

        void UpdateMeter()
        {
            if (meterFill == null) return;

            float fraction = Mathf.Clamp01(Charge / maxCharge);
            meterFill.localScale = new Vector3(meterWidth * fraction, meterHeight, 1f);
            meterFill.localPosition = meterOffset + Vector3.left * (meterWidth * (1f - fraction) * 0.5f);
            meterFillRenderer.color = Ready ? readyColor : chargingColor;
        }

        void BuildVignette()
        {
            if (head == null) return;

            var root = new GameObject("Ultimate Vignette").transform;
            root.SetParent(head, false);
            root.localPosition = Vector3.forward * vignetteDistance;

            vignetteQuads = new Transform[4];
            Vector2[] corners = { new(-1f, 1f), new(1f, 1f), new(-1f, -1f), new(1f, -1f) };
            for (int i = 0; i < corners.Length; i++)
            {
                var sprite = HudSprite.Create("Corner " + i, root,
                    new Vector3(corners[i].x * vignetteCornerOffset, corners[i].y * vignetteCornerOffset, 0f),
                    Vector2.zero, Color.black);
                sprite.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                vignetteQuads[i] = sprite.transform;
            }
        }

        void UpdateVignette(float dt)
        {
            if (vignetteQuads == null) return;

            float current = vignetteQuads[0].localScale.x / vignetteQuadSize;
            float next = Mathf.MoveTowards(current, vignetteAmount, dt / Mathf.Max(0.01f, transitionSeconds));
            Vector3 scale = new(vignetteQuadSize * next, vignetteQuadSize * next, 1f);
            foreach (var quad in vignetteQuads) quad.localScale = scale;
        }
    }
}
