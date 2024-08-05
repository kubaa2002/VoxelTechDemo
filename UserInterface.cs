using System.IO;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Myra;
using Myra.Graphics2D.UI;

namespace VoxelTechDemo{
    static class UserInterface{
        static public Desktop _desktop;
        static public void Initialize(Game1 game, GraphicsDeviceManager _graphics){
            FontSystem ordinaryFontSystem = new FontSystem();
            ordinaryFontSystem.AddFont(File.ReadAllBytes("Content/PublicPixel.ttf"));
            MyraEnvironment.Game = game;

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
                Value = game.RenderDistance,
                Integer = true,
                Minimum = 1,
                Maximum = 32,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(spinButton, 0);
            Grid.SetRow(spinButton, 1);
            spinButton.ValueChanged += (s, a) =>{
                game.RenderDistance = (byte)spinButton.Value;
                game.CheckChunks();
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
                game.IsFixedTimeStep = !game.IsFixedTimeStep;
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
                game.Exit();
            };
            grid.Widgets.Add(button);

            // Add it to the desktop
            _desktop = new Desktop{
                Root = grid
            };
        }
    }
}