using DragonBones;
using Godot;

namespace Test.code.dragonbones
{
    internal class GodotTextureData : TextureData
    {
        public Texture Texture;

        protected override void _OnClear()
        {
            base._OnClear();

            if (Texture == null) return;
            Texture.Free();
            Texture = null;
        }
    }
}