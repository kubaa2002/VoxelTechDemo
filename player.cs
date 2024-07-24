using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using static VoxelTechDemo.VoxelRenderer;

namespace VoxelTechDemo
{
    class Player
    {
        World currentWorld;
        public (int x, int y, int z) LookedAtBlock;
        public bool IsUnderWater = false, blockFound = false;
        bool CanJump = false;
        float movementSpeed, verticalSpeed;
        public Vector3 camPosition, camTarget, forward, right;
        public BoundingBox playerHitBox;
        public BlockFace currentSide;
        public (int x,int y,int z) CurrentChunk;
        public bool ChunkChanged = false;
        public Player(World world){
            currentWorld = world;
            int ylevel = 53+(int)Math.Floor(currentWorld.MountainNoise(0,0));
            camTarget = Vector3.Zero;
            if(ylevel>=64){
                camPosition = new Vector3(0.5f, ylevel, 0.5f);
            }
            else{
                for(int x=-1;x<=1;x++){
                    for(int z=-1;z<=1;z++){
                        if(world.GetBlock(x,63,z,(0,0,0))==14){
                            world.SetBlock(x,63,z,(0,0,0),6);
                        }
                    }
                }
                camPosition = new Vector3(0.5f, 66.7f, 0.5f);
            }
            playerHitBox = new(new Vector3(camPosition.X-0.2499f,(float)Math.Round(camPosition.Y-1.6999f,2),camPosition.Z-0.2499f),new Vector3(camPosition.X+0.2499f,(float)Math.Round(camPosition.Y+0.0999f,2),camPosition.Z+0.2499f));
        }
        public void GetLookedAtBlock(){
            blockFound = false;
            Ray ray = new(camPosition,forward);
            for(int i=1;i<10;i++){
                Vector3 currentRayPossition = camPosition+(forward*i);
                currentRayPossition.Floor();
                BoundingBox currentBlock = new(currentRayPossition,currentRayPossition+Vector3.One);
                if(!Block.IsNotSolid(currentWorld.GetBlock((int)currentBlock.Min.X,(int)currentBlock.Min.Y,(int)currentBlock.Min.Z,CurrentChunk))){
                    LookedAtBlock = ((int)currentBlock.Min.X,(int)currentBlock.Min.Y,(int)currentBlock.Min.Z);
                    blockFound = true;
                }
                for(int j=0;j<2;j++){
                    BlockFace Intersection = GetFace(ray,currentBlock);
                    if(Intersection == BlockFace.Front) currentBlock = new(new Vector3(currentBlock.Min.X,currentBlock.Min.Y,currentBlock.Min.Z-1),new Vector3(currentBlock.Max.X,currentBlock.Max.Y,currentBlock.Min.Z));
                    if(Intersection == BlockFace.Back) currentBlock = new(new Vector3(currentBlock.Min.X,currentBlock.Min.Y,currentBlock.Max.Z),new Vector3(currentBlock.Max.X,currentBlock.Max.Y,currentBlock.Min.Z+2));
                    if(Intersection == BlockFace.Right) currentBlock = new(new Vector3(currentBlock.Min.X-1,currentBlock.Min.Y,currentBlock.Min.Z),new Vector3(currentBlock.Min.X,currentBlock.Max.Y,currentBlock.Max.Z));
                    if(Intersection == BlockFace.Left) currentBlock = new(new Vector3(currentBlock.Max.X,currentBlock.Min.Y,currentBlock.Min.Z),new Vector3(currentBlock.Min.X+2,currentBlock.Max.Y,currentBlock.Max.Z));
                    if(Intersection == BlockFace.Top) currentBlock = new(new Vector3(currentBlock.Min.X,currentBlock.Max.Y,currentBlock.Min.Z),new Vector3(currentBlock.Max.X,currentBlock.Min.Y+2,currentBlock.Max.Z));
                    if(Intersection == BlockFace.Bottom) currentBlock = new(new Vector3(currentBlock.Min.X,currentBlock.Min.Y-1,currentBlock.Min.Z),new Vector3(currentBlock.Max.X,currentBlock.Min.Y,currentBlock.Max.Z));
                    if(!Block.IsNotSolid(currentWorld.GetBlock((int)currentBlock.Min.X,(int)currentBlock.Min.Y,(int)currentBlock.Min.Z,CurrentChunk))){
                        LookedAtBlock = ((int)currentBlock.Min.X,(int)currentBlock.Min.Y,(int)currentBlock.Min.Z);
                        blockFound = true;
                    }
                }
                if(blockFound == true){
                    currentSide = GetFace(ray,new BoundingBox(new Vector3(LookedAtBlock.x,LookedAtBlock.y,LookedAtBlock.z),new Vector3(LookedAtBlock.x+1,LookedAtBlock.y+1,LookedAtBlock.z+1)));
                    break;
                }
            }
        }
        public void NoClipMovement(KeyboardState keyboardState, GameTime gameTime){
            if(keyboardState.IsKeyDown(Keys.LeftShift)){
                movementSpeed = 0.1f;
            }
            else{
                movementSpeed = 0.025f;
            }
            if (keyboardState.IsKeyDown(Keys.W)){
                camPosition += forward*movementSpeed*(float)gameTime.ElapsedGameTime.TotalMilliseconds;         
            }
            if (keyboardState.IsKeyDown(Keys.S)){
                camPosition -= forward*movementSpeed*(float)gameTime.ElapsedGameTime.TotalMilliseconds;
            }
            if (keyboardState.IsKeyDown(Keys.A)){
                camPosition -= right*movementSpeed*(float)gameTime.ElapsedGameTime.TotalMilliseconds;
            }
            if (keyboardState.IsKeyDown(Keys.D)){
                camPosition += right*movementSpeed*(float)gameTime.ElapsedGameTime.TotalMilliseconds; 
            }
            if(keyboardState.IsKeyDown(Keys.Space)){
                camPosition += Vector3.Up*movementSpeed*(float)gameTime.ElapsedGameTime.TotalMilliseconds;
            }
            if(keyboardState.IsKeyDown(Keys.LeftControl)){
                camPosition -= Vector3.Up*movementSpeed*(float)gameTime.ElapsedGameTime.TotalMilliseconds;
            }
            ResetCamera();
            if(currentWorld.GetBlock((int)Math.Floor(camPosition.X),(int)Math.Floor(camPosition.Y),(int)Math.Floor(camPosition.Z),CurrentChunk)==14){
                IsUnderWater = true;
            }
            else{
                IsUnderWater = false;
            }
        }
        public void NormalMovement(KeyboardState keyboardState, GameTime gameTime, float yaw){
            forward = Vector3.Zero;
            if(currentWorld.GetBlock((int)Math.Floor(camPosition.X),(int)Math.Floor(playerHitBox.Min.Y),(int)Math.Floor(camPosition.Z),CurrentChunk)==14){
                if(keyboardState.IsKeyDown(Keys.LeftShift)){
                    movementSpeed = 0.005f;
                }
                else{
                    movementSpeed = 0.0025f;
                }
            }
            else{
                if(keyboardState.IsKeyDown(Keys.LeftShift)){
                    movementSpeed = 0.0125f;
                }
                else{
                    movementSpeed = 0.00625f;
                }
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
                if(forward.X>0){
                    if(Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Max.X+forward.X),(int)Math.Floor(playerHitBox.Min.Y),(int)Math.Floor(playerHitBox.Min.Z),CurrentChunk))
                    && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Max.X+forward.X),(int)Math.Floor(playerHitBox.Min.Y),(int)Math.Floor(playerHitBox.Max.Z),CurrentChunk))
                    && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Max.X+forward.X),(int)Math.Floor((playerHitBox.Max.Y+playerHitBox.Min.Y)/2),(int)Math.Floor(playerHitBox.Min.Z),CurrentChunk))
                    && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Max.X+forward.X),(int)Math.Floor((playerHitBox.Max.Y+playerHitBox.Min.Y)/2),(int)Math.Floor(playerHitBox.Max.Z),CurrentChunk))
                    && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Max.X+forward.X),(int)Math.Floor(playerHitBox.Max.Y),(int)Math.Floor(playerHitBox.Min.Z),CurrentChunk))
                    && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Max.X+forward.X),(int)Math.Floor(playerHitBox.Max.Y),(int)Math.Floor(playerHitBox.Max.Z),CurrentChunk))){
                        camPosition.X += forward.X;
                    }
                    else{
                        camPosition.X = (float)Math.Floor(camPosition.X+forward.X)+0.75f;
                    }
                }
                else{
                    if(Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Min.X+forward.X),(int)Math.Floor(playerHitBox.Min.Y),(int)Math.Floor(playerHitBox.Min.Z),CurrentChunk))
                    && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Min.X+forward.X),(int)Math.Floor(playerHitBox.Min.Y),(int)Math.Floor(playerHitBox.Max.Z),CurrentChunk))
                    && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Min.X+forward.X),(int)Math.Floor((playerHitBox.Max.Y+playerHitBox.Min.Y)/2),(int)Math.Floor(playerHitBox.Min.Z),CurrentChunk))
                    && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Min.X+forward.X),(int)Math.Floor((playerHitBox.Max.Y+playerHitBox.Min.Y)/2),(int)Math.Floor(playerHitBox.Max.Z),CurrentChunk))
                    && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Min.X+forward.X),(int)Math.Floor(playerHitBox.Max.Y),(int)Math.Floor(playerHitBox.Min.Z),CurrentChunk))
                    && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Min.X+forward.X),(int)Math.Floor(playerHitBox.Max.Y),(int)Math.Floor(playerHitBox.Max.Z),CurrentChunk))){
                        camPosition.X += forward.X;
                    }
                    else{
                        camPosition.X = (float)Math.Floor(camPosition.X+forward.X)+0.25f;
                    }
                }
                playerHitBox.Min.X = camPosition.X-0.2499f;
                playerHitBox.Max.X = camPosition.X+0.2499f;
                if(forward.Z>0){
                    if(Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Min.X),(int)Math.Floor(playerHitBox.Min.Y),(int)Math.Floor(playerHitBox.Max.Z+forward.Z),CurrentChunk))
                    && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Max.X),(int)Math.Floor(playerHitBox.Min.Y),(int)Math.Floor(playerHitBox.Max.Z+forward.Z),CurrentChunk))
                    && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Min.X),(int)Math.Floor((playerHitBox.Max.Y+playerHitBox.Min.Y)/2),(int)Math.Floor(playerHitBox.Max.Z+forward.Z),CurrentChunk))
                    && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Max.X),(int)Math.Floor((playerHitBox.Max.Y+playerHitBox.Min.Y)/2),(int)Math.Floor(playerHitBox.Max.Z+forward.Z),CurrentChunk))
                    && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Min.X),(int)Math.Floor(playerHitBox.Max.Y),(int)Math.Floor(playerHitBox.Max.Z+forward.Z),CurrentChunk))
                    && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Max.X),(int)Math.Floor(playerHitBox.Max.Y),(int)Math.Floor(playerHitBox.Max.Z+forward.Z),CurrentChunk))){
                        camPosition.Z += forward.Z;
                    }
                    else{
                        camPosition.Z = (float)Math.Floor(camPosition.Z+forward.Z)+0.75f;
                    }
                }
                else{
                    if(Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Min.X),(int)Math.Floor(playerHitBox.Min.Y),(int)Math.Floor(playerHitBox.Min.Z+forward.Z),CurrentChunk))
                    && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Max.X),(int)Math.Floor(playerHitBox.Min.Y),(int)Math.Floor(playerHitBox.Min.Z+forward.Z),CurrentChunk))
                    && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Min.X),(int)Math.Floor((playerHitBox.Max.Y+playerHitBox.Min.Y)/2),(int)Math.Floor(playerHitBox.Min.Z+forward.Z),CurrentChunk))
                    && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Max.X),(int)Math.Floor((playerHitBox.Max.Y+playerHitBox.Min.Y)/2),(int)Math.Floor(playerHitBox.Min.Z+forward.Z),CurrentChunk))
                    && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Min.X),(int)Math.Floor(playerHitBox.Max.Y),(int)Math.Floor(playerHitBox.Min.Z+forward.Z),CurrentChunk))
                    && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Max.X),(int)Math.Floor(playerHitBox.Max.Y),(int)Math.Floor(playerHitBox.Min.Z+forward.Z),CurrentChunk))){
                        camPosition.Z += forward.Z;
                    }
                    else{
                        camPosition.Z = (float)Math.Floor(camPosition.Z+forward.Z)+0.25f;
                    }
                }
                playerHitBox.Min.Z = camPosition.Z-0.2499f;
                playerHitBox.Max.Z = camPosition.Z+0.2499f;
            }
            if(currentWorld.GetBlock((int)Math.Floor(camPosition.X),(int)Math.Floor(playerHitBox.Min.Y),(int)Math.Floor(camPosition.Z),CurrentChunk)==14){
                if(currentWorld.GetBlock((int)Math.Floor(camPosition.X),(int)Math.Floor(playerHitBox.Min.Y+0.8f),(int)Math.Floor(camPosition.Z),CurrentChunk)==14){
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
                if(keyboardState.IsKeyDown(Keys.Space) && CanJump){
                    verticalSpeed = 0.0085f;
                }
                verticalSpeed -= 0.000015f*(float)gameTime.ElapsedGameTime.TotalMilliseconds;
                if(verticalSpeed<-1f){
                    verticalSpeed = -1f;
                }
            }
            if(verticalSpeed<=0){
                if(Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Min.X),(int)Math.Floor(playerHitBox.Min.Y+verticalSpeed*(float)gameTime.ElapsedGameTime.TotalMilliseconds),(int)Math.Floor(playerHitBox.Min.Z),CurrentChunk))
                && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Max.X),(int)Math.Floor(playerHitBox.Min.Y+verticalSpeed*(float)gameTime.ElapsedGameTime.TotalMilliseconds),(int)Math.Floor(playerHitBox.Min.Z),CurrentChunk))
                && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Min.X),(int)Math.Floor(playerHitBox.Min.Y+verticalSpeed*(float)gameTime.ElapsedGameTime.TotalMilliseconds),(int)Math.Floor(playerHitBox.Max.Z),CurrentChunk))
                && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Max.X),(int)Math.Floor(playerHitBox.Min.Y+verticalSpeed*(float)gameTime.ElapsedGameTime.TotalMilliseconds),(int)Math.Floor(playerHitBox.Max.Z),CurrentChunk))){
                    camPosition.Y += verticalSpeed*(float)gameTime.ElapsedGameTime.TotalMilliseconds;
                    CanJump = false;
                    if(currentWorld.GetBlock((int)Math.Floor(camPosition.X),(int)Math.Floor(playerHitBox.Min.Y),(int)Math.Floor(camPosition.Z),CurrentChunk)==14){
                        verticalSpeed -= 0.0000015f*(float)gameTime.ElapsedGameTime.TotalMilliseconds;
                    }
                    else{
                        verticalSpeed -= 0.000015f*(float)gameTime.ElapsedGameTime.TotalMilliseconds;
                    }
                }
                else{
                    camPosition.Y = (float)Math.Round((float)Math.Floor(playerHitBox.Min.Y)+1.7f,2);
                    CanJump = true;
                    verticalSpeed = 0;
                }
            }
            else{
                if(Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Min.X),(int)(Math.Floor(playerHitBox.Max.Y+0.13f)+verticalSpeed*(float)gameTime.ElapsedGameTime.TotalMilliseconds),(int)Math.Floor(playerHitBox.Min.Z),CurrentChunk))
                && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Max.X),(int)(Math.Floor(playerHitBox.Max.Y+0.13f)+verticalSpeed*(float)gameTime.ElapsedGameTime.TotalMilliseconds),(int)Math.Floor(playerHitBox.Min.Z),CurrentChunk))
                && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Min.X),(int)(Math.Floor(playerHitBox.Max.Y+0.13f)+verticalSpeed*(float)gameTime.ElapsedGameTime.TotalMilliseconds),(int)Math.Floor(playerHitBox.Max.Z),CurrentChunk))
                && Block.IsNotSolid(currentWorld.GetBlock((int)Math.Floor(playerHitBox.Max.X),(int)(Math.Floor(playerHitBox.Max.Y+0.13f)+verticalSpeed*(float)gameTime.ElapsedGameTime.TotalMilliseconds),(int)Math.Floor(playerHitBox.Max.Z),CurrentChunk))){
                    camPosition.Y += verticalSpeed*(float)gameTime.ElapsedGameTime.TotalMilliseconds;
                    CanJump = false;
                    if(currentWorld.GetBlock((int)Math.Floor(camPosition.X),(int)Math.Floor(playerHitBox.Min.Y),(int)Math.Floor(camPosition.Z),CurrentChunk)==14){
                        verticalSpeed -= 0.0000015f*(float)gameTime.ElapsedGameTime.TotalMilliseconds;
                    }
                    else{
                        verticalSpeed -= 0.000015f*(float)gameTime.ElapsedGameTime.TotalMilliseconds;
                    }
                }
                else{
                    verticalSpeed = 0;
                }
            }
            ResetCamera();
            playerHitBox.Min.Y = (float)Math.Round(camPosition.Y-1.6999f,2);
            playerHitBox.Max.Y = (float)Math.Round(camPosition.Y+0.0999f,2);
            if(currentWorld.GetBlock((int)Math.Floor(camPosition.X),(int)Math.Floor(camPosition.Y),(int)Math.Floor(camPosition.Z),CurrentChunk)==14){
                IsUnderWater = true;
            }
            else{
                IsUnderWater = false;
            }
        }
        public void ResetPlayerSpeed(){
            verticalSpeed = 0;
        }
        void ResetCamera(){
            if(camPosition.X>64f){
                camPosition.X-=64f;
                CurrentChunk.x+=1;
                playerHitBox.Min.X = camPosition.X-0.2499f;
                playerHitBox.Max.X = camPosition.X+0.2499f;
                ChunkChanged = true;
                
            }
            if(camPosition.X<0f){
                camPosition.X+=64f;
                CurrentChunk.x-=1;
                playerHitBox.Min.X = camPosition.X-0.2499f;
                playerHitBox.Max.X = camPosition.X+0.2499f;
                ChunkChanged = true;
            }
            if(camPosition.Z>64f){
                camPosition.Z-=64f;
                CurrentChunk.z+=1;
                playerHitBox.Min.Z = camPosition.Z-0.2499f;
                playerHitBox.Max.Z = camPosition.Z+0.2499f;
                ChunkChanged = true;

            }
            if(camPosition.Z<0f){
                camPosition.Z+=64f;
                CurrentChunk.z-=1;
                playerHitBox.Min.Z = camPosition.Z-0.2499f;
                playerHitBox.Max.Z = camPosition.Z+0.2499f;
                ChunkChanged = true;
            }
            if(camPosition.Y>64f){
                camPosition.Y-=64f;
                CurrentChunk.y+=1;
            }
            if(camPosition.Y<0f){
                camPosition.Y+=64f;
                CurrentChunk.y-=1;
            }
        }
    }
}