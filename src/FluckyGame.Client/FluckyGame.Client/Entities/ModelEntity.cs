using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FluckyGame.Client.Pipeline.Animation;

namespace FluckyGame.Client.Entities
{
    public class ModelEntity : Entity
    {
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scalation;

        Model model;
        public Model Model { get { return model; } }

        AnimationClip animationClip;
        public AnimationClip AnimationClip
        {
            get { return animationClip; }
            set
            {
                if (value == null)
                {
                    if (animationClip == null)
                        throw new Exception("No clip playing!");

                    animationClip = value;
                    boneInfos = null;
                }
                else
                {
                    if (animationClip != null)
                        throw new Exception("Another clip playing!");

                    animationClip = value;
                    animationPlaying = false;
                    animationLooping = false;
                    animationDuration = (float)animationClip.Duration;
                    animationSpeed = 1;
                    boneInfos = new BoneInfo[animationClip.Bones.Count];
                    for (int i = 0; i < boneInfos.Length; i++)
                        boneInfos[i] = new BoneInfo(model, animationClip.Bones[i]);
                    animationPosition = 0;
                }
            }
        }

        bool animationPlaying;
        public bool AnimationPlaying
        {
            get { if (animationClip == null) throw new Exception("No clip playing!"); else return animationPlaying; }
            set { if (animationClip == null) throw new Exception("No clip playing!"); else animationPlaying = value; }
        }

        bool animationLooping;
        public bool AnimationLooping
        {
            get { if (animationClip == null) throw new Exception("No clip playing!"); else return animationLooping; }
            set { if (animationClip == null) throw new Exception("No clip playing!"); else animationLooping = value; }
        }

        float animationDuration;
        public float AnimationDuration
        {
            get { if (animationClip == null) throw new Exception("No clip playing!"); else return animationDuration; }
        }

        float animationSpeed;
        public float AnimationSpeed
        {
            get { if (animationClip == null) throw new Exception("No clip playing!"); else return animationSpeed; }
            set { if (animationClip == null) throw new Exception("No clip playing!"); else animationSpeed = value; }
        }

        float animationPosition;
        public float AnimationPosition
        {
            get { if (animationClip == null) throw new Exception("No clip playing!"); else return animationPosition; }
            set
            {
                if (animationClip == null)
                    throw new Exception("No clip playing!");
                else
                {
                    animationPosition = value;

                    while (animationPosition < 0)
                        animationPosition += animationDuration;

                    while (animationPosition > animationDuration)
                        animationPosition -= animationDuration;

                    foreach (var bone in boneInfos)
                        bone.SetPosition(animationPosition);
                }
            }
        }

        BoneInfo[] boneInfos;

        Matrix world;

        public ModelEntity(Model model)
        {
            this.model = model;

            Position = Vector3.Zero;
            Rotation = Vector3.Zero;
            Scalation = new Vector3(1, 1, 1);

            UpdateWorld();
        }

        public ModelEntity(Model model, Vector3 position)
        {
            this.model = model;

            this.Position = position;
            Rotation = Vector3.Zero;
            Scalation = new Vector3(1, 1, 1);

            UpdateWorld();
        }

        public ModelEntity(Model model, Vector3 position, Vector3 rotation)
        {
            this.model = model;

            this.Position = position;
            this.Rotation = rotation;
            Scalation = new Vector3(1, 1, 1);

            UpdateWorld();
        }

        public ModelEntity(Model model, Vector3 position, Vector3 rotation, Vector3 scalation)
        {
            this.model = model;

            this.Position = position;
            this.Rotation = rotation;
            this.Scalation = scalation;

            UpdateWorld();
        }

        public void UpdateWorld()
        {
            world = Matrix.CreateScale(Scalation) * Matrix.CreateFromYawPitchRoll(Rotation.X, Rotation.Y, Rotation.Z) * Matrix.CreateTranslation(Position);
        }

        public override void Draw(GameTime gameTime)
        {
            if (animationClip == null)
            {
                foreach (var mesh in model.Meshes)
                {
                    foreach (var effect in mesh.Effects)
                        if (effect is IEffectMatrices)
                            (effect as IEffectMatrices).World = world;

                    mesh.Draw();
                }
            }
            else
            {
                if (animationPlaying)
                {
                    AnimationPosition += (float)gameTime.ElapsedGameTime.TotalSeconds * animationSpeed;
                    if (AnimationLooping && AnimationPosition >= AnimationDuration)
                        AnimationPosition -= AnimationDuration;
                }

                var modelExtra = Game1.currentInstance.modelExtras[model];
                var bones = Game1.currentInstance.modelBones[model];

                var boneTransforms = new Matrix[bones.Count];
                for (int i = 0; i < bones.Count; i++)
                {
                    var bone = bones[i];
                    bone.ComputeAbsoluteTransform();
                    boneTransforms[i] = bone.AbsoluteTransform;
                }

                var skeleton = new Matrix[modelExtra.Skeleton.Count];
                for (int s = 0; s < modelExtra.Skeleton.Count; s++)
                {
                    var bone = bones[modelExtra.Skeleton[s]];
                    skeleton[s] = bone.SkinTransform * bone.AbsoluteTransform;
                }

                foreach (var modelMesh in model.Meshes)
                {
                    foreach (var effect in modelMesh.Effects)
                    {
                        if (effect is IEffectMatrices)
                            (effect as IEffectMatrices).World = boneTransforms[modelMesh.ParentBone.Index] * world;

                        if (effect is SkinnedEffect)
                            (effect as SkinnedEffect).SetBoneTransforms(skeleton);

                        modelMesh.Draw();
                    }
                }
            }
        }

        private class BoneInfo
        {
            private int currentKeyframe = 0;
            private Bone assignedBone = null;
            public bool valid = false;
            private Quaternion rotation;
            public Vector3 translation;
            public AnimationClip.Bone.Keyframe Keyframe1;
            public AnimationClip.Bone.Keyframe Keyframe2;

            public AnimationClip.Bone ClipBone { get; set; }
            public Bone ModelBone { get { return assignedBone; } }

            public BoneInfo(Model model, AnimationClip.Bone bone)
            {
                this.ClipBone = bone;
                SetKeyframes();
                SetPosition(0);
                assignedBone = Game1.currentInstance.modelBones[model].FirstOrDefault(o => o.Name == ClipBone.Name);
            }

            public void SetPosition(float position)
            {
                var keyframes = ClipBone.Keyframes;
                if (keyframes.Count == 0)
                    return;

                while (position < Keyframe1.Time && currentKeyframe > 0)
                {
                    currentKeyframe--;
                    SetKeyframes();
                }

                while (position >= Keyframe2.Time && currentKeyframe < ClipBone.Keyframes.Count - 2)
                {
                    currentKeyframe++;
                    SetKeyframes();
                }

                if (Keyframe1 == Keyframe2)
                {
                    rotation = Keyframe1.Rotation;
                    translation = Keyframe1.Translation;
                }
                else
                {
                    var t = (float)((position - Keyframe1.Time) / (Keyframe2.Time - Keyframe1.Time));
                    rotation = Quaternion.Slerp(Keyframe1.Rotation, Keyframe2.Rotation, t);
                    translation = Vector3.Lerp(Keyframe1.Translation, Keyframe2.Translation, t);
                }

                valid = true;
                if (assignedBone != null)
                {
                    var m = Matrix.CreateFromQuaternion(rotation);
                    m.Translation = translation;
                    assignedBone.SetCompleteTransform(m);
                }
            }

            private void SetKeyframes()
            {
                if (ClipBone.Keyframes.Count > 0)
                {
                    Keyframe1 = ClipBone.Keyframes[currentKeyframe];
                    if (currentKeyframe == ClipBone.Keyframes.Count - 1)
                        Keyframe2 = Keyframe1;
                    else
                        Keyframe2 = ClipBone.Keyframes[currentKeyframe + 1];
                }
                else
                {
                    Keyframe1 = null;
                    Keyframe2 = null;
                }
            }
        }
    }
}
