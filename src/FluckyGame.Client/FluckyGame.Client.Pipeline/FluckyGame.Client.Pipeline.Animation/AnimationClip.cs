using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

namespace FluckyGame.Client.Pipeline.Animation
{
    public class AnimationClip
    {
        public class Bone
        {
            public class Keyframe
            {
                public double Time;
                public Quaternion Rotation;
                public Vector3 Translation;

                public Matrix Transform
                {
                    get
                    {
                        return Matrix.CreateFromQuaternion(Rotation) * Matrix.CreateTranslation(Translation);
                    }
                    set
                    {
                        Matrix transform = value;
                        transform.Right = Vector3.Normalize(transform.Right);
                        transform.Up = Vector3.Normalize(transform.Up);
                        transform.Backward = Vector3.Normalize(transform.Backward);
                        Rotation = Quaternion.CreateFromRotationMatrix(transform);
                        Translation = transform.Translation;
                    }
                }
            }

            private string name = "";
            private List<Keyframe> keyframes = new List<Keyframe>();
            public string Name { get { return name; } set { name = value; } }
            public List<Keyframe> Keyframes { get { return keyframes; } }
        }

        private List<Bone> bones = new List<Bone>();
        public string Name;
        public double Duration;
        public List<Bone> Bones { get { return bones; } }
    }
}
