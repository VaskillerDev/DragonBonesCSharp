using System;
using DragonBones;
using Godot;
using Transform = DragonBones.Transform;

namespace Test.code.dragonbones
{
    /*
     * Slot: _InitDisplay => _OnUpdateDisplay => _AddDisplay => _UpdateVisible => _UpdateFrame => _UpdateBlendMode
     * => _UpdateColor => _UpdateTransform
     * 
     * GodotArmature: DBClear => DBUpdate
     */
    public class GodotSlot : Slot
    {
        private GodotTextureAtlasData _currentTextureAtlasData;
        private Sprite _slotNode;
        private const int PIXEL_SCALE = 100;
        private Node2D _boneNode;

        public Sprite GetSlotNode()
        {
            return _slotNode;
        }

        public void SetBoneNode(Node2D boneNode)
        {
            _boneNode = boneNode;
        }

        protected override void _OnClear()
        {
            base._OnClear();
            _currentTextureAtlasData = null;
        }

        public void SetCurrentTextureAtlasData(ref GodotTextureAtlasData textureAtlasData)
        {
            _currentTextureAtlasData = textureAtlasData;
        }

        protected override void _InitDisplay(object value, bool isRetain) // 1
        {
        }

        protected override void _OnUpdateDisplay() // 2
        {
            var bone = _slotData.parent;
            var position = new Vector2(bone.transform.x, bone.transform.y);
            
            _slotNode = _rawDisplay as Sprite;

            if (_slotNode == null) return;

            _textureData = _textureData ?? _currentTextureAtlasData.GetTexture(_slotNode.Name);

            AtlasTexture atlas = new AtlasTexture();
            atlas.Atlas = (_textureData as GodotTextureData)?.Texture;

            var region = new Rect2(_textureData.region.x, _textureData.region.y, _textureData.region.width,
                _textureData.region.height);
            atlas.Region = region;
            atlas.FilterClip = true;
            atlas.Flags = 0; // todo: hardcode

            _slotNode.Texture = atlas;//(_textureData as GodotTextureData)?.Texture;
            _slotNode.Position = position;
        }

        protected override void _AddDisplay() // 3
        {
            // _slotNode = _
        }

        internal override void _UpdateVisible() // 4
        {
        }

        protected override void _UpdateFrame() // 5
        {
        }

        internal override void _UpdateBlendMode() // 6
        {
        }

        protected override void _UpdateColor() // 7
        {
        }

        protected override void _UpdateTransform() // 8
        {
            if (_currentTextureAtlasData != null)
            {
                { // bone update
                    var bone = _armature.GetBone(_slotData.parent.name);
                    var animationPose = bone.animationPose;
                    var boneAnimPosX = animationPose.x * PIXEL_SCALE;
                    var boneAnimPosY = animationPose.y * PIXEL_SCALE;
                    var boneAnimPos = new Vector2(boneAnimPosX, boneAnimPosY);
                    var rotation = animationPose.rotation * Transform.RAD_DEG;
                    var scale = new Vector2(animationPose.scaleX, animationPose.scaleY);

                    var boneData = _slotData.parent;
                    var (bonePosX, bonePosY) = (boneData.transform.x * PIXEL_SCALE, boneData.transform.y * PIXEL_SCALE);
                    var bonePos = new Vector2(bonePosX, bonePosY);

                    _boneNode.Position = bonePos + boneAnimPos;
                    _boneNode.RotationDegrees = rotation;
                    _boneNode.Scale = scale;
                }

                {
                    // position
                    var defaultDisplayData = _armature.GetSlot(_slotData.name)._displayDatas[0]; // todo: hardcore
                    var transform = defaultDisplayData.transform;
                    var (slotPosX, slotPosY) = (transform.x * PIXEL_SCALE, transform.y * PIXEL_SCALE);
                    var position = new Vector2(slotPosX, slotPosY);
                    var rotation = transform.rotation * Transform.RAD_DEG;
                    
                    _slotNode.Position = position;
                    _slotNode.RotationDegrees = rotation;
                }
                
            }
        }

        protected override void _DisposeDisplay(object value, bool isRelease)
        {
            throw new NotImplementedException();
        }

        protected override void _ReplaceDisplay(object value)
        {
            throw new NotImplementedException();
        }

        protected override void _RemoveDisplay()
        {
            throw new NotImplementedException();
        }

        protected override void _UpdateZOrder()
        {
            throw new NotImplementedException();
        }

        protected override void _UpdateMesh()
        {
            throw new NotImplementedException();
        }

        protected override void _IdentityTransform()
        {
            _slotNode.Position = Vector2.Zero;
            _slotNode.Rotation = 0;
            _slotNode.Scale = Vector2.One;
        }
    }
}