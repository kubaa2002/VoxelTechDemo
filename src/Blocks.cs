using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace VoxelTechDemo{
    internal class Blocks{
        public Dictionary<int, Vector2[]> TextureDictionary = [];
        public Blocks(){
            byte[] blockFaces = [
                240,240,240,240,240,240,    // 0  - Cube Frame (Air)

                1,1,0,2,1,1,                // 1  - Grass
                2,2,2,2,2,2,                // 2  - Dirt
                3,3,3,3,3,3,                // 3  - Stone
                4,4,4,4,4,4,                // 4  - Cobblestone
                16,16,17,17,16,16,          // 5  - Oak Log
                19,19,19,19,19,19,          // 6  - Planks
                18,18,18,18,18,18,          // 7  - Leaves
                48,48,48,48,48,48,          // 8  - Glass
                33,33,32,19,33,33,          // 9  - Crafting Table
                64,64,64,64,64,64,          // 10 - Stone Bricks
                5,5,5,5,5,5,                // 11 - Gravel
                6,6,6,6,6,6,                // 12 - Sand
                7,7,7,7,7,7,                // 13 - Snow
                80,80,80,80,80,80,          // 14 - Glowstone
                15,15,15,15,15,15,          // 15 - Water
                81,81,81,81,81,81,          // 16 - Red Glowstone
                82,82,82,82,82,82,          // 17 - Green Glowstone
                83,83,83,83,83,83,          // 18 - Blue Glowstone
            ];
            for(int i=0;i<(blockFaces.Length/6);i++){
                Vector2[] result= new Vector2[24];
                for(int j=0;j<6;j++){
                    int x=blockFaces[i*6+j]%16;
                    int y=blockFaces[i*6+j]/16;
                    result[j*4]=new Vector2(0.0625f*(x+1),0.0625f*(y+1));
                    result[j*4+1]=new Vector2(0.0625f*x,0.0625f*(y+1));
                    result[j*4+2]=new Vector2(0.0625f*(x+1),0.0625f*y);
                    result[j*4+3]=new Vector2(0.0625f*x,0.0625f*y);
                }
                TextureDictionary[i]=result;
            }
        }
        public static bool IsNotSolid(byte Id){
            switch(Id){
                case 0:
                    return true;
                case 15:
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
                case 15:
                    return true;
                default:
                    return false;
            }
        }
        public static bool IsFluid(byte Id){
            switch(Id){
                case 15:
                    return true;
                default:
                    return false;
            }
        }
        public static bool IsLightEminiting(byte Id) {
            switch (Id) {
                case 14:
                    return true;
                case 16:
                    return true;
                case 17:
                    return true;
                case 18:
                    return true;
                default:
                    return false;
            }
        }
        public static (int red, int blue, int green) ReturnBlockLightValues(byte Id) {
            switch (Id) {
                // yellow glowstone
                case 14:
                    return (31, 31, 31);
                case 16:
                    return (31, 0, 0);
                case 17:
                    return (0, 31, 0);
                case 18:
                    return (0, 0, 31);
                default:
                    return (0, 0, 0);
            }
        }
    }
}
