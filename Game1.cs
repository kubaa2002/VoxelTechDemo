using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static VoxelTechDemo.VoxelRenderer;
using Myra;
using Myra.Graphics2D.UI;
using FontStashSharp;
using System.IO;

namespace VoxelTechDemo
{
    public class Game1 : Game
    {   
        private Desktop _desktop;  
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _spriteFont;
        Matrix projectionMatrix, worldMatrix, viewMatrix, blockIconProjection, blockIconView = Matrix.CreateLookAt(new Vector3(3, 2, 3), new Vector3(0.5f,0.5f,0.5f), Vector3.Up);
        AlphaTestEffect basicEffect;
        readonly FrameCounter _frameCounter = new();
        readonly World world = new(12345);//DateTime.UtcNow.ToBinary());
        Point WindowCenter;
        float yaw=MathHelper.PiOver2, pitch, rotationSpeed = 0.005f;
        MouseState currentMouseState;
        bool LeftButtonPressed = false, RightButtonPressed = false, IsPaused = false, IsEscPressed = false, IsNoClipOn = false, IsNPressed = false;
        byte chosenBlock = 1;
        int ScrollWheelValue;
        Player player;
        byte RenderDistance = 3;
        readonly Dictionary<(int x,int z), Chunk> CurrentlyLoadedChunkLines = new(); 
        public Game1(){
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }
        protected override void Initialize(){
            //Fullscrean setup
            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.IsFullScreen = true;

            _graphics.ApplyChanges();
            InitializeVoxelRenderer(GraphicsDevice);

            for(int x=-(RenderDistance+1);x<=RenderDistance+1;x++){
                for(int z=-(RenderDistance+1);z<=RenderDistance+1;z++){
                    world.GenerateChunkLine(x,z);
                }
            }
            for(int x=-RenderDistance;x<=RenderDistance;x++){
                for(int z=-RenderDistance;z<=RenderDistance;z++){
                    for(int y=0;y<8;y++){
                        CurrentlyLoadedChunkLines[(x,z)]=world.WorldMap[(x,0,z)];
                        GenerateVertexVerticesAsync(world.WorldMap[(x,y,z)]);
                    }
                }
            }

            player = new Player(world);

            //Camera setup
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45f),GraphicsDevice.DisplayMode.AspectRatio,0.1f, 10000f);
            worldMatrix = Matrix.CreateWorld(player.camTarget, Vector3.Forward, Vector3.Up);
            viewMatrix = Matrix.CreateLookAt(player.camPosition, player.camTarget, Vector3.Up);
            WindowCenter = new Point(GraphicsDevice.Viewport.Width/2,GraphicsDevice.Viewport.Height/2);
            blockIconProjection = BlockIcon((int)(GraphicsDevice.Viewport.Width*0.93f),(int)(GraphicsDevice.Viewport.Height*0.9f),5);

            //Basic shader setup
            basicEffect = new AlphaTestEffect(GraphicsDevice){
                Alpha = 1f,
                Texture = Content.Load<Texture2D>("Textures"),
                World = worldMatrix
            };

            base.Initialize();
        }
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _spriteFont = Content.Load<SpriteFont>("PublicPixel");
            byte[] ttfData = File.ReadAllBytes("Content/PublicPixel.ttf");
            FontSystem ordinaryFontSystem = new FontSystem();
            ordinaryFontSystem.AddFont(ttfData);
            MyraEnvironment.Game = this;

            Grid grid = new(){
                RowSpacing = 8,
                ColumnSpacing = 8
            };

            grid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
            grid.RowsProportions.Add(new Proportion(ProportionType.Fill));

            // Render distance option
            Label textBox = new(){
                Text = "Render Distance:",
                Width = 320,
                Height = 60,
                Font = ordinaryFontSystem.GetFont(32)
            };
            Grid.SetColumn(textBox, 0);
            Grid.SetRow(textBox, 1);
            grid.Widgets.Add(textBox);

            SpinButton spinButton = new(){
                Width = 100,
                Nullable = false,
                Value = RenderDistance,
                Integer = true,
                Minimum = 1,
                Maximum = 32,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(spinButton, 0);
            Grid.SetRow(spinButton, 1);
            spinButton.ValueChanged += (s, a) =>{
                RenderDistance = (byte)spinButton.Value;
                CheckChunks();
            };
            grid.Widgets.Add(spinButton);

            // Unlock framerate option
            Label framerate = new(){
                Text = "Unlock framerate:",
                Width = 320,
                Height = 60,
                Font = ordinaryFontSystem.GetFont(32)
            };
            Grid.SetColumn(framerate, 0);
            Grid.SetRow(framerate, 2);
            grid.Widgets.Add(framerate);

            CheckButton checkBox = new(){
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(checkBox, 0);
            Grid.SetRow(checkBox, 2);
            checkBox.Click += (s, a) =>{
                //Unlockin Frame rate
                _graphics.SynchronizeWithVerticalRetrace = !_graphics.SynchronizeWithVerticalRetrace;
                IsFixedTimeStep = !IsFixedTimeStep;
                _graphics.ApplyChanges();
            };
            grid.Widgets.Add(checkBox);

            // Exit Button
            Button button = new(){
                Content = new Label{
                    Text = "Exit",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Font = ordinaryFontSystem.GetFont(64)
                },
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 270,
                Height = 80
            };
            Grid.SetColumn(button, 0);
            Grid.SetRow(button, 3);
            button.Click += (s, a) =>{
                Exit();
            };
            grid.Widgets.Add(button);

            // Add it to the desktop
            _desktop = new Desktop{
                Root = grid
            };
        }
        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.Escape) && !IsEscPressed){
                IsPaused = !IsPaused;
                IsEscPressed = true;
                IsMouseVisible = !IsMouseVisible;
                Mouse.SetPosition(WindowCenter.X,WindowCenter.Y);
            }   
            if(keyboardState.IsKeyUp(Keys.Escape)){
                IsEscPressed = false;
            }
            if(!IsPaused){
                //Camera look
                //TOFIX when looking at block at high distances for example 1000000f float point precision loss will cause jittering and eventually camera stops working at all
                currentMouseState = Mouse.GetState();
                yaw -= (currentMouseState.X - WindowCenter.X)*rotationSpeed;
                pitch -= (currentMouseState.Y - WindowCenter.Y)*rotationSpeed;
                pitch = MathHelper.Clamp(pitch, -1.57f,1.57f);//-MathHelper.PiOver2, MathHelper.PiOver2
                player.right = Vector3.Transform(Vector3.Right, Matrix.CreateFromYawPitchRoll(yaw, pitch, 0f));

                //Check looked at block
                player.GetLookedAtBlock();

                if(player.ChunkChanged){
                    CheckChunks();
                }

                //Check pressed keys
                if(keyboardState.IsKeyUp(Keys.N)){
                    IsNPressed = false;
                }
                if(keyboardState.IsKeyDown(Keys.N) && IsNPressed == false){
                    IsNoClipOn = !IsNoClipOn;
                    IsNPressed = true;
                    player.ResetPlayerSpeed();
                    player.playerHitBox = new(new Vector3(player.camPosition.X-0.2499f,player.camPosition.Y-1.6999f,player.camPosition.Z-0.2499f),new Vector3(player.camPosition.X+0.2499f,player.camPosition.Y+0.0999f,player.camPosition.Z+0.2499f));
                }
                if(IsNoClipOn){
                    player.NoClipMovement(keyboardState,gameTime);
                }
                else{
                    player.NormalMovement(keyboardState, gameTime, yaw);
                }
                if(currentMouseState.LeftButton == ButtonState.Pressed && LeftButtonPressed == false && player.blockFound == true){
                    world.SetBlock(player.LookedAtBlock.x,player.LookedAtBlock.y,player.LookedAtBlock.z,player.CurrentChunk,0);
                    LeftButtonPressed = true;
                }
                if(currentMouseState.LeftButton == ButtonState.Released){
                    LeftButtonPressed = false;
                }
                if(currentMouseState.RightButton == ButtonState.Pressed && RightButtonPressed == false && player.blockFound == true){
                    world.SetBlock(player.LookedAtBlock.x,player.LookedAtBlock.y,player.LookedAtBlock.z,player.CurrentChunk,chosenBlock,player.currentSide, player.playerHitBox);
                    RightButtonPressed = true;
                }
                if(currentMouseState.RightButton == ButtonState.Released){
                    RightButtonPressed = false;
                }
                if(currentMouseState.ScrollWheelValue>ScrollWheelValue){
                    chosenBlock++;
                    if(chosenBlock==15){
                        chosenBlock = 1;
                    }
                }
                if(currentMouseState.ScrollWheelValue<ScrollWheelValue){
                    chosenBlock--;
                    if(chosenBlock==0){
                        chosenBlock=14;
                    }
                }
                player.forward = Vector3.Transform(Vector3.Forward, Matrix.CreateFromYawPitchRoll(yaw, pitch, 0f));
                ScrollWheelValue = currentMouseState.ScrollWheelValue;
                Mouse.SetPosition(WindowCenter.X,WindowCenter.Y);
            }
            player.camTarget = player.camPosition + player.forward;
            viewMatrix = Matrix.CreateLookAt(player.camPosition, player.camTarget, Vector3.Up);
            base.Update(gameTime);
        }
        void CheckChunks(){
            player.ChunkChanged = false;
            for(int x=-RenderDistance;x<=RenderDistance;x++){
                for(int z=-RenderDistance;z<=RenderDistance;z++){
                    if(x*x+z*z<=(RenderDistance+0.5)*(RenderDistance+0.5)){
                        if(!CurrentlyLoadedChunkLines.ContainsKey((player.CurrentChunk.x+x,player.CurrentChunk.z+z))){
                            LoadChunkLine(player.CurrentChunk.x+x,player.CurrentChunk.z+z);
                            CurrentlyLoadedChunkLines[(player.CurrentChunk.x+x,player.CurrentChunk.z+z)]=world.WorldMap[(player.CurrentChunk.x+x,0,player.CurrentChunk.z+z)];
                        }
                    }
                }
            }
            List<(int x,int z)> ChunkForUnload = new();
            foreach(KeyValuePair<(int x,int z),Chunk> pair in CurrentlyLoadedChunkLines){
                if((pair.Key.x-player.CurrentChunk.x)*(pair.Key.x-player.CurrentChunk.x)+(pair.Key.z-player.CurrentChunk.z)*(pair.Key.z-player.CurrentChunk.z)>(RenderDistance+1.5)*(RenderDistance+1.5)){
                    ChunkForUnload.Add((pair.Key.x,pair.Key.z));
                }
            }
            for(int i=0;i<ChunkForUnload.Count;i++){
                UnloadChunkLine(ChunkForUnload[i].x,ChunkForUnload[i].z);
            }
        }
        Task LoadChunkLine(int x,int z){        
            if(!world.WorldMap.ContainsKey((x,0,z))){
                world.GenerateChunkLine(x,z);
            }    
            return Task.Run(()=>{
                if(!world.WorldMap.ContainsKey((x+1,0,z))){
                    world.GenerateChunkLine(x+1,z);
                }
                if(!world.WorldMap.ContainsKey((x,0,z+1))){
                    world.GenerateChunkLine(x,z+1);
                }
                if(!world.WorldMap.ContainsKey((x-1,0,z))){
                    world.GenerateChunkLine(x-1,z);
                }
                if(!world.WorldMap.ContainsKey((x,0,z-1))){
                    world.GenerateChunkLine(x,z-1);
                }
                for(int y=0;y<8;y++){
                    GenerateVertexVertices(world.WorldMap[(x,y,z)]);
                }
            });
        }
        void UnloadChunkLine(int x,int z){
            for(int y=0;y<8;y++){
                world.WorldMap[(x,y,z)].vertexBufferOpaque?.Dispose();
                world.WorldMap[(x,y,z)].vertexBufferTransparent?.Dispose();
                CurrentlyLoadedChunkLines.Remove((x,z));
            }
        }
        protected override void Draw(GameTime gameTime)
        {           
            basicEffect.Projection = projectionMatrix;
            basicEffect.View = viewMatrix;
            basicEffect.CurrentTechnique.Passes[0].Apply();

            GraphicsDevice.Clear(Color.CornflowerBlue);
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.AlphaBlend;

            //TODO: Make it so it doesn't recalculate world matrixes every frame
            for(int x=-RenderDistance;x<=RenderDistance;x++){
                for(int z=-RenderDistance;z<=RenderDistance;z++){
                    for(int y=-RenderDistance;y<=RenderDistance;y++){
                        if(world.WorldMap.ContainsKey((x+player.CurrentChunk.x,y+player.CurrentChunk.y,z+player.CurrentChunk.z)) && CurrentlyLoadedChunkLines.ContainsKey((x+player.CurrentChunk.x,z+player.CurrentChunk.z))){
                            basicEffect.World = Matrix.CreateWorld(new Vector3(x*64,y*64,z*64),Vector3.Forward,Vector3.Up);
                            basicEffect.CurrentTechnique.Passes[0].Apply();
                            DrawChunkOpaque(world.WorldMap[(x+player.CurrentChunk.x,y+player.CurrentChunk.y,z+player.CurrentChunk.z)]);
                        }
                    }
                }
            }

            //Opengl on windows doesn't like when transparent meshes are mixed with opaque ones
            for(int x=-RenderDistance;x<=RenderDistance;x++){
                for(int z=-RenderDistance;z<=RenderDistance;z++){
                    for(int y=-RenderDistance;y<=RenderDistance;y++){
                        if(world.WorldMap.ContainsKey((x+player.CurrentChunk.x,y+player.CurrentChunk.y,z+player.CurrentChunk.z)) && CurrentlyLoadedChunkLines.ContainsKey((x+player.CurrentChunk.x,z+player.CurrentChunk.z))){
                            basicEffect.World = Matrix.CreateWorld(new Vector3(x*64,y*64,z*64),Vector3.Forward,Vector3.Up);
                            basicEffect.CurrentTechnique.Passes[0].Apply();
                            DrawChunkTransparent(world.WorldMap[(x+player.CurrentChunk.x,y+player.CurrentChunk.y,z+player.CurrentChunk.z)]);
                        }
                    }
                }
            }

            basicEffect.World = Matrix.CreateWorld(new Vector3(player.LookedAtBlock.x,player.LookedAtBlock.y,player.LookedAtBlock.z),Vector3.Forward,Vector3.Up);
            basicEffect.CurrentTechnique.Passes[0].Apply();
            if(player.blockFound == true){
                DrawCubeFrame();
            }     

            //FPS counter and other UI
            _frameCounter.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            _spriteBatch.Begin();
            if(player.IsUnderWater){
                _spriteBatch.Draw(basicEffect.Texture,new Rectangle(0,0,GraphicsDevice.Viewport.Width,GraphicsDevice.Viewport.Height),new Rectangle(33,49,14,14),Color.White);
            }
            _spriteBatch.DrawString(_spriteFont, string.Format("FPS: {0}", _frameCounter.AverageFramesPerSecond), new(1,3), Color.Black);
            _spriteBatch.DrawString(_spriteFont,"X:"+Math.Round((double)player.camPosition.X+(long)player.CurrentChunk.x*64,2).ToString(),new(1,23),Color.Black);
            _spriteBatch.DrawString(_spriteFont,"Y:"+Math.Round(player.playerHitBox.Min.Y+player.CurrentChunk.y*64,2).ToString(),new(1,43),Color.Black);
            _spriteBatch.DrawString(_spriteFont,"Z:"+Math.Round((double)player.camPosition.Z+(long)player.CurrentChunk.z*64,2).ToString(),new(1,63),Color.Black);
            if(!IsPaused){
                _spriteBatch.DrawString(_spriteFont,"+",new Vector2(WindowCenter.X,WindowCenter.Y) - (_spriteFont.MeasureString("+")/2),Color.Black);
            }
            _spriteBatch.Draw(basicEffect.Texture,new Rectangle((int)(GraphicsDevice.Viewport.Width*0.885f),(int)(GraphicsDevice.Viewport.Height*0.82f),(int)(GraphicsDevice.Viewport.Width*0.09f),(int)(GraphicsDevice.Viewport.Height*0.16f)),new Rectangle(225,241,15,15),Color.White);
            _spriteBatch.End();

            basicEffect.Projection = blockIconProjection;
            basicEffect.View = blockIconView;
            basicEffect.CurrentTechnique.Passes[0].Apply();
            GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

            basicEffect.World = Matrix.CreateWorld(Vector3.Zero,Vector3.Forward,Vector3.Up);
            basicEffect.CurrentTechnique.Passes[0].Apply();
            DrawCubePreview(chosenBlock);

            if(IsPaused){
                _desktop.Render();
            }

            base.Draw(gameTime);
        }
    }
}