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
                if(!Blocks.IsNotSolid(currentWorld.GetBlock((int)currentBlock.Min.X,(int)currentBlock.Min.Y,(int)currentBlock.Min.Z,CurrentChunk))){
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
        public void NoClipMovement(KeyboardState keyboardState, GameTime gameTime){
            float movementSpeed = (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            if(keyboardState.IsKeyDown(Keys.LeftShift)){
                movementSpeed *= 0.1f;
            }
            else{
                movementSpeed *= 0.025f;
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
            if(Blocks.IsFluid(currentWorld.GetBlock((int)Math.Floor(camPosition.X),(int)Math.Floor(camPosition.Y-0.1f),(int)Math.Floor(camPosition.Z),CurrentChunk))){
                IsUnderWater = true;
            }
            else{
                IsUnderWater = false;
            }
        }
        public void NormalMovement(KeyboardState keyboardState, GameTime gameTime, float yaw){
            forward = Vector3.Zero;
            float movementSpeed;
            if(Blocks.IsFluid(currentWorld.GetBlock((int)Math.Floor(camPosition.X),(int)Math.Floor(playerHitBox.Min.Y),(int)Math.Floor(camPosition.Z),CurrentChunk))){
                movementSpeed = 0.0025f;
            }
            else{
                movementSpeed = 0.00625f;
            }
            if(keyboardState.IsKeyDown(Keys.LeftShift)){
                movementSpeed *= 2;
            }
            if (keyboardState.IsKeyDown(Keys.W)){
                forward += Vector3.Transform(Vector3.Forward, Matrix.CreateFromYawPitchRoll(yaw, 0f, 0f));
            }
            if (keyboardState.IsKeyDown(Keys.S)){
                forward -= Vector3.Transform(Vector3.Forward, Matrix.CreateFromYawPitchRoll(yaw, 0f, 0f));
            }
            if (keyboardState.IsKeyDown(Keys.A)){
                forward -= right;
            }
            if (keyboardState.IsKeyDown(Keys.D)){
                forward += right;
            }
            
            if(forward != Vector3.Zero){
                forward.Normalize();
                forward *= movementSpeed*(float)gameTime.ElapsedGameTime.TotalMilliseconds;
                if(forward.X != 0) {
                    int blockX = (int)Math.Floor((forward.X > 0 ? playerHitBox.Max.X : playerHitBox.Min.X) + forward.X);
                    if(CheckCollision(blockX,(int)Math.Floor(playerHitBox.Min.Y),(int)Math.Floor(playerHitBox.Min.Z),blockX,(int)Math.Floor(playerHitBox.Max.Y),(int)Math.Floor(playerHitBox.Max.Z))) {
                        camPosition.X = (float)Math.Floor(camPosition.X) + (forward.X > 0 ? 0.75f : 0.25f);
                    }
                    else{
                        camPosition.X += forward.X;
                    }
                    playerHitBox.Min.X = camPosition.X-0.2499f;
                    playerHitBox.Max.X = camPosition.X+0.2499f;
                }
                if(forward.Z != 0){
                    int blockZ = (int)Math.Floor((forward.Z > 0 ? playerHitBox.Max.Z : playerHitBox.Min.Z) + forward.Z);
                    if(CheckCollision((int)Math.Floor(playerHitBox.Min.X),(int)Math.Floor(playerHitBox.Min.Y),blockZ,(int)Math.Floor(playerHitBox.Max.X),(int)Math.Floor(playerHitBox.Max.Y),blockZ)){
                        camPosition.Z = (float)Math.Floor(camPosition.Z) + (forward.Z > 0 ? 0.75f : 0.25f);
                    }
                    else{
                        camPosition.Z += forward.Z;
                    }
                    playerHitBox.Min.Z = camPosition.Z-0.2499f;
                    playerHitBox.Max.Z = camPosition.Z+0.2499f;
                }
            }
            
            if(Blocks.IsFluid(currentWorld.GetBlock((int)Math.Floor(camPosition.X),(int)Math.Floor(playerHitBox.Min.Y),(int)Math.Floor(camPosition.Z),CurrentChunk))){
                if(Blocks.IsFluid(currentWorld.GetBlock((int)Math.Floor(camPosition.X),(int)Math.Floor(playerHitBox.Min.Y+0.8f),(int)Math.Floor(camPosition.Z),CurrentChunk))){
                    if(keyboardState.IsKeyDown(Keys.Space)){
                        verticalSpeed = 0.003f;
                    }
                    verticalSpeed -= 0.0000015f*(float)gameTime.ElapsedGameTime.TotalMilliseconds;
                    if(verticalSpeed<-0.004f){
                        verticalSpeed = -0.004f;
                    }
                }
            }
            else{
                if(keyboardState.IsKeyDown(Keys.Space) && canJump){
                    verticalSpeed = 0.0085f;
                }
                verticalSpeed -= 0.000015f*(float)gameTime.ElapsedGameTime.TotalMilliseconds;
                if(verticalSpeed<-1f){
                    verticalSpeed = -1f;
                }
            }
            
            int blockY = verticalSpeed <= 0
                ? (int)Math.Floor(playerHitBox.Min.Y + verticalSpeed * (float)gameTime.ElapsedGameTime.TotalMilliseconds)
                : (int)(Math.Floor(playerHitBox.Max.Y + 0.13f) + verticalSpeed * (float)gameTime.ElapsedGameTime.TotalMilliseconds);
            if(CheckCollision((int)Math.Floor(playerHitBox.Min.X),blockY,(int)Math.Floor(playerHitBox.Min.Z),(int)Math.Floor(playerHitBox.Max.X),blockY,(int)Math.Floor(playerHitBox.Max.Z))){
                if (verticalSpeed <= 0) {
                    camPosition.Y = (float)Math.Round((float)Math.Floor(playerHitBox.Min.Y) + 1.7f, 2);
                    canJump = true;
                }
                verticalSpeed = 0;
            }
            else{
                camPosition.Y += verticalSpeed*(float)gameTime.ElapsedGameTime.TotalMilliseconds;
                canJump = false;
                if(Blocks.IsFluid(currentWorld.GetBlock((int)Math.Floor(camPosition.X),(int)Math.Floor(playerHitBox.Min.Y),(int)Math.Floor(camPosition.Z),CurrentChunk))){
                    verticalSpeed -= 0.0000015f*(float)gameTime.ElapsedGameTime.TotalMilliseconds;
                }
                else{
                    verticalSpeed -= 0.000015f*(float)gameTime.ElapsedGameTime.TotalMilliseconds;
                }
            }
            ResetCamera();
            playerHitBox.Min.Y = camPosition.Y-1.6999f;
            playerHitBox.Max.Y = camPosition.Y+0.0999f;
            if(Blocks.IsFluid(currentWorld.GetBlock((int)Math.Floor(camPosition.X),(int)Math.Floor(camPosition.Y-0.1f),(int)Math.Floor(camPosition.Z),CurrentChunk))){
                IsUnderWater = true;
            }
            else{
                IsUnderWater = false;
            }
        }
        private bool CheckCollision(int startX, int startY, int startZ, int endX, int endY, int endZ) {
            for (int x = startX; x <= endX; x++) {
                for (int y = startY; y <= endY; y++) {
                    for (int z = startZ; z <= endZ; z++) {
                        if(!Blocks.IsNotSolid(currentWorld.GetBlock(x,y,z,CurrentChunk))) {
                            return true;
                        }
                    }
                }
            }
            return false;
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
