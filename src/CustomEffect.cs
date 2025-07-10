using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static VoxelTechDemo.UserSettings;
using static VoxelTechDemo.VoxelRenderer;

namespace VoxelTechDemo{
    public class CustomEffect : Effect{
        public readonly EffectParameter Texture;
        public readonly EffectParameter DiffuseColor;
        readonly EffectParameter FogColor;
        readonly EffectParameter FogVector;
        public readonly EffectParameter WorldViewProj;
        public readonly EffectParameter AnimationFrame;

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
            AnimationFrame = Parameters["AnimationFrame"];

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
        public void UpdateAnimationFrame(TimeSpan totalTime) {
            AnimationFrame.SetValue((float)Math.Round(totalTime.TotalSeconds * 8 % 15) / 16);
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
}