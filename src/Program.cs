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
        private bool IsPaused, IsNoClipOn;
        private KeyboardState lastKeyboardState;
        private MouseState lastMouseState;
        private byte chosenBlock = 1;
        private Point WindowCenter;
        private Texture2D blankTexture;
        
        public CustomEffect effect;
        public BasicEffect basicEffect;
        public CloudEffect cloudEffect;
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
            effect = new(this);
            cloudEffect = new CloudEffect(this);
            ChangeCubePreview(chosenBlock);
            UserInterface.Initialize(this, graphics);
            Directory.CreateDirectory("Save");
            basicEffect = new BasicEffect(GraphicsDevice) {
                VertexColorEnabled = true,
                FogEnabled = FogEnabled,
                FogStart = RenderDistance * 0.6f * ChunkSize,
                FogEnd = RenderDistance * 1f * ChunkSize,
                FogColor = Color.CornflowerBlue.ToVector3(),
                Projection = effect.projectionMatrix,
            };

            // Creating player and making sure that spawn terrain is generated
            player = new Player(world);
            world.GenerateChunkLine(player.CurrentChunk.x, player.CurrentChunk.z);
            world.UpdateLoadedChunks(player.CurrentChunk.x, player.CurrentChunk.z);

            WindowCenter = new Point(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);

            base.Initialize();
        }
        protected override void LoadContent() {
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
                    if (IsNoClipOn) {
                        player.playerHitBox = new BoundingBox(Vector3.Zero, Vector3.Zero);
                    }
                    else {
                        player.ResetHitBox();
                    }
                }
                if (IsNoClipOn) {
                    player.NoClipMovement(keyboardState, (float)gameTime.ElapsedGameTime.TotalSeconds);
                }
                else {
                    player.NormalMovement(keyboardState, (float)gameTime.ElapsedGameTime.TotalSeconds);
                }

                //Check looked at block
                player.forward = Vector3.Transform(Vector3.Forward, Matrix.CreateFromYawPitchRoll(yaw, pitch, 0f));
                player.GetLookedAtBlock();

                if (currentMouseState.LeftButton == ButtonState.Pressed && lastMouseState.LeftButton == ButtonState.Released && player.BlockFound) {
                    world.SetBlock(player.LookedAtBlock, player.CurrentChunk, 0);
                }
                if (currentMouseState.RightButton == ButtonState.Pressed && lastMouseState.RightButton == ButtonState.Released && player.BlockFound) {
                    world.SetBlock(player.LookedAtBlock, player.CurrentChunk, chosenBlock, player.currentSide, player.playerHitBox);
                }
                if (currentMouseState.ScrollWheelValue != lastMouseState.ScrollWheelValue) {
                    chosenBlock = (byte)(((currentMouseState.ScrollWheelValue / 120 % Blocks.NumOfBlocks + Blocks.NumOfBlocks) % Blocks.NumOfBlocks) + 1);
                    ChangeCubePreview(chosenBlock);
                }
                lastMouseState = currentMouseState;
                Mouse.SetPosition(WindowCenter.X, WindowCenter.Y);
                effect.UpdateViewMatrix(player);
                basicEffect.View = effect.viewMatrix;
            }
            lastKeyboardState = keyboardState;
            base.Update(gameTime);
        }
        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            GraphicsDevice.Indices = indexBuffer;
            GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
            if (player.IsUnderWater) {
                effect.ApplyUnderWaterSettings();
            }
            else {
                effect.ApplyNormalSettings();
            }

            BoundingFrustum frustum = new(effect.viewProj);

            // Render solid blocks
            DrawTerrain(frustum, 0);

            // Draw foliage
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            DrawTerrain(frustum, 2);
            
            // Apply water animation settings
            effect.UpdateAnimationFrame(gameTime.TotalGameTime);

            // Render fluids
            DrawTerrain(frustum, 1);

            // Return to normal settings
            effect.AnimationFrame.SetValue(0);
            
            if (CloudsEnabled) {
                //basicEffect.World = Matrix.CreateWorld(new Vector3((cloudOffset.x-player.CurrentChunk.x)*ChunkSize,-player.CurrentChunk.y * ChunkSize,(cloudOffset.z-player.CurrentChunk.z)*ChunkSize), Vector3.Forward, Vector3.Up);
                //basicEffect.CurrentTechnique.Passes[0].Apply();
                cloudEffect.Apply(effect, Matrix.CreateWorld(new Vector3((cloudOffset.x-player.CurrentChunk.x)*ChunkSize,-player.CurrentChunk.y * ChunkSize,(cloudOffset.z-player.CurrentChunk.z)*ChunkSize), Vector3.Forward, Vector3.Up));
                UpdateAndDrawClouds(world, player.CurrentChunk.x, player.CurrentChunk.z, gameTime.TotalGameTime.TotalMinutes);
            }
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

            if (player.BlockFound) {
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

                // Some settings have to be reset because sprite batch resets it
                GraphicsDevice.Indices = indexBuffer;
                GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
                
                DrawCubePreview(effect);
            }
            else {
                UserInterface._desktop.Render();
            }

            base.Draw(gameTime);
        }
        private void DrawTerrain(BoundingFrustum frustum, int id) {
            //TODO: Make it so it doesn't recalculate world matrices every frame
            int chunkX = player.CurrentChunk.x;
            int chunkY = player.CurrentChunk.y;
            int chunkZ = player.CurrentChunk.z;
            Vector3 chunkLineSize = new(ChunkSize, World.MaxHeight, ChunkSize);
            foreach ((int x, int z) in world.CurrentlyLoadedChunkLines) {
                Vector3 currentChunkCoords = new(x - chunkX, -chunkY, z - chunkZ);
                currentChunkCoords *= ChunkSize;
                if (!frustum.Intersects(new BoundingBox(currentChunkCoords, currentChunkCoords + chunkLineSize))) {
                    continue;
                }
                effect.Apply(Matrix.CreateWorld(currentChunkCoords, Vector3.Forward, Vector3.Up));
                for (int y = 0; y < World.MaxYChunk; y++) {
                    if (world.WorldMap.TryGetValue((x, y, z), out Chunk chunk)) {
                        switch (id) {
                            case 0:
                                DrawChunk(chunk.vertexBufferOpaque);
                                break;
                            case 1:
                                DrawChunk(chunk.vertexBufferTransparent);
                                break;
                            case 2:
                                DrawChunk(chunk.vertexBufferFoliage);
                                break;
                            default:
                                return;
                        }
                    }
                }
            }
        }
        public new void Exit() {
            foreach ((int x, int z) in world.CurrentlyLoadedChunkLines) {
                SaveFile.SaveChunkLine(world, x, z);
            }
            SaveFile.SavePlayer(player);
            
            base.Exit();
        }
    }
}