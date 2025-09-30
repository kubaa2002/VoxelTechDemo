using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static VoxelTechDemo.UserSettings;
using static VoxelTechDemo.VoxelRenderer;

namespace VoxelTechDemo{
    public class CustomEffect : Effect{
        readonly EffectParameter Texture;
        readonly EffectParameter FogColor;
        readonly EffectParameter FogVector;
        readonly EffectParameter WorldViewProj;
        public readonly EffectParameter AnimationFrame;
        public readonly EffectParameter CurrentSkyLightLevel;

        public Matrix projectionMatrix;
        public Matrix viewMatrix;
        public Matrix viewProj;
        private Matrix previewMatrix;

        float FogStart;
        public float FogEnd{
            set{
                fogValue = 1.0f / (FogStart - value);
            }
        }
        float fogValue;

        public CustomEffect(Game game) : this(new Effect(game.GraphicsDevice, File.ReadAllBytes("Content/CustomEffect.mgfx")), game){ }
        private CustomEffect(Effect clone, Game game):base(clone){
            Texture = Parameters["Texture"];
            FogColor = Parameters["FogColor"];
            FogVector = Parameters["FogVector"];
            WorldViewProj = Parameters["WorldViewProj"];
            AnimationFrame = Parameters["AnimationFrame"];
            CurrentSkyLightLevel = Parameters["CurrentSkyLightLevel"];

            Texture.SetValue(game.Content.Load<Texture2D>("Textures"));
            FogStart = RenderDistance*0.6f*ChunkSize;
            FogEnd = RenderDistance*0.8f*ChunkSize;

            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(FieldOfView), GraphicsDevice.DisplayMode.AspectRatio, 0.1f, 10000f);
            previewMatrix = Matrix.CreateLookAt(new Vector3(3, 2, 3), new Vector3(0.5f, 0.5f, 0.5f), Vector3.Up);
            previewMatrix *= CreateBlockPreviewProj((int)(GraphicsDevice.Viewport.Width * 0.93f), (int)(GraphicsDevice.Viewport.Height * 0.9f), 5);
        }
        private Matrix CreateBlockPreviewProj(int x,int y,float scale){
            float aspectRatio = GraphicsDevice.Viewport.AspectRatio * scale;
            float translateX = aspectRatio-(float)x / GraphicsDevice.Viewport.Width * (aspectRatio + aspectRatio);
            float translateY = (float)y / GraphicsDevice.Viewport.Height * (scale + scale) - scale;
            return Matrix.CreateOrthographicOffCenter(translateX-aspectRatio,aspectRatio+translateX,translateY-scale,scale+translateY,1f, 10f);
        }
        public void UpdateProjMatrix(Matrix proj, Player player) {
            projectionMatrix = proj;
            UpdateViewMatrix(player);
        }
        public void UpdateViewMatrix(Player player) {
            viewMatrix = Matrix.CreateLookAt(player.camPosition, player.camPosition + player.forward, Vector3.Up);
            viewProj = viewMatrix * projectionMatrix;
        }
        public void DrawBlockPreview() {
            WorldViewProj.SetValue(previewMatrix);
            FogVector.SetValue(Vector4.Zero);
            CurrentTechnique.Passes[0].Apply();
        }
        public void UpdateAnimationFrame(TimeSpan totalTime) {
            AnimationFrame.SetValue((float)Math.Round(totalTime.TotalSeconds * 8 % 15) / 16);
            if (DayCycle) {
                CurrentSkyLightLevel.SetValue((float)(Math.Sin((totalTime.TotalSeconds) * Math.PI / 60) + 1) / 2);
            }
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
        }
        public void ApplyNormalSettings(){
            GraphicsDevice.Clear(Color.CornflowerBlue);
            FogColor.SetValue(Color.CornflowerBlue.ToVector3());
            FogStart = RenderDistance*0.6f*ChunkSize;
            FogEnd = RenderDistance*0.8f*ChunkSize;
        }
    }

    public class CloudEffect : Effect {
        public readonly EffectParameter WorldViewProj;
        private readonly EffectParameter Scale;
        readonly EffectParameter FogColor;
        readonly EffectParameter FogVector;

        private float fogStart;
        private float fogValue;

        public CloudEffect(Game game) : this(new(game.GraphicsDevice, File.ReadAllBytes("Content/CloudEffect.mgfx")), game) { }
        private CloudEffect(Effect clone, Game game):base(clone){
            WorldViewProj = Parameters["WorldViewProj"];
            Scale = Parameters["Scale"];
            FogColor = Parameters["FogColor"];
            FogVector = Parameters["FogVector"];
            
            Scale.SetValue(cloudRes);
            FogColor.SetValue(Color.CornflowerBlue.ToVector3());
            fogStart = RenderDistance * 0.6f * ChunkSize;
            float fogEnd = RenderDistance * 1f * ChunkSize;
            fogValue = 1.0f / (fogStart - fogEnd);
        }
        public void Apply(CustomEffect effect, Matrix worldMatrix) {
            WorldViewProj.SetValue(worldMatrix * effect.viewProj);
            Matrix worldView = worldMatrix * effect.viewMatrix;
            FogVector.SetValue(new Vector4(worldView.M13,worldView.M23,worldView.M33,worldView.M43+fogStart)*fogValue);
            CurrentTechnique.Passes[0].Apply();
        }
    }
}