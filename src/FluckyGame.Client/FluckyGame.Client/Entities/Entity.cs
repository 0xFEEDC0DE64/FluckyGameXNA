using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FluckyGame.Client
{
    public class Entity
    {
        public virtual void Update(GameTime gameTime, KeyboardState keyboardState, MouseState mouseState)
        { }

        public virtual void Draw(GameTime gameTime)
        { }
    }
}
