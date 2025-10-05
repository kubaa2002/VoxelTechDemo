using System.Collections.Generic;
using System.IO;

namespace VoxelTechDemo;
static class UserSettings{
    private static byte renderDistance = 3;
    private static float mouseSensitivity = 0.005f;
    private static float fieldOfView = 45f;
    private static bool fogEnabled = true;
    private static bool frameRateUnlocked = false;
    private static bool fullscreen = true;
    private static bool dayCycle = true;
    private static bool cloudsEnabled = true;

    private static bool needUpdate = false;

    public static byte RenderDistance{
        get => renderDistance;
        set{
            if(value != renderDistance){
                renderDistance = value;
                needUpdate = true;
            }
        }
    }
    public static float MouseSensitivity{
        get => mouseSensitivity;
        set{
            if(value != mouseSensitivity){
                mouseSensitivity = value;
                needUpdate = true;
            }
        }
    }
    public static float FieldOfView{
        get => fieldOfView;
        set{
            if(value != fieldOfView){
                fieldOfView = value;
                needUpdate = true;
            }
        }
    }
    public static bool FogEnabled{
        get => fogEnabled;
        set{
            if(value != fogEnabled){
                fogEnabled = value;
                needUpdate = true;
            }
        }
    }
    public static bool FrameRateUnlocked{
        get => frameRateUnlocked;
        set{
            if(value != frameRateUnlocked){
                frameRateUnlocked = value;
                needUpdate = true;
            }
        }
    }
    public static bool Fullscreen {
        get => fullscreen;
        set{
            if (value != fullscreen){
                fullscreen = value;
                needUpdate = true;
            }
        }
    }
    public static bool DayCycle {
        get => dayCycle;
        set {
            if (value != dayCycle) {
                dayCycle = value;
                needUpdate = true;
            }
        }
    }

    public static bool CloudsEnabled {
        get => cloudsEnabled;
        set {
            if (value != cloudsEnabled) {
                cloudsEnabled = value;
                needUpdate = true;
            }
        }
    }

    public static void LoadSettings(){
        if(File.Exists("Settings.txt")){
            string[] lines = File.ReadAllLines("Settings.txt");
            Dictionary<string,string> variables = [];
            foreach(string a in lines){
                string[] split = a.Split("=");
                if(split.Length == 2){
                    variables[split[0]]=split[1];
                }
            }

            if(variables.TryGetValue("RenderDistance", out string value)){
                if(byte.TryParse(value, out byte result)){
                    if(result >= 1 && result <= 32){
                        renderDistance = result;
                    }
                }
            }
            if(variables.TryGetValue("MouseSensitivity", out value)){
                if(float.TryParse(value, out float result)){
                    if(result >= 0.001f && result <= 0.01f){
                        mouseSensitivity = result;
                    }
                }
            }
            if(variables.TryGetValue("FieldOfView", out value)){
                if(float.TryParse(value, out float result)){
                    if(result >= 30 && result <= 120){
                        fieldOfView = result;
                    }
                }
            }
            if(variables.TryGetValue("FogEnabled", out value)){
                if(bool.TryParse(value, out bool result)){
                    fogEnabled = result;
                }
            }
            if(variables.TryGetValue("FrameRateUnlocked", out value)){
                if(bool.TryParse(value, out bool result)){
                    frameRateUnlocked = result;
                }
            }
            if(variables.TryGetValue("Fullscreen", out value)) {
                if(bool.TryParse(value, out bool result)) {
                    fullscreen = result;
                }
            }
            if (variables.TryGetValue("DayCycle", out value)) {
                if (bool.TryParse(value, out bool result)) {
                    dayCycle = result;
                }
            }
            if (variables.TryGetValue("CloudsEnabled", out value)) {
                if (bool.TryParse(value, out bool result)) {
                    cloudsEnabled = result;
                }
            }
        }
        else{
            UpdateSettingsFile();
        }
    }
    public static void CheckSettingsFile(){
        if(needUpdate){
            needUpdate = false;
            UpdateSettingsFile();
        }
    }
    private static void UpdateSettingsFile(){
        using StreamWriter writer = new("Settings.txt");
        writer.WriteLine($"RenderDistance={renderDistance}");
        writer.WriteLine($"MouseSensitivity={mouseSensitivity}");
        writer.WriteLine($"FieldOfView={fieldOfView}");
        writer.WriteLine($"FogEnabled={fogEnabled}");
        writer.WriteLine($"FrameRateUnlocked={frameRateUnlocked}");
        writer.WriteLine($"Fullscreen={fullscreen}");
        writer.WriteLine($"DayCycle={dayCycle}");
        writer.WriteLine($"CloudsEnabled={cloudsEnabled}");
    }
}