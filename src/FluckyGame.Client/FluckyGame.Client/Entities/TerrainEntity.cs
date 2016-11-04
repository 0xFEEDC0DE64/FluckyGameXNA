using System;
using System.Collections.Generic;
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

        public TerrainEntity(int[,] world, GraphicsDevice graphicsDevice, Matrix projection) :
            base()
        {
            this.graphicsDevice = graphicsDevice;
            effect = new BasicEffect(this.graphicsDevice);
            effect.Projection = projection;
            effect.VertexColorEnabled = true;

            const int size = 10;

            var verticesList = new List<VertexPositionColor>();
            var indicesList = new List<short>();

            for (int z = 0; z < 100; z++)
                for (int x = 0; x < 100; x++)
                {
                    float y = world[x, z] - 25;

                    short index00 = (short)verticesList.Count;
                    verticesList.Add(new VertexPositionColor(new Vector3(x * size, y, z * size), Color.Red));
                    short index10 = (short)verticesList.Count;
                    verticesList.Add(new VertexPositionColor(new Vector3((x + 1) * size, y, z * size), Color.Green));
                    short index01 = (short)verticesList.Count;
                    verticesList.Add(new VertexPositionColor(new Vector3(x * size, y, (z + 1) * size), Color.Blue));
                    short index11 = (short)verticesList.Count;
                    verticesList.Add(new VertexPositionColor(new Vector3((x + 1) * size, y, (z + 1) * size), Color.Red));

                    short indexBottom00 = (short)verticesList.Count;
                    verticesList.Add(new VertexPositionColor(new Vector3(x * size, -100, z * size), Color.Red));
                    short indexBottom10 = (short)verticesList.Count;
                    verticesList.Add(new VertexPositionColor(new Vector3((x + 1) * size, -100, z * size), Color.Green));
                    short indexBottom01 = (short)verticesList.Count;
                    verticesList.Add(new VertexPositionColor(new Vector3(x * size, -100, (z + 1) * size), Color.Blue));
                    short indexBottom11 = (short)verticesList.Count;
                    verticesList.Add(new VertexPositionColor(new Vector3((x + 1) * size, -100, (z + 1) * size), Color.Red));

                    //Top
                    indicesList.Add(index11);
                    indicesList.Add(index01);
                    indicesList.Add(index00);

                    indicesList.Add(index10);
                    indicesList.Add(index11);
                    indicesList.Add(index00);

                    //North
                    indicesList.Add(index00);
                    indicesList.Add(index01);
                    indicesList.Add(indexBottom01);

                    indicesList.Add(index00);
                    indicesList.Add(indexBottom01);
                    indicesList.Add(indexBottom00);

                    //West
                    indicesList.Add(index10);
                    indicesList.Add(index00);
                    indicesList.Add(indexBottom00);

                    indicesList.Add(index10);
                    indicesList.Add(indexBottom00);
                    indicesList.Add(indexBottom10);

                    //South
                    indicesList.Add(index11);
                    indicesList.Add(index10);
                    indicesList.Add(indexBottom10);

                    indicesList.Add(index11);
                    indicesList.Add(indexBottom10);
                    indicesList.Add(indexBottom11);

                    //East
                    indicesList.Add(index01);
                    indicesList.Add(index11);
                    indicesList.Add(indexBottom11);

                    indicesList.Add(index01);
                    indicesList.Add(indexBottom11);
                    indicesList.Add(indexBottom01);
                }

            vertices = verticesList.ToArray();
            indices = indicesList.ToArray();
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            effect.CurrentTechnique.Passes[0].Apply();
            graphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length, indices, 0, indices.Length / 3);
        }
    }
}
