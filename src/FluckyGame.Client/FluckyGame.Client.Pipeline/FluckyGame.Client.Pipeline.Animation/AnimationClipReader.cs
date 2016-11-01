using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace FluckyGame.Client.Pipeline.Animation
{
    public class AnimationClipReader : ContentTypeReader<AnimationClip>
    {
        protected override AnimationClip Read(ContentReader input, AnimationClip existingInstance)
        {
            var clip = new AnimationClip();
            clip.Name = input.ReadString();
            clip.Duration = input.ReadDouble();

            int boneCnt = input.ReadInt32();
            for (int i = 0; i < boneCnt; i++)
            {
                var bone = new AnimationClip.Bone();
                clip.Bones.Add(bone);

                bone.Name = input.ReadString();

                int keyframeCnt = input.ReadInt32();
                for (int j = 0; j < keyframeCnt; j++)
                {
                    var keyframe = new AnimationClip.Bone.Keyframe();
                    keyframe.Time = input.ReadDouble();
                    keyframe.Rotation = input.ReadQuaternion();
                    keyframe.Translation = input.ReadVector3();

                    bone.Keyframes.Add(keyframe);
                }
            }

            return clip;
        }
    }
}
