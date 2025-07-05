using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static VoxelTechDemo.UserSettings;
using static VoxelTechDemo.VoxelRenderer;

namespace VoxelTechDemo{
    public class CustomEffect : Effect{
        public readonly EffectParameter Texture;
        protected readonly EffectParameter DiffuseColor;
        readonly EffectParameter FogColor;
        readonly EffectParameter FogVector;
        public readonly EffectParameter WorldViewProj;

        public float FogStart;
        public float FogEnd{
            private get{
                return fogValue;
            }
            set{
                fogValue = 1.0f / (FogStart - value);
            }
        }
        float fogValue;
        
        public CustomEffect(Effect clone, Game game):base(clone){
            Texture = Parameters["Texture"];
            DiffuseColor = Parameters["DiffuseColor"];
            FogColor = Parameters["FogColor"];
            FogVector = Parameters["FogVector"];
            WorldViewProj = Parameters["WorldViewProj"];

            Texture.SetValue(game.Content.Load<Texture2D>("Textures"));
            DiffuseColor.SetValue(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            FogStart = RenderDistance*0.6f*ChunkSize;
            FogEnd = RenderDistance*0.8f*ChunkSize;
            FogColor.SetValue(new Vector3(
                //Cornflower Blue
                100f / 255f,  // Red
                149f / 255f,  // Green
                237f / 255f   // Blue
            ));
        }
        public void Apply(Matrix worldView){
            if(FogEnabled){
                FogVector.SetValue(new Vector4(worldView.M13,worldView.M23,worldView.M33,worldView.M43+FogStart)*fogValue);
            }
            else{
                FogVector.SetValue(Vector4.Zero);
            }
            CurrentTechnique.Passes[0].Apply();
        }
        public void ApplyUnderWaterSettings(){
            FogColor.SetValue(new Vector3(0.3f,0.3f,0.7f));
            FogStart = -RenderDistance*0.2f*ChunkSize;
            FogEnd = RenderDistance*0.2f*ChunkSize;
        }
        public void ApplyNormalSettings(){
            FogColor.SetValue(new Vector3(100f/255f,149f/255f,237f/255f));
            FogStart = RenderDistance*0.6f*ChunkSize;
            FogEnd = RenderDistance*0.8f*ChunkSize;
        }
    }
    public class FluidEffect : CustomEffect{
        readonly EffectParameter AnimationFrame;

        int counter = 0;
        TimeSpan timer = new();

        public FluidEffect(Effect clone, Game game):base(clone, game){
            // Animation Frame can be from 0 to 15
            AnimationFrame = Parameters["AnimationFrame"];

            Texture.SetValue(game.Content.Load<Texture2D>("WaterTexture"));
            DiffuseColor.SetValue(new Vector3(0.7f,0.7f,1.4f));
        }
        public void UpdateAnimationFrame(TimeSpan elapsedTime){
            timer += elapsedTime;
            if(timer.Ticks>TimeSpan.TicksPerSecond/10){
                counter++;
                timer = new TimeSpan(timer.Ticks-TimeSpan.TicksPerSecond/10);
                if(counter>15){
                    counter = 0;
                }
                AnimationFrame.SetValue(counter);
            }
        }
    }
}