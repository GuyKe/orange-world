using UnityEngine;

namespace OrangeWorld
{
    // Marks a creature as the melee-brute blob variant, so systems that need to tell blob types apart (Blob
    // Bucks, XP, ...) can check for it the same way they check for BlobGun on a gunner.
    public class MeleeBlob : MonoBehaviour
    {
    }
}
