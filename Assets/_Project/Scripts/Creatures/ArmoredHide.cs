using UnityEngine;

namespace OrangeWorld
{
    // A shell that shrugs off bare-handed punches: swing a weapon, throw a rock, or fling another creature at it
    // and it takes damage like anything else (see ImpactDamager), but a hand alone does nothing.
    public class ArmoredHide : MonoBehaviour
    {
        public bool Blocks(GameObject source) => source.GetComponent<HandGrabber>() != null;
    }
}
