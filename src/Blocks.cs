using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace VoxelTechDemo{
    internal class Blocks{
        public Dictionary<int, Vector2[]> TextureDictionary = [];
        public Blocks(){
            byte[] TextureCoordinates = [
                241,241,241,241,241,241,//CubeFrame

                1,1,0,2,1,1,//Grass
                2,2,2,2,2,2, //Dirt
                3,3,3,3,3,3, //Stone
                4,4,4,4,4,4, //cobblestone
                16,16,17,17,16,16, //OakLog
                19,19,19,19,19,19, //Planks
                18,18,18,18,18,18, //Leaves
                48,48,48,48,48,48, //Glass
                33,33,32,19,33,33,//CraftingTable
                64,64,64,64,64,64, //StoneBricks
                5,5,5,5,5,5, //gravel
                6,6,6,6,6,6, //sand
                7,7,7,7,7,7, //snow
                80,80,80,80,80,80, // Glowstone
            ];
            for(int i=0;i<(TextureCoordinates.Length/6);i++){
                Vector2[] result= new Vector2[24];
                for(int j=0;j<6;j++){
                    int x=TextureCoordinates[i*6+j]%16;
                    int y=TextureCoordinates[i*6+j]/16;
                    result[j*4]=new Vector2(0.0625f*(x+1),0.0625f*(y+1));
                    result[j*4+1]=new Vector2(0.0625f*x,0.0625f*(y+1));
                    result[j*4+2]=new Vector2(0.0625f*(x+1),0.0625f*y);
                    result[j*4+3]=new Vector2(0.0625f*x,0.0625f*y);
                }
                TextureDictionary[i]=result;
            }
            Vector2[] result2 = new Vector2[24];
            for(int i=0;i<6;i++){
                result2[i*4]= new Vector2(15f/16f, 0);
                result2[i*4+1]= new Vector2(1, 0);
                result2[i*4+2]= new Vector2(15f/16f, 1f/16f);
                result2[i*4+3]= new Vector2(1, 1f/16.0f);
            }
            TextureDictionary[255] = result2;
        }
        public static bool IsNotSolid(byte Id){
            switch(Id){
                case 0:
                    return true;
                case 255:
                    return true;
                default:
                    return false;
            }
        }
        public static bool IsTransparent(byte Id){
            switch(Id){
                case 0:
                    return true;
                case 7:
                    return true;
                case 8:
                    return true;
                case 255:
                    return true;
                default:
                    return false;
            }
        }
        public static bool IsFluid(byte Id){
            switch(Id){
                case 255:
                    return true;
                default:
                    return false;
            }
        }
    }
}
