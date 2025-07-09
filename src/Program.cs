using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static VoxelTechDemo.VoxelRenderer;
using static VoxelTechDemo.UserSettings;

using var game = new VoxelTechDemo.Game1();
game.Run();

namespace VoxelTechDemo {
    public class Game1 : Game {
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _spriteFont;
        private float yaw = MathHelper.PiOver2, pitch;
        private bool IsPaused = false, IsNoClipOn = false;
        private KeyboardState lastKeyboardState;
        private MouseState lastMouseState;
        private byte chosenBlock = 1;
        private Point WindowCenter;
        private CustomEffect solidEffect;
        private FluidEffect fluidEffect;
        private Effect blockPreviewEffect;
        private Matrix previewMatrix;

        public Matrix projectionMatrix, viewMatrix;
        public readonly World world = new(12345);
        public Player player;
        
        public Game1() {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            // Setting window size
            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

            // Loading user settings from a file
            LoadSettings();
            if (FrameRateUnlocked) {
                _graphics.SynchronizeWithVerticalRetrace = !_graphics.SynchronizeWithVerticalRetrace;
                IsFixedTimeStep = !IsFixedTimeStep;
            }
            if (Fullscreen) {
                _graphics.IsFullScreen = true;
            }
        }
        protected override void Initialize() {
            InitializeVoxelRenderer(GraphicsDevice);

            // Custom shader setup
            solidEffect = new(Content.Load<Effect>("CustomEffect"), this);
            fluidEffect = new(Content.Load<Effect>("FluidEffect"), this);

            // Setting up block preview effect
            previewMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Up);
            previewMatrix *= Matrix.CreateLookAt(new Vector3(3, 2, 3), new Vector3(0.5f, 0.5f, 0.5f), Vector3.Up);
            previewMatrix *= CreateBlockPreviewProj((int)(GraphicsDevice.Viewport.Width * 0.93f), (int)(GraphicsDevice.Viewport.Height * 0.9f), 5);
            ChangeCubePreview(chosenBlock);
            blockPreviewEffect = solidEffect;

            UserInterface.Initialize(this, _graphics);

            player = new Player(world);
            world.UpdateLoadedChunks(0, 0);
            world.GenerateChunkLine(0, 0);

            //Camera setup
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(FieldOfView), GraphicsDevice.DisplayMode.AspectRatio, 0.1f, 10000f);
            WindowCenter = new Point(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);

            base.Initialize();
        }
        protected override void LoadContent() {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _spriteFont = Content.Load<SpriteFont>("PublicPixel");
        }
        protected override void Update(GameTime gameTime) {
            KeyboardState keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.Escape) && lastKeyboardState.IsKeyUp(Keys.Escape)) {
                IsPaused = !IsPaused;
                IsMouseVisible = !IsMouseVisible;
                Mouse.SetPosition(WindowCenter.X, WindowCenter.Y);
                CheckSettingsFile();
            }
            if (!IsPaused) {
                //Camera look
                MouseState currentMouseState = Mouse.GetState();
                yaw -= (currentMouseState.X - WindowCenter.X) * MouseSensitivity;
                pitch -= (currentMouseState.Y - WindowCenter.Y) * MouseSensitivity;
                pitch = MathHelper.Clamp(pitch, -1.57f, 1.57f);//-MathHelper.PiOver2, MathHelper.PiOver2
                player.right = Vector3.Transform(Vector3.Right, Matrix.CreateFromYawPitchRoll(yaw, pitch, 0f));

                //Check pressed keys
                if (keyboardState.IsKeyDown(Keys.N) && lastKeyboardState.IsKeyUp(Keys.N)) {
                    IsNoClipOn = !IsNoClipOn;
                    player.ResetHitBox();
                }
                if (IsNoClipOn) {
                    player.NoClipMovement(keyboardState, gameTime);
                }
                else {
                    player.NormalMovement(keyboardState, gameTime, yaw);
                }

                //Check looked at block
                player.forward = Vector3.Transform(Vector3.Forward, Matrix.CreateFromYawPitchRoll(yaw, pitch, 0f));
                player.GetLookedAtBlock();

                if (currentMouseState.LeftButton == ButtonState.Pressed && lastMouseState.LeftButton == ButtonState.Released && player.blockFound) {
                    world.SetBlock(player.LookedAtBlock, player.CurrentChunk, 0);
                }
                if (currentMouseState.RightButton == ButtonState.Pressed && lastMouseState.RightButton == ButtonState.Released && player.blockFound) {
                    world.SetBlock(player.LookedAtBlock, player.CurrentChunk, chosenBlock, player.currentSide, player.playerHitBox);
                }
                if(currentMouseState.ScrollWheelValue != lastMouseState.ScrollWheelValue) {
                    chosenBlock = (byte)((currentMouseState.ScrollWheelValue / 120 % 15 + 15) % 15);
                    if (chosenBlock == 0) {
                        blockPreviewEffect = fluidEffect;
                        ChangeCubePreview(255);
                        chosenBlock = 255;
                    }
                    else {
                        blockPreviewEffect = solidEffect;
                        ChangeCubePreview(chosenBlock);
                    }
                }
                lastMouseState = currentMouseState;
                Mouse.SetPosition(WindowCenter.X, WindowCenter.Y);
            }
            viewMatrix = Matrix.CreateLookAt(player.camPosition, player.camPosition + player.forward, Vector3.Up);
            lastKeyboardState = keyboardState;
            base.Update(gameTime);
        }
        protected override void Draw(GameTime gameTime) {
            if (player.IsUnderWater) {
                GraphicsDevice.Clear(new Color(new Vector3(0.3f, 0.3f, 0.7f)));
                solidEffect.ApplyUnderWaterSettings();
                fluidEffect.ApplyUnderWaterSettings();
            }
            else {
                GraphicsDevice.Clear(Color.CornflowerBlue);
                solidEffect.ApplyNormalSettings();
                fluidEffect.ApplyNormalSettings();
            }
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            GraphicsDevice.Indices = indexBuffer;

            Matrix worldMatrix;
            Matrix viewProj = viewMatrix * projectionMatrix;
            BoundingFrustum frustum = new(viewProj);

            fluidEffect.UpdateAnimationFrame(gameTime.ElapsedGameTime);

            //TODO: Make it so it doesn't recalculate world matrices every frame
            // Render solid blocks
            for (int x = -RenderDistance; x <= RenderDistance; x++) {
                for (int z = -RenderDistance; z <= RenderDistance; z++) {
                    if (world.CurrentlyLoadedChunkLines.Contains((x + player.CurrentChunk.x, z + player.CurrentChunk.z)) && frustum.Intersects(new BoundingBox(
                    new Vector3(x, -player.CurrentChunk.y, z) * ChunkSize, new Vector3(x + 1, -player.CurrentChunk.y + World.MaxHeight / ChunkSize, z + 1) * ChunkSize))) {
                        worldMatrix = Matrix.CreateWorld(new Vector3(x, -player.CurrentChunk.y, z) * ChunkSize, Vector3.Forward, Vector3.Up);
                        solidEffect.WorldViewProj.SetValue(worldMatrix * viewProj);
                        solidEffect.Apply(worldMatrix * viewMatrix);
                        for (int y = 0; y < World.MaxHeight / ChunkSize; y++) {
                            DrawChunk(world.WorldMap[(x + player.CurrentChunk.x, y, z + player.CurrentChunk.z)].vertexBufferOpaque);
                        }
                    }
                }
            }

            // Render fluids
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            for (int x = -RenderDistance; x <= RenderDistance; x++) {
                for (int z = -RenderDistance; z <= RenderDistance; z++) {
                    if (world.CurrentlyLoadedChunkLines.Contains((x + player.CurrentChunk.x, z + player.CurrentChunk.z)) && frustum.Intersects(new BoundingBox(
                    new Vector3(x, -player.CurrentChunk.y, z) * ChunkSize, new Vector3(x + 1, -player.CurrentChunk.y + World.MaxHeight / ChunkSize, z + 1) * ChunkSize))) {
                        worldMatrix = Matrix.CreateWorld(new Vector3(x, -player.CurrentChunk.y, z) * ChunkSize, Vector3.Forward, Vector3.Up);
                        fluidEffect.WorldViewProj.SetValue(worldMatrix * viewProj);
                        fluidEffect.Apply(worldMatrix * viewMatrix);
                        for (int y = 0; y < World.MaxHeight / ChunkSize; y++) {
                            DrawChunk(world.WorldMap[(x + player.CurrentChunk.x, y, z + player.CurrentChunk.z)].vertexBufferTransparent);
                        }
                    }
                }
            }
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

            worldMatrix = Matrix.CreateWorld(player.LookedAtBlock, Vector3.Forward, Vector3.Up);
            solidEffect.WorldViewProj.SetValue(worldMatrix * viewProj);
            solidEffect.Apply(worldMatrix * viewMatrix);
            if (player.blockFound) {
                DrawCubeFrame();
            }

            //FPS counter and other UI
            _spriteBatch.Begin();
            if (!IsPaused) {
                _spriteBatch.DrawString(_spriteFont, $"FPS:{UserInterface.frameCounter.GetFPS(gameTime.ElapsedGameTime.TotalSeconds)}", new(1, 3), Color.Black);
                _spriteBatch.DrawString(_spriteFont, $"X:{Math.Round((double)player.camPosition.X + (long)player.CurrentChunk.x * ChunkSize, 2)}", new(1, 23), Color.Black);
                _spriteBatch.DrawString(_spriteFont, $"Y:{Math.Round(player.camPosition.Y + player.CurrentChunk.y * ChunkSize - 1.7f, 2)}", new(1, 43), Color.Black);
                _spriteBatch.DrawString(_spriteFont, $"Z:{Math.Round((double)player.camPosition.Z + (long)player.CurrentChunk.z * ChunkSize, 2)}", new(1, 63), Color.Black);
                _spriteBatch.DrawString(_spriteFont, "+", new Vector2(WindowCenter.X, WindowCenter.Y) - (_spriteFont.MeasureString("+") / 2), Color.Black);
                _spriteBatch.Draw(solidEffect.Texture.GetValueTexture2D(), new Rectangle((int)(GraphicsDevice.Viewport.Width * 0.885f), (int)(GraphicsDevice.Viewport.Height * 0.82f), (int)(GraphicsDevice.Viewport.Width * 0.09f), (int)(GraphicsDevice.Viewport.Height * 0.16f)), new Rectangle(225, 241, 15, 15), Color.White);
            }
            _spriteBatch.End();

            // Sprite batch resets some settings so they need to be set again
            GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

            if (!IsPaused) {
                blockPreviewEffect.Parameters["WorldViewProj"].SetValue(previewMatrix);
                blockPreviewEffect.Parameters["FogVector"].SetValue(Vector4.Zero);
                blockPreviewEffect.CurrentTechnique.Passes[0].Apply();
                DrawCubePreview();
            }
            else {
                UserInterface._desktop.Render();
            }

            base.Draw(gameTime);
        }
    }
}