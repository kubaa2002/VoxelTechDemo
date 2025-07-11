using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static VoxelTechDemo.UserSettings;
using static VoxelTechDemo.VoxelRenderer;

namespace VoxelTechDemo{
    public class CustomEffect : Effect{
        public readonly EffectParameter Texture;
        readonly EffectParameter FogColor;
        readonly EffectParameter FogVector;
        public readonly EffectParameter WorldViewProj;
        public readonly EffectParameter AnimationFrame;

        public Matrix projectionMatrix;
        private Matrix viewMatrix;
        public Matrix viewProj;
        private Matrix previewMatrix;

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
            FogColor = Parameters["FogColor"];
            FogVector = Parameters["FogVector"];
            WorldViewProj = Parameters["WorldViewProj"];
            AnimationFrame = Parameters["AnimationFrame"];

            Texture.SetValue(game.Content.Load<Texture2D>("Textures"));
            FogStart = RenderDistance*0.6f*ChunkSize;
            FogEnd = RenderDistance*0.8f*ChunkSize;
            FogColor.SetValue(new Vector3(
                //Cornflower Blue
                100f / 255f,  // Red
                149f / 255f,  // Green
                237f / 255f   // Blue
            ));

            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(FieldOfView), GraphicsDevice.DisplayMode.AspectRatio, 0.1f, 10000f);
            previewMatrix = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Up);
            previewMatrix *= Matrix.CreateLookAt(new Vector3(3, 2, 3), new Vector3(0.5f, 0.5f, 0.5f), Vector3.Up);
            previewMatrix *= CreateBlockPreviewProj((int)(GraphicsDevice.Viewport.Width * 0.93f), (int)(GraphicsDevice.Viewport.Height * 0.9f), 5);
        }
        public void UpdateViewMatrix(Player player) {
            viewMatrix = Matrix.CreateLookAt(player.camPosition, player.camPosition + player.forward, Vector3.Up);
            viewProj = viewMatrix * projectionMatrix;
        }
        public void DrawBlockPreview() {
            // Sprite batch resets some settings so they need to be set again
            GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
            WorldViewProj.SetValue(previewMatrix);
            FogVector.SetValue(Vector4.Zero);
            CurrentTechnique.Passes[0].Apply();
            
        }
        public void UpdateAnimationFrame(TimeSpan totalTime) {
            AnimationFrame.SetValue((float)Math.Round(totalTime.TotalSeconds * 8 % 15) / 16);
        }
        public void Apply(Matrix worldMatrix){
            WorldViewProj.SetValue(worldMatrix * viewProj);
            Matrix worldView = worldMatrix * viewMatrix;
            if (FogEnabled){
                FogVector.SetValue(new Vector4(worldView.M13,worldView.M23,worldView.M33,worldView.M43+FogStart)*fogValue);
            }
            else{
                FogVector.SetValue(Vector4.Zero);
            }
            CurrentTechnique.Passes[0].Apply();
        }
        public void ApplyUnderWaterSettings(){
            GraphicsDevice.Clear(new Color(new Vector3(0.3f, 0.3f, 0.7f)));
            FogColor.SetValue(new Vector3(0.3f,0.3f,0.7f));
            FogStart = -RenderDistance*0.2f*ChunkSize;
            FogEnd = RenderDistance*0.2f*ChunkSize;
            ApplySettings();
        }
        public void ApplyNormalSettings(){
            GraphicsDevice.Clear(Color.CornflowerBlue);
            FogColor.SetValue(new Vector3(100f/255f,149f/255f,237f/255f));
            FogStart = RenderDistance*0.6f*ChunkSize;
            FogEnd = RenderDistance*0.8f*ChunkSize;
            ApplySettings();
        }
        private void ApplySettings() {
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            GraphicsDevice.Indices = indexBuffer;
            GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
        }
    }
}