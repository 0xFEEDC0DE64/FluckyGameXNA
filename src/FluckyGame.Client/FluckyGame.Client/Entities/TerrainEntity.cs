using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FluckyGame.Client.Entities
{
    public class TerrainEntity : Entity
    {
        VertexPositionColor[] vertices;
        short[] indices;
        BasicEffect effect;
        GraphicsDevice graphicsDevice;

        public Matrix View { set { effect.View = value; } }

        public TerrainEntity(int width, int height, GraphicsDevice graphicsDevice, Matrix projection) :
            base()
        {
            this.graphicsDevice = graphicsDevice;
            effect = new BasicEffect(this.graphicsDevice);
            effect.Projection = projection;
            effect.VertexColorEnabled = true;

            vertices = new VertexPositionColor[width * height];
            indices = new short[(width - 1) * (height - 1) * 6];
            int currentIndex = 0;
            for (int x = 0; x < width; x++)
                for(int z = 0; z < height; z++)
                {
                    vertices[x + z * width].Position = new Vector3(-8000 + (16000 / width * x), (float)Game1.random.NextDouble() * 50 - 50, -8000 + (16000 / height * z));
                    vertices[x + z * width].Color = new Color(Game1.random.Next(0, 255), Game1.random.Next(0, 255), Game1.random.Next(0, 255));

                    if(x < width - 1 && z < height - 1)
                    {
                        short downLeft = (short)(x + z * width);
                        short downRight = (short)((x + 1) + z * width);
                        short upLeft = (short)(x + (z + 1) * width);
                        short upRight = (short)((x + 1) + (z + 1) * width);

                        indices[currentIndex++] = downRight;
                        indices[currentIndex++] = upLeft;
                        indices[currentIndex++] = downLeft;
                        indices[currentIndex++] = upRight;
                        indices[currentIndex++] = upLeft;
                        indices[currentIndex++] = downRight;
                    }
                }
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            effect.CurrentTechnique.Passes[0].Apply();
            graphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length, indices, 0, indices.Length / 3);
        }
    }
}
