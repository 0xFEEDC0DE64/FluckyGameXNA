using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

namespace FluckyGame.Client.Pipeline.Animation
{
    [ContentTypeWriter]
    public class AnimationClipWriter : ContentTypeWriter<AnimationClip>
    {
        protected override void Write(ContentWriter output, AnimationClip clip)
        {
            output.Write(clip.Name);
            output.Write(clip.Duration);
            output.Write(clip.Bones.Count);

            foreach (var bone in clip.Bones)
            {
                output.Write(bone.Name);
                output.Write(bone.Keyframes.Count);

                foreach (var keyframe in bone.Keyframes)
                {
                    output.Write(keyframe.Time);
                    output.Write(keyframe.Rotation);
                    output.Write(keyframe.Translation);
                }
            }
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(AnimationClipReader).AssemblyQualifiedName;
        }
    }
}
