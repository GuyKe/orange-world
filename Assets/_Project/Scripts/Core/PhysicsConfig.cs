using UnityEngine;

namespace OrangeWorld
{
    public static class PhysicsConfig
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Configure()
        {
            // Match Quest's 72 Hz display so physics hands don't stutter between frames.
            Time.fixedDeltaTime = 1f / 72f;
            Physics.defaultSolverIterations = 10;
            Physics.defaultSolverVelocityIterations = 4;
            Physics.defaultMaxAngularSpeed = 50f;
        }
    }
}
