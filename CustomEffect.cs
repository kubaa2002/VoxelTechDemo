using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VoxelTechDemo{
    public class CustomEffect : Effect{
        public EffectParameter Texture;
        public EffectParameter DiffuseColor;
        public EffectParameter AlphaTest;
        public EffectParameter FogColor;
        public EffectParameter FogVector;
        public EffectParameter WorldViewProj;

        public float fogStart;
        // fogValue = 1.0f/(fogStart-fogEnd)
        public float fogValue;
        
        public CustomEffect(Effect clone):base(clone){
            Initialize();
        }
        private void Initialize(){
            Texture = Parameters["Texture"];
            DiffuseColor = Parameters["DiffuseColor"];
            AlphaTest = Parameters["AlphaTest"];
            FogColor = Parameters["FogColor"];
            FogVector = Parameters["FogVector"];
            WorldViewProj = Parameters["WorldViewProj"];
        }
        public void Apply(Matrix worldView){
            if(UserSettings.FogEnabled){
                FogVector.SetValue(new Vector4(worldView.M13,worldView.M23,worldView.M33,worldView.M43+fogStart)*fogValue);
            }
            else{
                FogVector.SetValue(Vector4.Zero);
            }
            CurrentTechnique.Passes[0].Apply();
        }
    }
}