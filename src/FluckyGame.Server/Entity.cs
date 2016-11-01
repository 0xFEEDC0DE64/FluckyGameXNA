using System;
using Microsoft.Xna.Framework;

namespace FluckyGame.Server
{
    class Entity
    {
        public string Id;
        public string Model;
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scalation;

        public Entity(string id, string model, Vector3 position, Vector3 rotation, Vector3 scalation)
        {
            Id = id;
            Model = model;
            Position = position;
            Rotation = rotation;
            Scalation = scalation;
        }

        public virtual void Update() { }
    }
}
