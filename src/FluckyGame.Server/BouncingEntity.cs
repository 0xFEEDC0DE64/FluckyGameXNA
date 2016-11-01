using System;
using Microsoft.Xna.Framework;
using FluckyGame.Library;

namespace FluckyGame.Server
{
    class BouncingEntity : Entity
    {
        private float ySpeed;

        const float randomMin = 1;
        const float randomMax = 5;

        public BouncingEntity(string id, string model, Vector3 position, Vector3 rotation, Vector3 scalation) :
            base(id, model, position, rotation, scalation)
        {
            ySpeed = ((float)Program.random.NextDouble() * (randomMax - randomMin)) + randomMin;
        }

        public override void Update()
        {
            base.Update();

            if (Position.Y > 0)
                ySpeed -= 0.1f;
            else
                ySpeed = ((float)Program.random.NextDouble() * (randomMax - randomMin)) + randomMin;

            Position.Y += ySpeed;

            lock (Program.clients)
                foreach (var client in Program.clients)
                    client.SendPacket(new Packet() {
                                {"type", "UPDATE" },
                                {"id", Id },
                                {"position", Position }
                            });
        }
    }
}
