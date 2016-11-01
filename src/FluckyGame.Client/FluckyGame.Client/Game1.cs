using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FluckyGame.Client.Entities;
using FluckyGame.Client.Pipeline.Animation;
using FluckyGame.Library;

namespace FluckyGame.Client
{
    internal class Game1 : Microsoft.Xna.Framework.Game
    {
        public static Game1 currentInstance;
        public static Random random;

        SpriteBatch spriteBatch;
        SpriteFont spriteFont;

        public GraphicsDeviceManager graphicsDeviceManager;

        public Dictionary<string, Model> models;
        public Dictionary<Model, ModelExtra> modelExtras;
        public Dictionary<Model, List<Bone>> modelBones;

        public List<Entity> entities;
        public Dictionary<string, ModelEntity> entitiesById;
        public PlayerEntity player;
        public TerrainEntity terrain;

        bool[] last;
        bool wireframe;

        public Thread networkingThread;
        public TcpClient tcpClient;
        public NetworkStream networkStream;
        private Queue<Packet> queue;
        private bool exiting;

        static Game1()
        {
            random = new Random();
        }

        public Game1(TcpClient tcpClient, ClientSettings clientSettings)
        {
            this.tcpClient = tcpClient;

            currentInstance = this;

            graphicsDeviceManager = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = clientSettings.resolution.width,
                PreferredBackBufferHeight = clientSettings.resolution.height,
                IsFullScreen = clientSettings.resolution.fullscreen
            };
            Content.RootDirectory = "Content";
            
            IsMouseVisible = true;

            last = new bool[] { false, false, false, false, false, false, false, false, false, false };
        }

        protected override void Initialize()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            spriteFont = Content.Load<SpriteFont>("SpriteFont");

            models = new Dictionary<string, Model>();
            modelExtras = new Dictionary<Model, ModelExtra>();
            modelBones = new Dictionary<Model, List<Bone>>();

            var projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 0.1f, 5000.0f);
            foreach (var modelName in new[] { "box", "cone", "cylinder", "dude", "figure", "monkey", "sphere", "torus", "Victoria-hat-dance", "Victoria-hat-tpose" })
            {
                var model = Content.Load<Model>(Path.Combine("Models", modelName));

                foreach (var mesh in model.Meshes)
                {
                    foreach (var effect in mesh.Effects)
                    {
                        if (effect is IEffectMatrices)
                            (effect as IEffectMatrices).Projection = projection;

                        if (effect is IEffectLights)
                            (effect as IEffectLights).EnableDefaultLighting();

                        if (effect is BasicEffect)
                            (effect as BasicEffect).PreferPerPixelLighting = true;

                        if (effect is SkinnedEffect)
                            (effect as SkinnedEffect).PreferPerPixelLighting = true;
                    }
                }

                models.Add(modelName, model);

                var bones = new List<Bone>();
                foreach (ModelBone bone in model.Bones)
                    bones.Add(new Bone(bone.Name, bone.Transform, bone.Parent != null ? bones[bone.Parent.Index] : null));
                modelBones.Add(model, bones);

                if (model.Tag is ModelExtra)
                {
                    var modelExtra = model.Tag as ModelExtra;
                    modelExtras.Add(model, modelExtra);
                }
            }

            var texture = Content.Load<Texture2D>(Path.Combine("Models", "figure_texture"));
            foreach (var mesh in models["figure"].Meshes)
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.Texture = texture;
                    effect.TextureEnabled = true;
                }

            entities = new List<Entity>()
            {
                (player = new PlayerEntity()),
                (terrain = new TerrainEntity(200, 200, GraphicsDevice, projection))
            };

            entitiesById = new Dictionary<string, ModelEntity>();

            queue = new Queue<Packet>();

            networkStream = this.tcpClient.GetStream();

            networkingThread = new Thread(ReceivePackets);
            networkingThread.IsBackground = true;
            networkingThread.Start();

            base.Initialize();
        }

        private void ReceivePackets()
        {
            while(true)
            {
                try
                {
                    var packet = Packet.Receive(networkStream);

                    lock (queue)
                        queue.Enqueue(packet);
                }
                catch(Exception ex)
                {
                    if (!exiting)
                    {
                        System.Windows.Forms.MessageBox.Show("Verbindungsfehler:\n\n" + ex.Message, "Verbindungsfehler!", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                        Exit();
                    }
                    return;
                }
            }
        }

        public void SendPacket(Packet packet)
        {
            packet.Send(networkStream);
        }

        protected override void Update(GameTime gameTime)
        {
            lock(queue)
            {
                while(queue.Count > 0)
                {
                    var packet = queue.Dequeue();

                    var type = (string)packet["type"];
                    if (type == "NEW")
                    {
                        var model = (string)packet["model"];
                        var position = (Vector3)packet["position"];
                        var rotation = (Vector3)packet["rotation"];
                        var scalation = (Vector3)packet["scalation"];
                        var entity = new ModelEntity(
                            models[model],
                            position,
                            rotation,
                            scalation
                        );
                        entities.Add(entity);
                        entitiesById.Add((string)packet["id"], entity);
                    }
                    else if(type == "REMOVE")
                    {
                        var id = (string)packet["id"];
                        entities.Remove(entitiesById[id]);
                        entitiesById.Remove(id);
                    }
                    else if (type == "UPDATE")
                    {
                        ModelEntity entity = entitiesById[(string)packet["id"]];
                        if (packet.ContainsKey("position"))
                            entity.Position = (Vector3)packet["position"];
                        if (packet.ContainsKey("rotation"))
                            entity.Rotation = (Vector3)packet["rotation"];
                        if (packet.ContainsKey("scalation"))
                            entity.Scalation = (Vector3)packet["scalation"];
                        entity.UpdateWorld();
                    }
                    else
                        throw new Exception("Unknown packet type!");
                }
            }

            var kState = IsActive ? Keyboard.GetState() : new KeyboardState();
            var mState = IsActive ? Mouse.GetState() : new MouseState();

            if (kState.IsKeyDown(Keys.Escape))
            {
                exiting = true;
                networkingThread.Abort();
                Exit();
            }

            if (kState.IsKeyDown(Keys.D1) && !last[0])
                SendPacket(new Packet() {
                    { "type", "ADD" },
                    { "model", "box" },
                    { "position", player.Position + Vector3.Transform(new Vector3(0, 0, -100), Matrix.CreateRotationY(player.Rotation.X)) + new Vector3(0, 25, 0) },
                    { "rotation", player.Rotation },
                    { "scalation", new Vector3(25) }
                });
            last[0] = kState.IsKeyDown(Keys.D1);

            if (kState.IsKeyDown(Keys.D2) && !last[1])
                SendPacket(new Packet() {
                    { "type", "ADD" },
                    { "model", "cone" },
                    { "position", player.Position + Vector3.Transform(new Vector3(0, 0, -100), Matrix.CreateRotationY(player.Rotation.X)) + new Vector3(0, 25, 0) },
                    { "rotation", player.Rotation },
                    { "scalation", new Vector3(25) }
                });
            last[1] = kState.IsKeyDown(Keys.D2);

            if (kState.IsKeyDown(Keys.D3) && !last[2])
                SendPacket(new Packet() {
                    { "type", "ADD" },
                    { "model", "cylinder" },
                    { "position", player.Position + Vector3.Transform(new Vector3(0, 0, -100), Matrix.CreateRotationY(player.Rotation.X)) + new Vector3(0, 25, 0) },
                    { "rotation", player.Rotation },
                    { "scalation", new Vector3(25) }
                });
            last[2] = kState.IsKeyDown(Keys.D3);

            if (kState.IsKeyDown(Keys.D4) && !last[3])
                SendPacket(new Packet() {
                    { "type", "ADD" },
                    { "model", "dude" },
                    { "position", player.Position + Vector3.Transform(new Vector3(0, 0, -100), Matrix.CreateRotationY(player.Rotation.X)) + new Vector3(0, 25, 0) },
                    { "rotation", player.Rotation },
                    { "scalation", new Vector3(1) }
                });
            last[3] = kState.IsKeyDown(Keys.D4);

            if (kState.IsKeyDown(Keys.D5) && !last[4])
                SendPacket(new Packet() {
                    { "type", "ADD" },
                    { "model", "monkey" },
                    { "position", player.Position + Vector3.Transform(new Vector3(0, 0, -100), Matrix.CreateRotationY(player.Rotation.X)) + new Vector3(0, 25, 0) },
                    { "rotation", player.Rotation },
                    { "scalation", new Vector3(25) }
                });
            last[4] = kState.IsKeyDown(Keys.D5);

            if (kState.IsKeyDown(Keys.D6) && !last[5])
                SendPacket(new Packet() {
                    { "type", "ADD" },
                    { "model", "sphere" },
                    { "position", player.Position + Vector3.Transform(new Vector3(0, 0, -100), Matrix.CreateRotationY(player.Rotation.X)) + new Vector3(0, 25, 0) },
                    { "rotation", player.Rotation },
                    { "scalation", new Vector3(25) }
                });
            last[5] = kState.IsKeyDown(Keys.D6);

            if (kState.IsKeyDown(Keys.D7) && !last[6])
                SendPacket(new Packet() {
                    { "type", "ADD" },
                    { "model", "torus" },
                    { "position", player.Position + Vector3.Transform(new Vector3(0, 0, -100), Matrix.CreateRotationY(player.Rotation.X)) + new Vector3(0, 25, 0) },
                    { "rotation", player.Rotation },
                    { "scalation", new Vector3(25) }
                });
            last[6] = kState.IsKeyDown(Keys.D7);

            if (kState.IsKeyDown(Keys.D8) && !last[7])
                entities.Add(new ModelEntity(models["Victoria-hat-tpose"], player.Position + Vector3.Transform(new Vector3(0, 0, -100), Matrix.CreateRotationY(player.Rotation.X)), player.Rotation));
            last[7] = kState.IsKeyDown(Keys.D8);

            if (kState.IsKeyDown(Keys.D9) && !last[8])
            {
                var entity = new ModelEntity(models["Victoria-hat-tpose"], player.Position + Vector3.Transform(new Vector3(0, 0, -100), Matrix.CreateRotationY(player.Rotation.X)), player.Rotation, new Vector3(0.4f));
                entity.AnimationClip = modelExtras[models["Victoria-hat-dance"]].Clips[0];
                entity.AnimationLooping = true;
                entity.AnimationPlaying = true;
                entities.Add(entity);
            }
            last[8] = kState.IsKeyDown(Keys.D9);

            if (kState.IsKeyDown(Keys.R) && !last[9])
                wireframe = !wireframe;
            last[9] = kState.IsKeyDown(Keys.R);

            foreach (var entity in entities)
                entity.Update(gameTime, kState, mState);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            if(wireframe)
            {
                var rState = new RasterizerState();
                rState.FillMode = FillMode.WireFrame;
                GraphicsDevice.RasterizerState = rState;
            }

            foreach (var entity in entities)
                entity.Draw(gameTime);

            spriteBatch.Begin();

            {
                var text = string.Format("{0} FPS", (int)(1000 / gameTime.ElapsedGameTime.TotalMilliseconds));
                spriteBatch.DrawString(spriteFont, text, new Vector2(graphicsDeviceManager.PreferredBackBufferWidth - spriteFont.MeasureString(text).X, 0), Color.White);
            }

            spriteBatch.DrawString(spriteFont, string.Format("Player: Position: {0:F2}, {1:F2}, {2:F2} Rotation: {3:F2} {4:F2} {5:F2}", player.Position.X, player.Position.Y, player.Position.Z, player.Rotation.X, player.Rotation.Y, player.Rotation.Z), Vector2.Zero, Color.White);

            spriteBatch.DrawString(spriteFont, "Press any key to spawn entity:", new Vector2(0, 40), Color.White);
            spriteBatch.DrawString(spriteFont, "1 - Box", new Vector2(0, 60), Color.White);
            spriteBatch.DrawString(spriteFont, "2 - Cone", new Vector2(0, 80), Color.White);
            spriteBatch.DrawString(spriteFont, "3 - Cylinder", new Vector2(0, 100), Color.White);
            spriteBatch.DrawString(spriteFont, "4 - Dude", new Vector2(0, 120), Color.White);
            spriteBatch.DrawString(spriteFont, "5 - Monkey", new Vector2(0, 140), Color.White);
            spriteBatch.DrawString(spriteFont, "6 - Sphere", new Vector2(0, 160), Color.White);
            spriteBatch.DrawString(spriteFont, "7 - Torus", new Vector2(0, 180), Color.White);
            spriteBatch.DrawString(spriteFont, "8 - Victoria (not synced)", new Vector2(0, 200), Color.White);
            spriteBatch.DrawString(spriteFont, "9 - Victoria (dancing) (not synced)", new Vector2(0, 220), Color.White);
            spriteBatch.DrawString(spriteFont, string.Format("{0} entities", entities.Count), new Vector2(0, 260), Color.White);

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
