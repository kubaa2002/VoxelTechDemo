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

        public float fogStart = 115f;
        public float fogEnd = 153f;

        public bool fogEnabled = true;
        
        public CustomEffect(GraphicsDevice device, byte[] code):base(device, code){
            Initialize();
        }
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
        public void Apply(Matrix world, Matrix view){
            float scale = 1.0f/(fogStart-fogEnd);
            if(fogEnabled){
                FogVector.SetValue(new Vector4((world*view).M13*scale,(world*view).M23*scale,(world*view).M33*scale,((world*view).M43+fogStart)*scale));
            }
            else{
                FogVector.SetValue(new Vector4(0,0,0,0));
            }
            CurrentTechnique.Passes[0].Apply();
        }
        public void Apply(){
            FogVector.SetValue(new Vector4(0,0,0,0));
            CurrentTechnique.Passes[0].Apply();
        }
    }
}