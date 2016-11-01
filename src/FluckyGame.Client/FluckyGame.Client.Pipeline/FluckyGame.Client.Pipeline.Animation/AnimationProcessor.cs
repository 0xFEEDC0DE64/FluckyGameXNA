using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

namespace FluckyGame.Client.Pipeline.Animation
{
    [ContentProcessor(DisplayName = "Animation Processor")]
    public class AnimationProcessor : ModelProcessor
    {
        private ModelContent model;
        private ModelExtra modelExtra = new ModelExtra();
        private Dictionary<MaterialContent, SkinnedMaterialContent> toSkinnedMaterial = new Dictionary<MaterialContent, SkinnedMaterialContent>();
        private Dictionary<string, int> bones = new Dictionary<string, int>();
        private Matrix[] boneTransforms;
        private Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();

        public override ModelContent Process(NodeContent input, ContentProcessorContext context)
        {
            var skeleton = ProcessSkeleton(input);
            SwapSkinnedMaterial(input);
            model = base.Process(input, context);
            ProcessAnimations(model, input, context);
            model.Tag = modelExtra;
            return model;
        }

        private BoneContent ProcessSkeleton(NodeContent input)
        {
            var skeleton = MeshHelper.FindSkeleton(input);

            if (skeleton == null)
                return null;

            FlattenTransforms(input, skeleton);
            TrimSkeleton(skeleton);

            var nodes = FlattenHeirarchy(input);
            var bones = MeshHelper.FlattenSkeleton(skeleton);

            var nodeToIndex = new Dictionary<NodeContent, int>();
            for (int i = 0; i < nodes.Count; i++)
                nodeToIndex[nodes[i]] = i;

            foreach (var bone in bones)
                modelExtra.Skeleton.Add(nodeToIndex[bone]);

            return skeleton;
        }

        private List<NodeContent> FlattenHeirarchy(NodeContent item)
        {
            var nodes = new List<NodeContent>();
            nodes.Add(item);

            foreach (var child in item.Children)
                FlattenHeirarchy(nodes, child);

            return nodes;
        }

        private void FlattenHeirarchy(List<NodeContent> nodes, NodeContent item)
        {
            nodes.Add(item);
            foreach (var child in item.Children)
                FlattenHeirarchy(nodes, child);
        }

        void FlattenTransforms(NodeContent node, BoneContent skeleton)
        {
            foreach (var child in node.Children)
            {
                if (child == skeleton)
                    continue;

                if(IsSkinned(child))
                    FlattenAllTransforms(child);
            }
        }

        void FlattenAllTransforms(NodeContent node)
        {
            MeshHelper.TransformScene(node, node.Transform);

            node.Transform = Matrix.Identity;

            foreach (var child in node.Children)
                FlattenAllTransforms(child);
        }

        void TrimSkeleton(NodeContent skeleton)
        {
            var todelete = new List<NodeContent>();

            foreach (var child in skeleton.Children)
                if (child.Name.EndsWith("Nub") || child.Name.EndsWith("Footsteps"))
                    todelete.Add(child);
                else
                    TrimSkeleton(child);

            foreach (var child in todelete)
                skeleton.Children.Remove(child);
        }

        bool IsSkinned(NodeContent node)
        {
            var mesh = node as MeshContent;
            if (mesh != null)
                foreach (var geometry in mesh.Geometry)
                    foreach (var vchannel in geometry.Vertices.Channels)
                        if (vchannel is VertexChannel<BoneWeightCollection>)
                            return true;

            return false;
        }

        void SwapSkinnedMaterial(NodeContent node)
        {
            var mesh = node as MeshContent;
            if (mesh != null)
            {
                foreach (var geometry in mesh.Geometry)
                {
                    bool swap = false;
                    foreach (var vchannel in geometry.Vertices.Channels)
                    {
                        if (vchannel is VertexChannel<BoneWeightCollection>)
                        {
                            swap = true;
                            break;
                        }
                    }

                    if (swap)
                    {
                        if (toSkinnedMaterial.ContainsKey(geometry.Material))
                            geometry.Material = toSkinnedMaterial[geometry.Material];
                        else
                        {
                            var smaterial = new SkinnedMaterialContent();
                            var bmaterial = geometry.Material as BasicMaterialContent;

                            smaterial.Alpha = bmaterial.Alpha;
                            smaterial.DiffuseColor = bmaterial.DiffuseColor;
                            smaterial.EmissiveColor = bmaterial.EmissiveColor;
                            smaterial.SpecularColor = bmaterial.SpecularColor;
                            smaterial.SpecularPower = bmaterial.SpecularPower;
                            smaterial.Texture = bmaterial.Texture;
                            smaterial.WeightsPerVertex = 4;

                            toSkinnedMaterial[geometry.Material] = smaterial;
                            geometry.Material = smaterial;
                        }
                    }
                }
            }

            foreach (var child in node.Children)
                SwapSkinnedMaterial(child);
        }

        private void ProcessAnimations(ModelContent model, NodeContent input, ContentProcessorContext context)
        {
            for (int i = 0; i < model.Bones.Count; i++)
                bones[model.Bones[i].Name] = i;

            boneTransforms = new Matrix[model.Bones.Count];

            ProcessAnimationsRecursive(input);

            if (modelExtra.Clips.Count == 0)
            {
                var clip = new AnimationClip();
                modelExtra.Clips.Add(clip);

                var clipName = "Take 001";

                clips[clipName] = clip;

                clip.Name = clipName;
                foreach (var bone in model.Bones)
                {
                    var clipBone = new AnimationClip.Bone();
                    clipBone.Name = bone.Name;

                    clip.Bones.Add(clipBone);
                }
            }

            foreach (var clip in modelExtra.Clips)
            {
                for (int b = 0; b < bones.Count; b++)
                {
                    var keyframes = clip.Bones[b].Keyframes;
                    if (keyframes.Count == 0 || keyframes[0].Time > 0)
                    {
                        var keyframe = new AnimationClip.Bone.Keyframe();
                        keyframe.Time = 0;
                        keyframe.Transform = boneTransforms[b];
                        keyframes.Insert(0, keyframe);
                    }
                }
            }
        }

        private void ProcessAnimationsRecursive(NodeContent input)
        {
            int inputBoneIndex;
            if (bones.TryGetValue(input.Name, out inputBoneIndex))
                boneTransforms[inputBoneIndex] = input.Transform;

            foreach (var animation in input.Animations)
            {
                AnimationClip clip;
                string clipName = animation.Key;

                if (!clips.TryGetValue(clipName, out clip))
                {
                    clip = new AnimationClip();
                    modelExtra.Clips.Add(clip);

                    clips[clipName] = clip;

                    clip.Name = clipName;
                    foreach (var bone in model.Bones)
                    {
                        var clipBone = new AnimationClip.Bone();
                        clipBone.Name = bone.Name;

                        clip.Bones.Add(clipBone);
                    }
                }

                if (animation.Value.Duration.TotalSeconds > clip.Duration)
                    clip.Duration = animation.Value.Duration.TotalSeconds;

                foreach (var channel in animation.Value.Channels)
                {
                    int boneIndex;
                    if (!bones.TryGetValue(channel.Key, out boneIndex))
                        continue;

                    if (UselessAnimationTest(boneIndex))
                        continue;

                    var keyframes = new LinkedList<AnimationClip.Bone.Keyframe>();
                    foreach (var keyframe in channel.Value)
                    {
                        var transform = keyframe.Transform;

                        var newKeyframe = new AnimationClip.Bone.Keyframe();
                        newKeyframe.Time = keyframe.Time.TotalSeconds;
                        newKeyframe.Transform = transform;

                        keyframes.AddLast(newKeyframe);
                    }

                    LinearKeyframeReduction(keyframes);
                    foreach (var keyframe in keyframes)
                        clip.Bones[boneIndex].Keyframes.Add(keyframe);
                }
            }

            foreach (var child in input.Children)
                ProcessAnimationsRecursive(child);
        }

        private const float TinyLength = 1e-8f;
        private const float TinyCosAngle = 0.9999999f;

        private void LinearKeyframeReduction(LinkedList<AnimationClip.Bone.Keyframe> keyframes)
        {
            if (keyframes.Count < 3)
                return;

            for (var node = keyframes.First.Next; ; )
            {
                var next = node.Next;
                if (next == null)
                    break;

                // Determine nodes before and after the current node.
                var a = node.Previous.Value;
                var b = node.Value;
                var c = next.Value;

                var t = (float)((node.Value.Time - node.Previous.Value.Time) / (next.Value.Time - node.Previous.Value.Time));

                var translation = Vector3.Lerp(a.Translation, c.Translation, t);
                var rotation = Quaternion.Slerp(a.Rotation, c.Rotation, t);

                if ((translation - b.Translation).LengthSquared() < TinyLength && Quaternion.Dot(rotation, b.Rotation) > TinyCosAngle)
                    keyframes.Remove(node);

                node = next;
            }
        }

        bool UselessAnimationTest(int boneId)
        {
            foreach (var mesh in model.Meshes)
                if (mesh.ParentBone.Index == boneId)
                    return false;

            foreach (int b in modelExtra.Skeleton)
                if (boneId == b)
                    return false;

            return true;
        }
    }
}