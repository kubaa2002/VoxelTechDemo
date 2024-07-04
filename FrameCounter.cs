using System;
using System.Collections.Generic;
using System.Linq;
public class FrameCounter{
    public float AverageFramesPerSecond { get; private set; }
    private readonly Queue<float> _sampleBuffer = new();
    public FrameCounter(){
        for(int i=0;i<10;i++){
            _sampleBuffer.Enqueue(0);
        }
    }
    public void Update(float deltaTime){
        _sampleBuffer.Dequeue();
        _sampleBuffer.Enqueue(deltaTime);
        AverageFramesPerSecond = (float)Math.Round(1.0f/_sampleBuffer.Average(),2);
    }
}