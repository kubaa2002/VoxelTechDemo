using System.Collections.Generic;
using System.IO;

namespace VoxelTechDemo{
    static class UserSettings{
        static byte renderDistance = 3;
        static float mouseSensitivity = 0.005f;
        static float fieldOfView = 45f;
        static bool fogEnabled = true;
        static bool frameRateUnlocked = false;

        static bool needUpdate = false;

        public static byte RenderDistance{
                get{
                    return renderDistance;
                }
                set{
                    if(value != renderDistance){
                        renderDistance = value;
                        needUpdate = true;
                    }
                }
        }
        public static float MouseSensitivity{
                get{
                    return mouseSensitivity;
                }
                set{
                    if(value != mouseSensitivity){
                        mouseSensitivity = value;
                        needUpdate = true;
                    }
                }
        }
        public static float FieldOfView{
                get{
                    return fieldOfView;
                }
                set{
                    if(value != fieldOfView){
                        fieldOfView = value;
                        needUpdate = true;
                    }
                }
        }
        public static bool FogEnabled{
                get{
                    return fogEnabled;
                }
                set{
                    if(value != fogEnabled){
                        fogEnabled = value;
                        needUpdate = true;
                    }
                }
        }
        public static bool FrameRateUnlocked{
                get{
                    return frameRateUnlocked;
                }
                set{
                    if(value != frameRateUnlocked){
                        frameRateUnlocked = value;
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
        }
    }
}