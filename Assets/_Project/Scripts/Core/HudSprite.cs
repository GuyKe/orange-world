using UnityEngine;

namespace OrangeWorld
{
    // Shared building block for simple head-locked HUD bars: a tinted, stretchable sprite. Sprites alpha-blend
    // and render double-sided correctly out of the box on any render pipeline, unlike a mesh + material, so
    // this is how every faded HUD element in the game (health, ultimate charge, blob coins) is built.
    public static class HudSprite
    {
        static Sprite solidSprite;

        public static SpriteRenderer Create(string name, Transform parent, Vector3 localPosition, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Solid();
            renderer.color = color;
            return renderer;
        }

        static Sprite Solid()
        {
            if (solidSprite != null) return solidSprite;
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            solidSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return solidSprite;
        }
    }
}
