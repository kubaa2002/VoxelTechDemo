namespace VoxelTechDemo
{
    class Block
    {
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