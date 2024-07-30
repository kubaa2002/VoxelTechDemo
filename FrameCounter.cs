using System;
using System.Collections.Generic;
using System.Linq;
public class FrameCounter{
    public double AverageFramesPerSecond { get; private set; }
    private readonly Queue<double> _sampleBuffer = new();
    public FrameCounter(){
        for(int i=0;i<10;i++){
            _sampleBuffer.Enqueue(0);
        }
    }
    public void Update(double deltaTime){
        _sampleBuffer.Dequeue();
        _sampleBuffer.Enqueue(deltaTime);
        AverageFramesPerSecond = Math.Round(1.0d/_sampleBuffer.Average(),2);
    }
}