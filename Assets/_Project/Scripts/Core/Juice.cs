using UnityEngine;

namespace OrangeWorld
{
    // Procedural placeholder audio so the prototype has feedback before real sound assets exist.
    public static class Juice
    {
        const int SampleRate = 44100;
        const float BoingLength = 0.35f;

        static AudioClip boing;
        static float lastBoingTime = -1f;

        public static void Boing(Vector3 position, float intensity)
        {
            if (Time.time - lastBoingTime < 0.03f) return;
            lastBoingTime = Time.time;
            if (boing == null) boing = CreateBoing();

            intensity = Mathf.Clamp01(intensity);
            var go = new GameObject("Boing");
            go.transform.position = position;
            var source = go.AddComponent<AudioSource>();
            source.clip = boing;
            source.spatialBlend = 1f;
            source.volume = 0.3f + intensity * 0.7f;
            source.pitch = Random.Range(0.8f, 1.3f) * Mathf.Lerp(1.3f, 0.7f, intensity);
            source.Play();
            Object.Destroy(go, boing.length / source.pitch + 0.1f);
        }

        static AudioClip CreateBoing()
        {
            int count = Mathf.RoundToInt(SampleRate * BoingLength);
            var data = new float[count];
            double phase = 0;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float frequency = Mathf.Lerp(520f, 110f, t / BoingLength) * (1f + 0.15f * Mathf.Sin(t * 2f * Mathf.PI * 18f));
                phase += 2.0 * Mathf.PI * frequency / SampleRate;
                data[i] = Mathf.Sin((float)phase) * Mathf.Exp(-t * 9f) * 0.8f;
            }
            var clip = AudioClip.Create("Boing", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
