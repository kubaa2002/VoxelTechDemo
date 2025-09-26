using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using static VoxelTechDemo.VoxelRenderer;

namespace VoxelTechDemo{
    public class Player{
        private World currentWorld;
        private float verticalSpeed;
        private bool canJump;
        
        public Vector3 LookedAtBlock;
        public bool IsUnderWater, BlockFound;
        public Vector3 camPosition, forward, right;
        public BoundingBox playerHitBox;
        public BlockFace currentSide;
        public (int x,int y,int z) CurrentChunk;
        public Player(World world){
            currentWorld = world;
            if (!SaveFile.GetPlayerPosition(this) || camPosition == Vector3.Zero)
                camPosition = new Vector3(35.5f, 53+(int)Math.Floor(currentWorld.MountainNoise(35+CurrentChunk.x*ChunkSize,35+CurrentChunk.z*ChunkSize)), 35.5f);
            playerHitBox = new BoundingBox(Vector3.Zero,Vector3.Zero);
            ResetHitBox();
        }
        public void GetLookedAtBlock(){
            BlockFound = false;
            Ray ray = new(camPosition,forward);
            for(int i=1;i<10;i++){
                Vector3 currentRayPosition = camPosition+(forward*i);
                currentRayPosition.Floor();
                BoundingBox currentBlock = new(currentRayPosition,currentRayPosition+Vector3.One);
                byte blockId = currentWorld.GetBlock((int)currentBlock.Min.X, (int)currentBlock.Min.Y,
                    (int)currentBlock.Min.Z, CurrentChunk);
                if(blockId != 0 && !Blocks.IsFluid(blockId)){
                    LookedAtBlock = currentBlock.Min;
                    BlockFound = true;
                }
                for(int j=0;j<2;j++){
                    switch(GetFace(ray,currentBlock)){
                        case BlockFace.Front:
                            currentBlock.Min.Z--;
                            currentBlock.Max.Z--;
                            break;
                        case BlockFace.Back:
                            currentBlock.Min.Z++;
                            currentBlock.Max.Z++;
                            break;
                        case BlockFace.Right:
                            currentBlock.Min.X--;
                            currentBlock.Max.X--;
                            break;
                        case BlockFace.Left:
                            currentBlock.Min.X++;
                            currentBlock.Max.X++;
                            break;
                        case BlockFace.Top:
                            currentBlock.Min.Y++;
                            currentBlock.Max.Y++;
                            break;
                        case BlockFace.Bottom:
                            currentBlock.Min.Y--;
                            currentBlock.Max.Y--;
                            break;
                    }
                    if(!Blocks.IsNotSolid(currentWorld.GetBlock((int)currentBlock.Min.X,(int)currentBlock.Min.Y,(int)currentBlock.Min.Z,CurrentChunk))){
                        LookedAtBlock = currentBlock.Min;
                        BlockFound = true;
                    }
                }
                if(BlockFound){
                    currentSide = GetFace(ray,new BoundingBox(LookedAtBlock,LookedAtBlock+Vector3.One));
                    break;
                }
            }
        }
        public void NoClipMovement(KeyboardState keyboardState, float elapsedSeconds){
            float movementSpeed = 25f * elapsedSeconds;
            if(keyboardState.IsKeyDown(Keys.LeftShift)){
                movementSpeed *= 4;
            }
            if (keyboardState.IsKeyDown(Keys.W)){
                camPosition += forward*movementSpeed;         
            }
            if (keyboardState.IsKeyDown(Keys.S)){
                camPosition -= forward*movementSpeed;
            }
            if (keyboardState.IsKeyDown(Keys.A)){
                camPosition -= right*movementSpeed;
            }
            if (keyboardState.IsKeyDown(Keys.D)){
                camPosition += right*movementSpeed; 
            }
            if(keyboardState.IsKeyDown(Keys.Space)){
                camPosition.Y += movementSpeed;
            }
            if(keyboardState.IsKeyDown(Keys.LeftControl)){
                camPosition.Y -= movementSpeed;
            }
            ResetCamera();
            IsUnderWater = CheckIfInWater(camPosition.X, camPosition.Y - 0.1f, camPosition.Z);
        }
        public void NormalMovement(KeyboardState keyboardState, float elapsedSeconds){
            Vector3 direction = Vector3.Zero;
            if (keyboardState.IsKeyDown(Keys.W)){
                direction += forward;
            }
            if (keyboardState.IsKeyDown(Keys.S)){
                direction -= forward;
            }
            if (keyboardState.IsKeyDown(Keys.A)){
                direction -= right;
            }
            if (keyboardState.IsKeyDown(Keys.D)){
                direction += right;
            }
            
            if(direction != Vector3.Zero){
                float movementSpeed = 6.25f;
                if(CheckIfInWater(camPosition.X, playerHitBox.Min.Y, camPosition.Z)){
                    movementSpeed = 2.5f;
                }
                if(keyboardState.IsKeyDown(Keys.LeftShift)){
                    movementSpeed *= 2;
                }
                direction.Y = 0;
                direction.Normalize();
                direction *= movementSpeed * elapsedSeconds;

                float blockX = (direction.X > 0 ? playerHitBox.Max.X : playerHitBox.Min.X) + direction.X;
                if(CheckCollision(blockX, playerHitBox.Min.Y, playerHitBox.Min.Z, blockX, playerHitBox.Max.Y, playerHitBox.Max.Z)) {
                    camPosition.X = (float)Math.Floor(camPosition.X) + (direction.X > 0 ? 0.75f : 0.25f);
                }
                else{
                    camPosition.X += direction.X;
                }
                playerHitBox.Min.X = camPosition.X - 0.2499f;
                playerHitBox.Max.X = camPosition.X + 0.2499f;

                float blockZ = (direction.Z > 0 ? playerHitBox.Max.Z : playerHitBox.Min.Z) + direction.Z;
                if(CheckCollision(playerHitBox.Min.X, playerHitBox.Min.Y, blockZ, playerHitBox.Max.X, playerHitBox.Max.Y, blockZ)){
                    camPosition.Z = (float)Math.Floor(camPosition.Z) + (direction.Z > 0 ? 0.75f : 0.25f);
                }
                else{
                    camPosition.Z += direction.Z;
                }
                playerHitBox.Min.Z = camPosition.Z - 0.2499f;
                playerHitBox.Max.Z = camPosition.Z + 0.2499f;
            }
            
            if(CheckIfInWater(camPosition.X, playerHitBox.Min.Y, camPosition.Z)){
                if(CheckIfInWater(camPosition.X, playerHitBox.Min.Y + 0.8f, camPosition.Z)){
                    if(keyboardState.IsKeyDown(Keys.Space)){
                        verticalSpeed = 3f;
                    }
                    verticalSpeed -= 1.5f * elapsedSeconds;
                    verticalSpeed = Math.Max(verticalSpeed, -4f);
                }
            }
            else{
                if(keyboardState.IsKeyDown(Keys.Space) && canJump){
                    verticalSpeed = 8.5f;
                }
                verticalSpeed -= 15f * elapsedSeconds;
                verticalSpeed = Math.Max(verticalSpeed, -1000f);
            }
            
            float blockY = (verticalSpeed <= 0 ? playerHitBox.Min.Y : playerHitBox.Max.Y) + verticalSpeed * elapsedSeconds;
            if(CheckCollision(playerHitBox.Min.X, blockY, playerHitBox.Min.Z, playerHitBox.Max.X, blockY, playerHitBox.Max.Z)){
                if (verticalSpeed <= 0) {
                    camPosition.Y = (float)Math.Floor(playerHitBox.Min.Y) + 1.7f;
                    canJump = true;
                }
                verticalSpeed = 0;
            }
            else{
                camPosition.Y += verticalSpeed * elapsedSeconds;
                canJump = false;
                verticalSpeed -= (CheckIfInWater(camPosition.X, playerHitBox.Min.Y, camPosition.Z) ? 1.5f : 15f) * elapsedSeconds;
            }
            ResetCamera();
            playerHitBox.Min.Y = camPosition.Y - 1.6999f;
            playerHitBox.Max.Y = camPosition.Y + 0.0999f;
            IsUnderWater = CheckIfInWater(camPosition.X, camPosition.Y - 0.1f, camPosition.Z);
        }
        private bool CheckCollision(float startX, float startY, float startZ, float endX, float endY, float endZ) {
            for (int x = (int)Math.Floor(startX); x <= (int)Math.Floor(endX); x++) {
                for (int y = (int)Math.Floor(startY); y <= (int)Math.Floor(endY); y++) {
                    for (int z = (int)Math.Floor(startZ); z <= (int)Math.Floor(endZ); z++) {
                        if(!Blocks.IsNotSolid(currentWorld.GetBlock(x,y,z,CurrentChunk))) {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        private bool CheckIfInWater(float x, float y, float z) {
            return Blocks.IsFluid(currentWorld.GetBlock((int)Math.Floor(x),(int)Math.Floor(y),(int)Math.Floor(z),CurrentChunk));
        }
        public void ResetHitBox(){
            playerHitBox.Min.X = camPosition.X-0.2499f;
            playerHitBox.Min.Y = camPosition.Y-1.6999f;
            playerHitBox.Min.Z = camPosition.Z-0.2499f;
            playerHitBox.Max.X = camPosition.X+0.2499f;
            playerHitBox.Max.Y = camPosition.Y+0.0999f;
            playerHitBox.Max.Z = camPosition.Z+0.2499f;

            verticalSpeed = 0;
        }
        private void ResetCamera(){
            bool chunkChanged = false;
            if(camPosition.X>ChunkSize){
                camPosition.X-=ChunkSize;
                CurrentChunk.x+=1;
                playerHitBox.Min.X = camPosition.X-0.2499f;
                playerHitBox.Max.X = camPosition.X+0.2499f;
                chunkChanged = true;
                
            }
            if(camPosition.X<0f){
                camPosition.X+=ChunkSize;
                CurrentChunk.x-=1;
                playerHitBox.Min.X = camPosition.X-0.2499f;
                playerHitBox.Max.X = camPosition.X+0.2499f;
                chunkChanged = true;
            }
            if(camPosition.Z>ChunkSize){
                camPosition.Z-=ChunkSize;
                CurrentChunk.z+=1;
                playerHitBox.Min.Z = camPosition.Z-0.2499f;
                playerHitBox.Max.Z = camPosition.Z+0.2499f;
                chunkChanged = true;

            }
            if(camPosition.Z<0f){
                camPosition.Z+=ChunkSize;
                CurrentChunk.z-=1;
                playerHitBox.Min.Z = camPosition.Z-0.2499f;
                playerHitBox.Max.Z = camPosition.Z+0.2499f;
                chunkChanged = true;
            }
            if(camPosition.Y>ChunkSize){
                camPosition.Y-=ChunkSize;
                CurrentChunk.y+=1;
            }
            if(camPosition.Y<0f){
                camPosition.Y+=ChunkSize;
                CurrentChunk.y-=1;
            }
            if (chunkChanged) {
                currentWorld.UpdateLoadedChunks(CurrentChunk.x, CurrentChunk.z);
            }
        }
    }
}
