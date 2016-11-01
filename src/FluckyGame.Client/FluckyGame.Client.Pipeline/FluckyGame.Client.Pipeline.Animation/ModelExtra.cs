using System;
using System.Collections.Generic;

namespace FluckyGame.Client.Pipeline.Animation
{
    public class ModelExtra
    {
        private List<int> skeleton = new List<int>();
        private List<AnimationClip> clips = new List<AnimationClip>();

        public List<int> Skeleton { get { return skeleton; } set { skeleton = value; } }
        public List<AnimationClip> Clips { get { return clips; } set { clips = value; } }
    }
}
