﻿namespace Loupedeck.StreamlabsPlugin.Actions
{
    using System;
    using System.Collections.Generic;

    internal class SourceVolumeAdjustment : PluginDynamicAdjustment
    {
        private const String IMGSourceSelected = "AudioOn.png";
        private const String IMGSourceUnselected = "AudioOff.png";
        private const String IMGSourceInaccessible = "AudioInaccessible.png";
        private const String SourceNameUnknown = "Offline";

        // private const String SpecialSourceGroupName = "General Audio";

        public SourceVolumeAdjustment()
            : base(false)
        {
            this.DisplayName = "Volume Mixer";
            this.Description = "Controls the volume of the audio sources in Streamlabs Desktop";
            this.GroupName = "3. Audio";
        }

        protected override Boolean OnLoad()
        {
            this.IsEnabled = false;
            StreamlabsPlugin.Proxy.AppConnected += this.OnAppConnected;
            StreamlabsPlugin.Proxy.AppDisconnected += this.OnAppDisconnected;

            StreamlabsPlugin.Proxy.AppEvtSceneListChanged += this.OnSceneListChanged;
            StreamlabsPlugin.Proxy.AppEvtCurrentSceneChanged += this.OnCurrentSceneChanged;

            StreamlabsPlugin.Proxy.AppEvtSourceMuteStateChanged += this.OnSourceMuteStateChanged;
            StreamlabsPlugin.Proxy.AppEvtSourceVolumeChanged += this.OnSourceVolumeChanged;

            StreamlabsPlugin.Proxy.AppSourceCreated += this.OnSourceCreated;
            StreamlabsPlugin.Proxy.AppSourceDestroyed += this.OnSourceDestroyed;

            this.OnAppDisconnected(this, null);

            return true;
        }

        //FIXME:  When flipping between scene collections,  the icons for adjustments are updated with delay
        protected override Boolean OnUnload()
        {
            StreamlabsPlugin.Proxy.AppConnected -= this.OnAppConnected;
            StreamlabsPlugin.Proxy.AppDisconnected -= this.OnAppDisconnected;

            StreamlabsPlugin.Proxy.AppEvtSceneListChanged -= this.OnSceneListChanged;
            StreamlabsPlugin.Proxy.AppEvtCurrentSceneChanged -= this.OnCurrentSceneChanged;

            StreamlabsPlugin.Proxy.AppEvtSourceMuteStateChanged -= this.OnSourceMuteStateChanged;
            StreamlabsPlugin.Proxy.AppEvtSourceVolumeChanged -= this.OnSourceVolumeChanged;

            StreamlabsPlugin.Proxy.AppSourceCreated -= this.OnSourceCreated;
            StreamlabsPlugin.Proxy.AppSourceDestroyed -= this.OnSourceDestroyed;

            return true;
        }

        protected override String GetAdjustmentDisplayName(String actionParameter, PluginImageSize imageSize) => SceneItemKey.TryParse(actionParameter, out var key) ? key.Source : SourceNameUnknown;

        protected override void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            if (SceneKey.TryParse(actionParameter, out var key))
            {
                StreamlabsPlugin.Proxy.AppSetVolume(key.Source, diff);
            }

            this.AdjustmentValueChanged();
        }

        protected override void RunCommand(String actionParameter)
        {
            if (SceneKey.TryParse(actionParameter, out var key))
            {
                // Pressing the button toggles mute
                StreamlabsPlugin.Proxy.AppToggleMute(key.Source);
            }
            else
            {
                this.Plugin.Log.Info($"Warning: Cannot  parse actionParameter {actionParameter}");
            }
        }

        protected override String GetAdjustmentValue(String actionParameter) 
        {
            return StreamlabsPlugin.Proxy.AppGetVolumeLabel(SceneKey.TryParse(actionParameter, out var key) ? key.Source : "N/A");
        }
        private void OnSourceMuteStateChanged(Object sender, MuteEventArgs args)
        {
            var actionParameter = SceneKey.Encode(StreamlabsPlugin.Proxy.CurrentSceneCollection, args.SourceName);

            // FIXME: Check if this 'has parameter' check is needed.
            if (this.TryGetParameter(actionParameter, out _) && this._muteStates.ContainsKey(actionParameter))
            {
                this._muteStates[actionParameter] = args.isMuted;

                this.ActionImageChanged(actionParameter);
            }
        }

        private void OnSceneListChanged(Object sender, EventArgs e) => this.ResetParameters(true);

        private void OnCurrentSceneChanged(Object sender, EventArgs e) => this.ActionImageChanged();

        private void OnAppConnected(Object sender, EventArgs e) => this.IsEnabled = true; // We expect to get SceneCollectionChange so doin' nothin' here.

        private void OnAppDisconnected(Object sender, EventArgs e)
        {
            this.IsEnabled = false;
            this.ResetParameters(false);
            this.ActionImageChanged();
        }

        private void OnSourceVolumeChanged(Object sender, VolumeEventArgs args)
        {
            var actionParameter = SceneKey.Encode(StreamlabsPlugin.Proxy.CurrentSceneCollection, args.SourceName);
            this.AdjustmentValueChanged(actionParameter);
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var sourceName = SourceNameUnknown;
            var imageName = IMGSourceInaccessible;
            var selected = false;
            if (SceneKey.TryParse(actionParameter, out var parsed) && this._muteStates.ContainsKey(actionParameter))
            {
                sourceName = parsed.Source;
                selected = (parsed.Collection == StreamlabsPlugin.Proxy.CurrentSceneCollection) && this._muteStates[actionParameter];
                imageName = parsed.Collection != StreamlabsPlugin.Proxy.CurrentSceneCollection ? IMGSourceInaccessible : this._muteStates[actionParameter] ? IMGSourceUnselected : IMGSourceSelected;
            }

            return (this.Plugin as StreamlabsPlugin).GetPluginCommandImage(imageSize, imageName, sourceName, !selected);
        }

        private void OnSourceCreated(Object sender, SourceNameEventArgs args)
        {
            this.AddSource(args.SourceName);
            this.ParametersChanged();
        }

        private void OnSourceDestroyed(Object sender, SourceNameEventArgs args)
        {
            var key = SceneKey.Encode(StreamlabsPlugin.Proxy.CurrentSceneCollection, args.SourceName);

            if (this.TryGetParameter(key, out _))
            {
                this.RemoveParameter(key);
                _ = this._muteStates.Remove(key);
                this.ParametersChanged();
            }
        }

        // Instead of Value for Multistate actions, we will hold the mute state here
        private readonly Dictionary<String, Boolean> _muteStates = new Dictionary<String, Boolean>();

        private void AddSource(String sourceName, Boolean isSpecialSource = false)
        {
            var key = SceneKey.Encode(StreamlabsPlugin.Proxy.CurrentSceneCollection, sourceName);
            var displayName = sourceName + (isSpecialSource ? "(G)" : "");
            this.AddParameter(key, displayName, this.GroupName).Description = $"Control volume of audio source \"{sourceName}\"";

            // Moving to same group this.AddParameter(key, $"{sourceName}", isSpecialSource ? SpecialSourceGroupName : this.GroupName);
            this._muteStates[key] = StreamlabsPlugin.Proxy.AppGetMute(sourceName);
        }

        private void ResetParameters(Boolean readContent)
        {
            this.RemoveAllParameters();
            this._muteStates.Clear();
            if (readContent)
            {
                foreach (var item in StreamlabsPlugin.Proxy.CurrentAudioSources)
                {
                    this.AddSource(item.Key, item.Value.SpecialSource);
                }
            }

            this.ParametersChanged();
        }
    }
}
