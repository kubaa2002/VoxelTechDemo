using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace VoxelTechDemo;
internal class Blocks {
    public static int NumOfBlocks = -1;
    public readonly Dictionary<int, Vector2[]> TextureDictionary = [];
    public Blocks() {
        AddBlock([240, 240, 240, 240, 240, 240]); // 0  - Cube Frame (Air)
        AddBlock([1, 1, 0, 2, 1, 1]); // 1  - Grass
        AddBlock([2, 2, 2, 2, 2, 2]); // 2  - Dirt
        AddBlock([3, 3, 3, 3, 3, 3]); // 3  - Stone
        AddBlock([4, 4, 4, 4, 4, 4]); // 4  - Cobblestone
        AddBlock([16, 16, 17, 17, 16, 16]); // 5  - Oak Log
        AddBlock([19, 19, 19, 19, 19, 19]); // 6  - Planks
        AddBlock([18, 18, 18, 18, 18, 18]); // 7  - Leaves
        AddBlock([48, 48, 48, 48, 48, 48]); // 8  - Glass
        AddBlock([33, 33, 32, 19, 33, 33]); // 9  - Crafting Table
        AddBlock([64, 64, 64, 64, 64, 64]); // 10 - Stone Bricks
        AddBlock([5, 5, 5, 5, 5, 5]); // 11 - Gravel
        AddBlock([6, 6, 6, 6, 6, 6]); // 12 - Sand
        AddBlock([7, 7, 7, 7, 7, 7]); // 13 - Snow
        AddBlock([80, 80, 80, 80, 80, 80]); // 14 - Glowstone
        AddBlock([15, 15, 15, 15, 15, 15]); // 15 - Water
        AddBlock([81, 81, 81, 81, 81, 81]); // 16 - Red Glowstone
        AddBlock([82, 82, 82, 82, 82, 82]); // 17 - Green Glowstone
        AddBlock([83, 83, 83, 83, 83, 83]); // 18 - Blue Glowstone
        AddSprite(96); // 19 - Tall Grass
        AddSprite(97); // 20 - Red Flower
        AddSprite(98); // 21 - Yellow Flower
        AddBlock([8, 8, 8, 8, 8, 8]); // 22 - Ice
        AddSprite(100); // 23 - Snow Grass
        AddBlock([20, 20, 21, 21, 20, 20]); // 24 - Spruce Log
        AddBlock([23, 23, 23, 23, 23, 23]); // 25 - Spruce Planks
        AddBlock([22, 22, 22, 22, 22, 22]); // 26  - Spruce Leaves
        AddSprite(99); // 27 - Dead Bush
        AddBlock([34, 34, 35, 35, 34, 34]); // 28 - Cactus
    }
    private void AddBlock(byte[] faces) {
        NumOfBlocks++;
        Vector2[] result= new Vector2[6];
        for(int i=0;i<6;i++){
            result[i]=CalcUV(faces[i]);
        }
        TextureDictionary[NumOfBlocks]=result;
    }
    private void AddSprite(byte face) {
        NumOfBlocks++;
        TextureDictionary[NumOfBlocks] = [CalcUV(face)];
    }
    private Vector2 CalcUV(byte face) {
        return new Vector2(face%16, face/16)*0.0625f;
    }
    public static readonly int[] NoRotation = [0, 0, 0, 0, 0, 0];
    public static readonly int[] AxisXRotation = [1,1,0,0,1,1];
    public static readonly int[] AxisZRotation = [1,1,1,1,0,0];
    public static bool IsNotSolid(byte Id){
        switch(Id){
            case 0:
                return true;
            case 15:
                return true;
            case 19:
                return true;
            case 20:
                return true;
            case 21:
                return true;
            case 23:
                return true;
            case 27:
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
            case 19:
                return true;
            case 20:
                return true;
            case 21:
                return true;
            case 23:
                return true;
            case 26:
                return true;
            case 27:
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
                return (Light.lightMask, Light.lightMask, Light.lightMask);
            case 16:
                return (Light.lightMask, 0, 0);
            case 17:
                return (0, Light.lightMask, 0);
            case 18:
                return (0, 0, Light.lightMask);
            default:
                return (0, 0, 0);
        }
    }
    public static bool IsFoliage(byte Id) {
        switch (Id) {
            case 19:
                return true;
            case 20:
                return true;
            case 21:
                return true;
            case 23:
                return true;
            case 27:
                return true;
            default:
                return false;
        }
    }
    public static bool CanRotate(byte id) {
        switch (id) {
            case 5:
                return true;
            case 24:
                return true;
            case 28:
                return true;
            default:
                return false;
        }
    }
}