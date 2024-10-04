using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace VoxelTechDemo{
    internal class Blocks{
        public Dictionary<int, Vector2[]> TextureDictionary = [];
        public Blocks(){
            byte[] TextureCoordinates = [
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
                50,50,50,50,50,50, //water
                80,80,80,80,80,80, // Glowstone

                255,255,255,255,255,255,//CubeFrame
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
                TextureDictionary[i+1]=result;
            }
        }
        public static bool IsNotSolid(byte Id){
            switch(Id){
                case 0:
                    return true;
                case 14:
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
                case 14:
                    return true;
                default:
                    return false;
            }
        }
    }
}
