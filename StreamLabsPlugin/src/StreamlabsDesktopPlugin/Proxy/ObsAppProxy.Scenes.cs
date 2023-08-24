namespace Loupedeck.StreamlabsPlugin
{
    using System;
    using System.Collections.Generic;

    using SLOBSharp.Domain.Services;

    internal class LDSceneItem
    {
        public String SourceName { get; private set; }
        public LDSceneItem(String sourceName) => this.SourceName = sourceName; 
    }

    internal class Scene
    {
        public String Name { get; private set; }
        public  List<LDSceneItem> Items { get; private set; } 
    /*    public Scene(OBSScene scene)
        {
            this.Name = scene.Name;
            this.Items = scene.Items.ConvertAll(x => new LDSceneItem(x.SourceName));
        }
    */
        public Scene(String name="")
        {
            this.Name = name;
            this.Items = new List<LDSceneItem>();
        }
    }

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    internal partial class AppProxy
    {
        public event EventHandler<EventArgs> AppEvtSceneListChanged;

        public event EventHandler<OldNewStringChangeEventArgs> AppEvtCurrentSceneChanged;
        public Loupedeck.StreamlabsPlugin.Scene CurrentScene { get; private set; } = new Loupedeck.StreamlabsPlugin.Scene();

        public List<Loupedeck.StreamlabsPlugin.Scene> Scenes { get; private set; } = new List<Loupedeck.StreamlabsPlugin.Scene>();

        

        /// <summary>
        /// Attemts to get the Scene object for scene in current collection
        /// </summary>
        /// <param name="sceneName">Name of scene</param>
        /// <param name="scene">scene object</param>
        /// <returns>true if scene retreived</returns>
        public Boolean TryGetSceneByName(String sceneName, out Loupedeck.StreamlabsPlugin.Scene scene)
        {
            scene = null;
            if(!String.IsNullOrEmpty(sceneName))
            {
                scene = this.Scenes.Find(x => x.Name == sceneName);
            }
            return scene != null;
        }
        public void AppSwitchToScene(String newScene)
        {
            if (this.IsAppConnected && this.TryGetSceneByName(newScene, out var _))
            {
                this.Plugin.Log.Info($"Switching to scene {newScene}");

                Helpers.TryExecuteSafe(() =>
                {
                    if (this._currentStudioMode)
                    {
                        this.SetPreviewScene(newScene);
                    }
                    else
                    {
                        this.SetCurrentScene(newScene);
                    }
                });
            }
        }

        private void OnObsSceneListChanged(Object sender, EventArgs e)
        {
            // Rescan the scene list
            if (this.IsAppConnected && Helpers.TryExecuteFunc(() => this.GetSceneList(), out var scenesList))
            {

                this.Scenes = scenesList;

                this.Plugin.Log.Info($"SLOBS Rescanned scene list. Currently {this.Scenes?.Count} scenes in collection {this.CurrentSceneCollection} ");

                // Retreiving properties for all scenes
                this.OnObsSceneCollectionChange_FetchSceneItems();

                if (Helpers.TryExecuteFunc(() => this.GetCurrentScene(), out var scene))
                {
                    if (!String.IsNullOrEmpty(scene.Name) && !scene.Name.Equals(this.CurrentScene?.Name))
                    {
                        this.OnObsSceneChanged(e, new OneStringEventArgs(scene.Name));
                    }
                }
                else
                {
                    this.Plugin.Log.Warning("SceneListChanged: cannot fetch current scene");
                }
#if false
                this.OnObsSceneCollectionChanged_RetreiveAudioSources();
#endif
                this.AppEvtSceneListChanged?.Invoke(sender, e);
            }
            else
            {
                this.Plugin.Log.Warning("Cannot handle SceneListChanged event");
            }
        }

        private void OnSceneChanged(String newScene)
        {
            if (this.TryGetSceneByName(newScene, out var scene) && this.CurrentScene != scene)
            {
                this.Plugin.Log.Info($"OBS - Current scene changed from {this.CurrentScene?.Name} to {newScene}");
                var args = new OldNewStringChangeEventArgs(this.CurrentScene?.Name, scene.Name);
                this.CurrentScene = scene;
                this.AppEvtCurrentSceneChanged?.Invoke(this, args);
            }
            else
            {
                this.Plugin.Log.Warning($"Cannot find scene {newScene} in current collection {this.CurrentSceneCollection}");
            }
        }

        private void OnObsPreviewSceneChanged(Object sender, String newScene)
        {
            //Scenes and preview: When in the Studio mode
            // Scene A: Preview scene
            // Scene B: Program scene
            // Scene C: Some other scene
            //  On the device, scene B is scene. Scenes A and C are not selected
            //  If Scene C is pressed, it becomes new Preview scene but REMAINS UNSELECTED
            //  if Scene B is pressed, nothing happens 
            //  if Scene A is pressed, nothing happens

            // This the same as onObsSceneChanged but in previewmode
            // For Studio mode, OBS plugin will set / monitor Preview scenes, not main scene
            //  NB: the SceneChange command changes program scene, not a preview one
            if(this._currentStudioMode)
            {
                this.OnSceneChanged(newScene);
            }
            else
            {
                this.Plugin.Log.Info($"PreviewSceneChange to {newScene} but not in Studio mode, igrnoring");
            }
        }
        private void OnObsTransitionEnd(Object sender, String transitionName, String transitionType, Int32 duration, String toScene)
        {
            this.Plugin.Log.Info($"Transition {transitionName} to scene {toScene} ended");
            if (this._currentStudioMode)
            {
                //In studio mode (see above), the selected scene == Program scene
                this.OnSceneChanged(toScene);
            }
        }

        private void OnObsSceneChanged(Object sender, OneStringEventArgs arg)
        {
            if (!this._currentStudioMode)
            {
                this.OnSceneChanged(arg.Value);
            }
            else
            {
                //FIXME: FOR SLOBS, at least temporarily, we do the same 

                this.OnSceneChanged(arg.Value);
                //This is handled in OnObsTransitionEnd
                // -> ???this.Plugin.Log.Info($"OnObsSceneChanged to {arg.Value} ignoring in Studio mode");
            }
        }

    }
}
