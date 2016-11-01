using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FluckyGame.Client.Entities
{
    class BouncingEntity : ModelEntity
    {
        float ySpeed;

        const float randomMin = 1;
        const float randomMax = 5;

        public BouncingEntity(Model model) :
            base(model)
        {
            ySpeed = ((float)Game1.random.NextDouble() * (randomMax - randomMin)) + randomMin;
        }

        public BouncingEntity(Model model, Vector3 position) :
            base(model, position)
        {
            ySpeed = ((float)Game1.random.NextDouble() * (randomMax - randomMin)) + randomMin;
        }

        public BouncingEntity(Model model, Vector3 position, Vector3 rotation) :
            base(model, position, rotation)
        {
            ySpeed = ((float)Game1.random.NextDouble() * (randomMax - randomMin)) + randomMin;
        }

        public BouncingEntity(Model model, Vector3 position, Vector3 rotation, Vector3 scalation) :
            base(model, position, rotation, scalation)
        {
            ySpeed = ((float)Game1.random.NextDouble() * (randomMax - randomMin)) + randomMin;
        }

        public override void Update(GameTime gameTime, KeyboardState keyboardState, MouseState mouseState)
        {
            base.Update(gameTime, keyboardState, mouseState);

            if (Position.Y > 0)
                ySpeed -= 0.1f / 17 * gameTime.ElapsedGameTime.Milliseconds;
            else
                ySpeed = ((float)Game1.random.NextDouble() * (randomMax - randomMin)) + randomMin;

            Position.Y += ySpeed / 17 * gameTime.ElapsedGameTime.Milliseconds;

            UpdateWorld();
        }
    }
}
