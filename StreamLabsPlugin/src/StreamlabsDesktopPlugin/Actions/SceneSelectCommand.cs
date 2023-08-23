namespace Loupedeck.StreamlabsPlugin.Actions
{
    using System;

    internal class SceneSelectCommand : PluginMultistateDynamicCommand
    {
        public const String IMGSceneSelected = "SceneOn.png";
        public const String IMGSceneUnselected = "SceneOff.png";
        public const String IMGSceneInaccessible = "SceneInaccessible.png";
        public const String SceneNameUnknown = "Offline";

        public SceneSelectCommand()
        {
            this.Description = "Switches to a specific scene in Streamlabs Desktop";
            this.GroupName = "1. Scenes";
            _ = this.AddState("Unselected", "Scene unselected");
            _ = this.AddState("Selected", "Scene selected");
        }

        protected override Boolean OnLoad()
        {
            this.IsEnabled = false;

            StreamlabsPlugin.Proxy.AppConnected += this.OnAppConnected;
            StreamlabsPlugin.Proxy.AppDisconnected += this.OnAppDisconnected;

            StreamlabsPlugin.Proxy.AppEvtSceneListChanged += this.OnSceneListChanged;
            StreamlabsPlugin.Proxy.AppEvtCurrentSceneChanged += this.OnCurrentSceneChanged;

            this.OnAppDisconnected(this, null);

            return true;
        }

        protected override Boolean OnUnload()
        {
            StreamlabsPlugin.Proxy.AppConnected -= this.OnAppConnected;
            StreamlabsPlugin.Proxy.AppDisconnected -= this.OnAppDisconnected;

            StreamlabsPlugin.Proxy.AppEvtSceneListChanged -= this.OnSceneListChanged;
            StreamlabsPlugin.Proxy.AppEvtCurrentSceneChanged -= this.OnCurrentSceneChanged;

            return true;
        }

        protected override void RunCommand(String actionParameter)
        {
            if (SceneKey.TryParse(actionParameter, out var key))
            {
                StreamlabsPlugin.Proxy.AppSwitchToScene(key.Scene);
            }
        }

        private void ResetParameters(Boolean readContent)
        {
            this.RemoveAllParameters();

            if (readContent)
            {
                this.Plugin.Log.Info($"Adding {StreamlabsPlugin.Proxy.Scenes?.Count} scene items");
                foreach (var scene in StreamlabsPlugin.Proxy.Scenes)
                {
                    var key = SceneKey.Encode(StreamlabsPlugin.Proxy.CurrentSceneCollection, scene.Name);
                    this.AddParameter(key, scene.Name, this.GroupName).Description=$"Switch to scene \"{scene.Name}\"";
                    this.SetCurrentState(key, scene.Name.Equals(StreamlabsPlugin.Proxy.CurrentScene?.Name) ? 1 : 0);
                }
            }

            this.ParametersChanged();
        }

        private void OnSceneListChanged(Object sender, EventArgs e) =>
            this.ResetParameters(true);

        private void OnCurrentSceneChanged(Object sender, OldNewStringChangeEventArgs arg)
        {
            var oldPar = SceneKey.Encode(StreamlabsPlugin.Proxy.CurrentSceneCollection, arg.Old);
            var newPar = SceneKey.Encode(StreamlabsPlugin.Proxy.CurrentSceneCollection, arg.New);

            //unselecting old and selecting new
            this.SetCurrentState(oldPar, 0);
            this.SetCurrentState(newPar, 1);

            this.ActionImageChanged(oldPar);
            this.ActionImageChanged(newPar);

            this.ParametersChanged();
        }

        private void OnAppConnected(Object sender, EventArgs e) => this.IsEnabled = true;

        private void OnAppDisconnected(Object sender, EventArgs e)
        {
            this.IsEnabled = false;
            this.ResetParameters(false);
            this.ActionImageChanged();
        }

        protected override BitmapImage GetCommandImage(String actionParameter, Int32 stateIndex, PluginImageSize imageSize)
        {
            var imageName = IMGSceneInaccessible;
            var sceneName = SceneNameUnknown;

            if (SceneKey.TryParse(actionParameter, out var parsed))
            {
                sceneName = parsed.Scene;

                if( StreamlabsPlugin.Proxy.TryGetSceneByName(parsed.Scene, out var _) )
                {
                    imageName = stateIndex == 1 ? IMGSceneSelected : IMGSceneUnselected;
                }
            }            
            return (this.Plugin as StreamlabsPlugin).GetPluginCommandImage(imageSize, imageName, sceneName, imageName == IMGSceneSelected);
        }
    }
}
