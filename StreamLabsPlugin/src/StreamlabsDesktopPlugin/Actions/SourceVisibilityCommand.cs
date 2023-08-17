namespace Loupedeck.StreamlabsPlugin.Actions
{
    using System;

    internal class SourceVisibilityCommand : PluginMultistateDynamicCommand
    {
        public const String IMGSceneSelected = "SourceOn.png";
        public const String IMGSceneUnselected = "SourceOff.png";
        public const String IMGSceneInaccessible = "SourceInaccessible.png";
        public const String SourceNameUnknown = "Offline";

        public SourceVisibilityCommand()
        {
            this.Description = "Shows/Hides a Source";
            this.GroupName = "2. Sources";
            _ = this.AddState("Hidden", "Source hidden");
            _ = this.AddState("Visible", "Source visible");
        }

        protected override Boolean OnLoad()
        {
            this.IsEnabled = false;

            StreamlabsPlugin.Proxy.AppConnected += this.OnAppConnected;
            StreamlabsPlugin.Proxy.AppDisconnected += this.OnAppDisconnected;

            StreamlabsPlugin.Proxy.AppEvtSceneListChanged += this.OnSceneListChanged;
            StreamlabsPlugin.Proxy.AppEvtCurrentSceneChanged += this.OnCurrentSceneChanged;

            StreamlabsPlugin.Proxy.AppEvtSceneItemVisibilityChanged += this.OnSceneItemVisibilityChanged;

            StreamlabsPlugin.Proxy.AppEvtSceneItemAdded += this.OnSceneItemAdded;
            StreamlabsPlugin.Proxy.AppEvtSceneItemRemoved += this.OnSceneItemRemoved;

            this.OnAppDisconnected(this, null);

            return true;
        }

        protected override Boolean OnUnload()
        {
            StreamlabsPlugin.Proxy.AppConnected -= this.OnAppConnected;
            StreamlabsPlugin.Proxy.AppDisconnected -= this.OnAppDisconnected;

            StreamlabsPlugin.Proxy.AppEvtSceneListChanged -= this.OnSceneListChanged;
            StreamlabsPlugin.Proxy.AppEvtCurrentSceneChanged -= this.OnCurrentSceneChanged;
            StreamlabsPlugin.Proxy.AppEvtSceneItemVisibilityChanged -= this.OnSceneItemVisibilityChanged;

            StreamlabsPlugin.Proxy.AppEvtSceneItemAdded -= this.OnSceneItemAdded;
            StreamlabsPlugin.Proxy.AppEvtSceneItemRemoved -= this.OnSceneItemRemoved;

            return true;
        }

        protected override void RunCommand(String actionParameter) => StreamlabsPlugin.Proxy.AppSceneItemVisibilityToggle(actionParameter);

        private void OnSceneListChanged(Object sender, EventArgs e) => this.ResetParameters(true);

        private void OnCurrentSceneChanged(Object sender, EventArgs e) => this.ActionImageChanged();

        private void OnSceneItemAdded(Object sender, TwoStringArgs arg)
        {
            this.AddSceneItemParameter(arg.Item1, arg.Item2);
            this.ParametersChanged();
        }

        private void OnSceneItemRemoved(Object sender, TwoStringArgs arg)
        {
            this.RemoveParameter(SceneItemKey.Encode(StreamlabsPlugin.Proxy.CurrentSceneCollection, arg.Item1, arg.Item2));
            this.ParametersChanged();
        }

        private void OnAppConnected(Object sender, EventArgs e) => this.IsEnabled = true;

        private void OnAppDisconnected(Object sender, EventArgs e)
        {
            this.IsEnabled = false;

            this.ResetParameters(false);
            this.ActionImageChanged();
        }

        protected void OnSceneItemVisibilityChanged(Object sender, SceneItemVisibilityChangedArgs arg)
        {
            var actionParameter = SceneItemKey.Encode(StreamlabsPlugin.Proxy.CurrentSceneCollection, arg.SceneName, arg.ItemName);
            _ = this.SetCurrentState(actionParameter, arg.Visible ? 1 : 0);
            this.ActionImageChanged(actionParameter);
        }

        protected override BitmapImage GetCommandImage(String actionParameter, Int32 stateIndex, PluginImageSize imageSize)
        {
            var sourceName = SourceNameUnknown;
            var imageName = IMGSceneInaccessible;
            if (SceneItemKey.TryParse(actionParameter, out var parsed))
            {
                sourceName = parsed.Source;
                imageName = parsed.Collection != StreamlabsPlugin.Proxy.CurrentSceneCollection
                    ? IMGSceneInaccessible
                    : stateIndex == 1 ? IMGSceneSelected : IMGSceneUnselected;
            }

            return (this.Plugin as StreamlabsPlugin).GetPluginCommandImage(imageSize, imageName, sourceName, imageName == IMGSceneSelected);
        }

        private void AddSceneItemParameter(String sceneName, String itemName)
        {
            var key = SceneItemKey.Encode(StreamlabsPlugin.Proxy.CurrentSceneCollection, sceneName, itemName);
            this.AddParameter(key, $"{itemName}", $"{this.GroupName}{CommonStrings.SubgroupSeparator}{sceneName}").Description = 
                        StreamlabsPlugin.Proxy.AllSceneItems[key].Visible ? "Hide" : "Show" + $" source \"{itemName}\" of scene \"{sceneName}\"";
            this.SetCurrentState(key, StreamlabsPlugin.Proxy.AllSceneItems[key].Visible ? 1 : 0);
        }

        private void ResetParameters(Boolean readContent)
        {
            this.RemoveAllParameters();

            if (readContent)
            {
                this.Plugin.Log.Info($"Adding {StreamlabsPlugin.Proxy.AllSceneItems?.Count} sources");

                foreach (var item in StreamlabsPlugin.Proxy.AllSceneItems)
                {
                    this.AddSceneItemParameter(item.Value.SceneName, item.Value.SourceName);
                }
            }

            this.ParametersChanged();
        }
    }
}
