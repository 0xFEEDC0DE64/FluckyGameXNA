using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FluckyGame.Library;

namespace FluckyGame.Client.Entities
{
    public class PlayerEntity : ModelEntity
    {
        bool lastDragging;

        float cameraYaw;
        float CameraYaw { get { return cameraYaw; } }

        float cameraPitch;
        float CameraPitch { get { return cameraPitch; } }

        float cameraDistance;
        float CameraDistance { get { return cameraDistance; } }

        public PlayerEntity() :
            base(Game1.currentInstance.models["dude"])
        {
            AnimationClip = Game1.currentInstance.modelExtras[Model].Clips[0];
            AnimationLooping = true;

            cameraDistance = 200;
        }

        public override void Update(GameTime gameTime, KeyboardState keyboardState, MouseState mouseState)
        {
            base.Update(gameTime, keyboardState, mouseState);

            var shift = keyboardState.IsKeyDown(Keys.LeftShift);

            if (keyboardState.IsKeyDown(Keys.W))
                Position += Vector3.Transform(new Vector3(0, 0, shift ? -3 :-1), Matrix.CreateRotationY(Rotation.X));

            if (keyboardState.IsKeyDown(Keys.S))
                Position += Vector3.Transform(new Vector3(0, 0, shift ? 3 : 1), Matrix.CreateRotationY(Rotation.X));

            if (keyboardState.IsKeyDown(Keys.A))
                Rotation.X += 0.05f;

            if (keyboardState.IsKeyDown(Keys.D))
                Rotation.X -= 0.05f;

            Rotation.X = MathHelper.WrapAngle(Rotation.X);

            if (Game1.currentInstance.IsActive)
            {
                bool currentlyDragging = mouseState.LeftButton == ButtonState.Pressed;

                if (currentlyDragging)
                {
                    var centerX = Game1.currentInstance.graphicsDeviceManager.PreferredBackBufferWidth / 2;
                    var centerY = Game1.currentInstance.graphicsDeviceManager.PreferredBackBufferHeight / 2;

                    if (lastDragging)
                    {
                        cameraYaw -= (mouseState.X - centerX) / 500.0f;
                        cameraPitch -= (mouseState.Y - centerY) / 500.0f;

                        cameraYaw = MathHelper.WrapAngle(cameraYaw);
                        cameraPitch = MathHelper.Clamp(cameraPitch, -MathHelper.PiOver2, MathHelper.PiOver2);
                    }

                    Mouse.SetPosition(centerX, centerY);
                }

                lastDragging = currentlyDragging;
            }

            if (keyboardState.IsKeyDown(Keys.W) ||
                keyboardState.IsKeyDown(Keys.S) ||
                keyboardState.IsKeyDown(Keys.A) ||
                keyboardState.IsKeyDown(Keys.D))
            {
                AnimationPlaying = true;
                AnimationSpeed = keyboardState.IsKeyDown(Keys.S) ? (shift ? -3 : -1) : (shift ? 3 : 1);
                if (Game1.currentInstance.IsActive && mouseState.LeftButton == ButtonState.Released)
                {
                    cameraYaw = MathHelper.SmoothStep(cameraYaw, new List<float>() { Rotation.X - MathHelper.TwoPi, Rotation.X, Rotation.X + MathHelper.TwoPi }.OrderBy(o => Math.Abs(o - cameraYaw)).First(), 0.1f);
                    cameraPitch = MathHelper.SmoothStep(cameraPitch, Rotation.Y + (MathHelper.PiOver4 / 4), 0.1f);
                }

                Game1.currentInstance.SendPacket(new Packet() {
                    { "type", "PLAYER" },
                    { "position", Position },
                    { "rotation", Rotation }
                });
            }
            else
            {
                AnimationPlaying = false;
            }

            UpdateWorld();

            var test = new Vector3(0, 50, 0);

            var view = Matrix.CreateLookAt(test + Position + Vector3.Transform(new Vector3(0, cameraDistance, 0), Matrix.CreateRotationX(cameraPitch - MathHelper.PiOver2) * Matrix.CreateRotationY(cameraYaw - MathHelper.Pi)), test + Position, Vector3.Up);
            foreach (var model in Game1.currentInstance.models.Values)
                foreach (var mesh in model.Meshes)
                    foreach (var effect in mesh.Effects)
                        if (effect is IEffectMatrices)
                        {
                            var meffect = effect as IEffectMatrices;
                            meffect.View = view;
                        }
            Game1.currentInstance.terrain.View = view;
        }
    }
}
