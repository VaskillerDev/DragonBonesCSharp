﻿using System;

namespace DragonBones
{
    /**
     * @private
     * @internal
     */
    internal abstract class Constraint : BaseObject
    {
        protected static readonly Matrix _helpMatrix = new Matrix();
        protected static readonly Transform _helpTransform = new Transform();
        protected static readonly Point _helpPoint = new Point();

        internal ConstraintData _constraintData;
        protected Armature _armature;

        internal Bone _target;
        internal Bone _bone;
        internal Bone _root;

        protected override void _OnClear()
        {
            this._armature = null;
            this._target = null; //
            this._bone = null; //
            this._root = null; //
        }

        public abstract void Init(ConstraintData constraintData, Armature armature);
        public abstract void Update();
        public abstract void InvalidUpdate();

        public string name
        {
            get { return this._constraintData.name; }
        }
    }
    /**
     * @private
     * @internal
     */
    internal class IKConstraint : Constraint
    {
        internal bool _scaleEnabled; // TODO
        internal bool _bendPositive;
        internal float _weight;

        protected override void _OnClear()
        {
            base._OnClear();

            this._scaleEnabled = false;
            this._bendPositive = false;
            this._weight = 1.0f;
            this._constraintData = null;
        }

        private void _ComputeA()
        {
            var ikGlobal = this._target.global;
            var global = this._bone.global;
            var globalTransformMatrix = this._bone.globalTransformMatrix;
            // const boneLength = this.bone.boneData.length;
            // const x = globalTransformMatrix.a * boneLength; 

            var radian = (float)Math.Atan2(ikGlobal.y - global.y, ikGlobal.x - global.x);
            if (global.scaleX < 0.0f)
            {
                radian += (float)Math.PI;
            }

            global.rotation += (radian - global.rotation) * this._weight;
            global.ToMatrix(globalTransformMatrix);
        }

        private void _ComputeB()
        {
            var boneLength = this._bone.boneData.length;
            var parent = this._root as Bone;
            var ikGlobal = this._target.global;
            var parentGlobal = parent.global;
            var global = this._bone.global;
            var globalTransformMatrix = this._bone.globalTransformMatrix;

            var x = globalTransformMatrix.a * boneLength;
            var y = globalTransformMatrix.b * boneLength;

            var lLL = x * x + y * y;
            var lL = (float)Math.Sqrt(lLL);

            var dX = global.x - parentGlobal.x;
            var dY = global.y - parentGlobal.y;
            var lPP = dX * dX + dY * dY;
            var lP = (float)Math.Sqrt(lPP);
            var rawRadian = global.rotation;
            var rawParentRadian = parentGlobal.rotation;
            var rawRadianA = (float)Math.Atan2(dY, dX);

            dX = ikGlobal.x - parentGlobal.x;
            dY = ikGlobal.y - parentGlobal.y;
            var lTT = dX * dX + dY * dY;
            var lT = (float)Math.Sqrt(lTT);

            var radianA = 0.0f;
            if (lL + lP <= lT || lT + lL <= lP || lT + lP <= lL)
            {
                radianA = (float)Math.Atan2(ikGlobal.y - parentGlobal.y, ikGlobal.x - parentGlobal.x);
                if (lL + lP <= lT)
                {
                }
                else if (lP < lL)
                {
                    radianA += (float)Math.PI;
                }
            }
            else
            {
                var h = (lPP - lLL + lTT) / (2.0f * lTT);
                var r = (float)Math.Sqrt(lPP - h * h * lTT) / lT;
                var hX = parentGlobal.x + (dX * h);
                var hY = parentGlobal.y + (dY * h);
                var rX = -dY * r;
                var rY = dX * r;

                var isPPR = false;
                if (parent._parent != null)
                {
                    var parentParentMatrix = parent._parent.globalTransformMatrix;
                    isPPR = parentParentMatrix.a * parentParentMatrix.d - parentParentMatrix.b * parentParentMatrix.c < 0.0f;
                }

                if (isPPR != this._bendPositive)
                {
                    global.x = hX - rX;
                    global.y = hY - rY;
                }
                else
                {
                    global.x = hX + rX;
                    global.y = hY + rY;
                }

                radianA = (float)Math.Atan2(global.y - parentGlobal.y, global.x - parentGlobal.x);
            }

            var dR = Transform.NormalizeRadian(radianA - rawRadianA);
            parentGlobal.rotation = rawParentRadian + dR * this._weight;
            parentGlobal.ToMatrix(parent.globalTransformMatrix);
            //
            var currentRadianA = rawRadianA + dR * this._weight;
            global.x = parentGlobal.x + (float)Math.Cos(currentRadianA) * lP;
            global.y = parentGlobal.y + (float)Math.Sin(currentRadianA) * lP;
            //
            var radianB = (float)Math.Atan2(ikGlobal.y - global.y, ikGlobal.x - global.x);
            if (global.scaleX < 0.0f)
            {
                radianB += (float)Math.PI;
            }

            global.rotation = parentGlobal.rotation + rawRadian - rawParentRadian + Transform.NormalizeRadian(radianB - dR - rawRadian) * this._weight;
            global.ToMatrix(globalTransformMatrix);
        }

        public override void Init(ConstraintData constraintData, Armature armature)
        {
            if (this._constraintData != null)
            {
                return;
            }

            this._constraintData = constraintData;
            this._armature = armature;
            this._target = this._armature.GetBone(this._constraintData.target.name);
            this._bone = this._armature.GetBone(this._constraintData.bone.name);
            this._root = this._constraintData.root != null ? this._armature.GetBone(this._constraintData.root.name) : null;

            {
                var ikConstraintData = this._constraintData as IKConstraintData;
                //
                this._scaleEnabled = ikConstraintData.scaleEnabled;
                this._bendPositive = ikConstraintData.bendPositive;
                this._weight = ikConstraintData.weight;
            }

            this._bone._hasConstraint = true;
        }

        public override void Update()
        {
            if (this._root == null)
            {
                this._bone.UpdateByConstraint();
                this._ComputeA();
            }
            else
            {
                this._root.UpdateByConstraint();
                this._bone.UpdateByConstraint();
                this._ComputeB();
            }
        }

        public override void InvalidUpdate()
        {
            if (this._root == null)
            {
                this._bone.InvalidUpdate();
            }
            else
            {
                this._root.InvalidUpdate();
                this._bone.InvalidUpdate();
            }
        }
    }
}