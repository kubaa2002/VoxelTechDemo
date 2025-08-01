using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static VoxelTechDemo.VoxelRenderer;
using static VoxelTechDemo.UserSettings;

using var game = new VoxelTechDemo.Game1();
game.Run();

namespace VoxelTechDemo {
    public class Game1 : Game {
        private readonly GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private SpriteFont font;
        private float yaw = MathHelper.PiOver2, pitch;
        private bool IsPaused = false, IsNoClipOn = false;
        private KeyboardState lastKeyboardState;
        private MouseState lastMouseState;
        private byte chosenBlock = 1;
        private Point WindowCenter;
        public CustomEffect effect;

        private Texture2D blankTexture;

        public readonly World world = new(12345);
        public Player player;

        public Game1() {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            // Setting window size
            graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

            // Loading user settings from a file
            LoadSettings();
            if (FrameRateUnlocked) {
                graphics.SynchronizeWithVerticalRetrace = !graphics.SynchronizeWithVerticalRetrace;
                IsFixedTimeStep = !IsFixedTimeStep;
            }
            if (Fullscreen) {
                graphics.IsFullScreen = true;
            }
        }
        protected override void Initialize() {
            InitializeVoxelRenderer(GraphicsDevice);
            ChangeCubePreview(chosenBlock);
            UserInterface.Initialize(this, graphics);
            Directory.CreateDirectory("Save");

            // Creating player and making sure that spawn terrain is generated
            player = new Player(world);
            world.GenerateChunkLine(player.CurrentChunk.x, player.CurrentChunk.z);
            world.UpdateLoadedChunks(player.CurrentChunk.x, player.CurrentChunk.z);

            WindowCenter = new Point(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);

            base.Initialize();
        }
        protected override void LoadContent() {
            byte[] bytes = File.ReadAllBytes("Content/CustomEffect.mgfx");
            effect = new(new Effect(GraphicsDevice, bytes), this);
            spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("PublicPixel");
            blankTexture = new(GraphicsDevice, 1, 1);
            blankTexture.SetData([new Color(0, 0, 0, 128)]);
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
                if (currentMouseState.ScrollWheelValue != lastMouseState.ScrollWheelValue) {
                    chosenBlock = (byte)(((currentMouseState.ScrollWheelValue / 120 % 18 + 18) % 18) + 1);
                    ChangeCubePreview(chosenBlock);
                }
                lastMouseState = currentMouseState;
                Mouse.SetPosition(WindowCenter.X, WindowCenter.Y);
                effect.UpdateViewMatrix(player);
            }
            lastKeyboardState = keyboardState;
            base.Update(gameTime);
        }
        protected override void Draw(GameTime gameTime) {
            if (player.IsUnderWater) {
                effect.ApplyUnderWaterSettings();
            }
            else {
                effect.ApplyNormalSettings();
            }

            BoundingFrustum frustum = new(effect.viewProj);

            // Render solid blocks
            DrawTerrain(frustum, true);

            // Apply water animation settings
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            effect.UpdateAnimationFrame(gameTime.TotalGameTime);

            // Render fluids
            DrawTerrain(frustum, false);

            // Return to normal settings
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            effect.AnimationFrame.SetValue(0);

            if (player.blockFound) {
                DrawCubeFrame(effect, player.LookedAtBlock);
            }

            //FPS counter and other UI
            if (!IsPaused) {
                spriteBatch.Begin();
                spriteBatch.DrawString(font, $"FPS:{UserInterface.frameCounter.GetFPS(gameTime.ElapsedGameTime.TotalSeconds)}", new(1, 3), Color.Black);
                spriteBatch.DrawString(font, $"X:{Math.Round((double)player.camPosition.X + (long)player.CurrentChunk.x * ChunkSize, 2)}", new(1, 23), Color.Black);
                spriteBatch.DrawString(font, $"Y:{Math.Round(player.camPosition.Y + player.CurrentChunk.y * ChunkSize - 1.7f, 2)}", new(1, 43), Color.Black);
                spriteBatch.DrawString(font, $"Z:{Math.Round((double)player.camPosition.Z + (long)player.CurrentChunk.z * ChunkSize, 2)}", new(1, 63), Color.Black);
                if (world.WorldMap.TryGetValue(player.CurrentChunk, out Chunk chunk)) {
                    ushort value = chunk.blockLightValues[((int)player.camPosition.X) + (((int)player.camPosition.Y) * ChunkSize) + ((int)player.camPosition.Z * ChunkSizeSquared)];
                    spriteBatch.DrawString(font, $"Red level:{value & Light.lightMask}", new(1, 83), Color.Black);
                    spriteBatch.DrawString(font, $"Green level:{(value >> Light.GreenLight) & Light.lightMask}", new(1, 103), Color.Black);
                    spriteBatch.DrawString(font, $"Blue level:{(value >> Light.BlueLight) & Light.lightMask}", new(1, 123), Color.Black);
                    spriteBatch.DrawString(font, $"Sky level:{value >> Light.SkyLight}", new(1, 143), Color.Black);
                }
                spriteBatch.DrawString(font, "+", new Vector2(WindowCenter.X, WindowCenter.Y) - (font.MeasureString("+") / 2), Color.Black);
                spriteBatch.Draw(blankTexture, new Rectangle((int)(GraphicsDevice.Viewport.Width * 0.885f), (int)(GraphicsDevice.Viewport.Height * 0.82f), (int)(GraphicsDevice.Viewport.Width * 0.09f), (int)(GraphicsDevice.Viewport.Height * 0.16f)), Color.White);
                spriteBatch.End();

                DrawCubePreview(effect);
            }
            else {
                UserInterface._desktop.Render();
            }

            base.Draw(gameTime);
        }
        private void DrawTerrain(BoundingFrustum frustum, bool opaque) {
            //TODO: Make it so it doesn't recalculate world matrices every frame
            int chunkX = player.CurrentChunk.x;
            int chunkY = player.CurrentChunk.y;
            int chunkZ = player.CurrentChunk.z;
            Vector3 chunkLineSize = new(ChunkSize, World.MaxHeight, ChunkSize);
            foreach ((int x, int z) in world.CurrentlyLoadedChunkLines) {
                Vector3 currentChunkCoords = new(x - chunkX, -chunkY, z - chunkZ);
                currentChunkCoords *= ChunkSize;
                if (frustum.Intersects(new BoundingBox(currentChunkCoords, currentChunkCoords + chunkLineSize))) {
                    effect.Apply(Matrix.CreateWorld(currentChunkCoords, Vector3.Forward, Vector3.Up));
                    for (int y = 0; y < World.MaxYChunk; y++) {
                        if (world.WorldMap.TryGetValue((x, y, z), out Chunk chunk)) {
                            DrawChunk(opaque ? chunk.vertexBufferOpaque : chunk.vertexBufferTransparent);
                        }
                    }
                }
            }
        }
        public new void Exit() {
            foreach ((int x, int z) in world.CurrentlyLoadedChunkLines) {
                SaveFile.SaveChunkLine(world, x, z);
            }

            base.Exit();
        }
    }
}