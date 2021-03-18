using DragonBones;

namespace Test.code.dragonbones
{
    public class GodotTextureAtlasData : TextureAtlasData
    {
        public override TextureData CreateTexture()
        {
            return BorrowObject<GodotTextureData>();
        }
    }
}