using GHPC.State;
using MelonLoader;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TextureLoaderFork
{
    
    public class TextureLoaderForkClass : MelonMod
    {
        static string handlerInstalledForScene;
        const string notInstalled = "TextureLoaderHandlerNotInstalled";

        readonly string folderPath = "Mods/GMPCTextureLoader/";
        readonly string imageExtension = ".png";
    

        public override void OnInitializeMelon()
        {
            handlerInstalledForScene = notInstalled;

            Directory.CreateDirectory(folderPath);
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (handlerInstalledForScene != notInstalled)  return;
            else if (sceneName == "LOADER_INITIAL" || sceneName == "LOADER_MENU") return;
            else if (sceneName == "MainMenu2_Scene" || sceneName == "t64_menu" || sceneName == "MainMenu2-1_Scene")
            {
                LoggerInstance.Msg($"Patching textures in main menu {sceneName}.");
                MelonCoroutines.Start(LoadTextures(GameState.AppLoaded));
            }
            else
            {
                // Vehicles are loaded after OnSceneWasInitialized finishes, so we have to install an event to load the textures when everything is ready                
                var status = StateController.RunOrDefer(GameState.GameReady, new GameStateEventHandler(LoadTextures), GameStatePriority.Lowest);
                if (status != GameStateInvocationStatus.Fail)
                {
                    handlerInstalledForScene = sceneName;
                }
                LoggerInstance.Msg($"Trying to load replacement textures on scene {sceneName}, result: {status}");                
            }
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            if (handlerInstalledForScene != notInstalled)
            {
            //    LoggerInstance.Msg($"Texture loader handler is being unloaded together with scene {handlerInstalledForScene}, a new one can now be installed.");
                handlerInstalledForScene = notInstalled;
            }
        }

        IEnumerator LoadTextures(GameState state)
        {
            LoggerInstance.Msg($"GameState is {state}, loading texture replacements.");

            HashSet<string> replacements = Directory.GetFiles(folderPath, "*" + imageExtension).Select(p => Path.GetFileNameWithoutExtension(p)).ToHashSet();
            LoggerInstance.Msg($"Found {replacements.Count} *{imageExtension} texture replacements in \"{folderPath}\".");

            Texture2D[] textures = Resources.FindObjectsOfTypeAll<Texture2D>();
            LoggerInstance.Msg($"Found {textures.Length} Texture2Ds.");
            for (int i = 0; i < textures.Length; ++i)
            {
                Texture2D texture = textures[i];
                string texName = texture.name;
                if (replacements.Contains(texName))
                {
                    string filePath = folderPath + texName + imageExtension;
                    
                    LoggerInstance.Msg($"Reading replacement texture for \"{texName}\" from file...");
                    byte[] data = File.ReadAllBytes(filePath);

                        // This takes a while, but it only has to be done once per scene load
                    if (!texture.LoadImage(data, true))
                    {
                        LoggerInstance.Error("Failed to upload replacement texture into the GPU memory!");
                    }  
                }
            }
            yield break;
        }
    }
}
