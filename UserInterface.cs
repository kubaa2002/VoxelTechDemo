using System.IO;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Myra;
using Myra.Graphics2D.UI;
using static VoxelTechDemo.VoxelRenderer;
using static VoxelTechDemo.UserSettings;

namespace VoxelTechDemo{
    static class UserInterface{
        static public Desktop _desktop;
        static public void Initialize(Game1 game, GraphicsDeviceManager _graphics,CustomEffect effect){
            FontSystem ordinaryFontSystem = new();
            ordinaryFontSystem.AddFont(File.ReadAllBytes("Content/PublicPixel.ttf"));
            MyraEnvironment.Game = game;

            VerticalStackPanel mainPanel = new(){
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            Grid grid = new(){
                RowSpacing = 8,
                ColumnSpacing = 8,
                VerticalAlignment = VerticalAlignment.Stretch,
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
            spinButton.ValueChanged += (s, a) =>{
                RenderDistance = (byte)spinButton.Value;
                game.CheckChunks();
                effect.FogStart = RenderDistance*0.6f*ChunkSize;
                effect.FogEnd = RenderDistance*0.8f*ChunkSize;
            };
            Grid.SetColumn(spinButton, 1);
            Grid.SetRow(spinButton, 1);
            grid.Widgets.Add(spinButton);

            // Unlock framerate button
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
                HorizontalAlignment = HorizontalAlignment.Center,
                IsChecked = FrameRateUnlocked
            };
            checkBox.Click += (s, a) =>{
                //Unlockin Frame rate
                _graphics.SynchronizeWithVerticalRetrace = !_graphics.SynchronizeWithVerticalRetrace;
                game.IsFixedTimeStep = !game.IsFixedTimeStep;
                _graphics.ApplyChanges();
                FrameRateUnlocked = !FrameRateUnlocked;
            };
            Grid.SetColumn(checkBox, 1);
            Grid.SetRow(checkBox, 2);
            grid.Widgets.Add(checkBox);

            // Fog button
            Label fog = new(){
                Text = "Fog enabled:",
                Width = 320,
                Height = 60,
                Font = ordinaryFontSystem.GetFont(32)
            };
            Grid.SetColumn(fog, 0);
            Grid.SetRow(fog, 3);
            grid.Widgets.Add(fog);

            CheckButton fogCheck = new(){
                HorizontalAlignment = HorizontalAlignment.Center,
                IsChecked = FogEnabled
            };
            fogCheck.Click += (s, a) =>{
                FogEnabled = !FogEnabled;
            };
            Grid.SetColumn(fogCheck, 1);
            Grid.SetRow(fogCheck, 3);
            grid.Widgets.Add(fogCheck);

            // Field of view slider
            Label FOV = new(){
                Text = "Field of view:",
                Width = 320,
                Height = 60,
                Font = ordinaryFontSystem.GetFont(32)
            };
            Grid.SetColumn(FOV,0);
            Grid.SetRow(FOV,4);
            grid.Widgets.Add(FOV);

            HorizontalSlider FOVslider = new(){
                Minimum = 30,
                Maximum = 120,
                Value = FieldOfView
            };
            FOVslider.ValueChanged += (s, a) =>{
                FieldOfView = FOVslider.Value;
                game.projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(FieldOfView),game.GraphicsDevice.DisplayMode.AspectRatio,0.1f, 10000f);
            };
            Grid.SetColumn(FOVslider,1);
            Grid.SetRow(FOVslider,4);
            grid.Widgets.Add(FOVslider);

            // Mouse sensitivity slider
            Label Mouse = new(){
                Text = "Mouse sensitivity:",
                Width = 320,
                Height = 60,
                Font = ordinaryFontSystem.GetFont(32)
            };
            Grid.SetColumn(Mouse,0);
            Grid.SetRow(Mouse,5);
            grid.Widgets.Add(Mouse);

            HorizontalSlider MouseSlider = new(){
                Minimum = 0.001f,
                Maximum = 0.01f,
                Value = MouseSensitivity
            };
            MouseSlider.ValueChanged += (s, a) =>{
                MouseSensitivity = MouseSlider.Value;
            };
            Grid.SetColumn(MouseSlider,1);
            Grid.SetRow(MouseSlider,5);
            grid.Widgets.Add(MouseSlider);
            mainPanel.Widgets.Add(grid);

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
            button.Click += (s, a) =>{
                CheckSettingsFile();
                game.Exit();
            };
            mainPanel.Widgets.Add(button);

            // Add it to the desktop
            _desktop = new Desktop{
                Root = mainPanel
            };
        }
    }
}