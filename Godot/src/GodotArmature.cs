using System;
using DragonBones;

namespace Test.code.dragonbones
{
    public class GodotArmature : GodotEventDispatcher, IArmatureProxy
    {
        public Armature armature { get; set; }

        public Animation animation => armature.animation;

        public void DBInit(Armature armature)
        {
            this.armature = armature;
        }

        public void DBClear()
        {
            armature = null;
        }

        public void DBUpdate()
        {
            foreach (var slot in armature.GetSlots())
            {
                var godotSlot = slot as GodotSlot;
                godotSlot?.UpdateTransformAndMatrix();
            }
        }

        public void Dispose(bool disposeProxy)
        {
            throw new NotImplementedException();
        }
    }
}