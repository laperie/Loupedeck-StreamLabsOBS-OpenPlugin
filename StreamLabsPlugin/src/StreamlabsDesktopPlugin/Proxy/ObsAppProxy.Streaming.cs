namespace Loupedeck.StreamlabsPlugin
{
    using System;

    using SLOBSharp.Client.Responses;


    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    internal partial class AppProxy
    {
        // STREAMING TOGGLE
        public event EventHandler<EventArgs> AppEvtStreamingOn;

        public event EventHandler<EventArgs> AppEvtStreamingOff;

        public void AppToggleStreaming()
        { 
            if (!this.StreamingStateChangeIsInProgress())
            {
                this.SafeRunConnected(() => this.ToggleStreaming(), "Cannot toggle streaming");
            }
        }

        public void AppStartStreaming() 
        { 
            if (!this.StreamingStateChangeIsInProgress())
            {
                this.SafeRunConnected(() => this.StartStreaming(), "Cannot start streaming");
            }
        }

        public void AppStopStreaming()
        {
            if (!this.StreamingStateChangeIsInProgress())
            {
                this.SafeRunConnected(() => this.StopStreaming(), "Cannot stop streaming");
            }
        }

        void StartStreaming() 
        { 
            if(this._currentSlobsStreamingState == StreamlabsStreamingStatus.Offline)
            {
                this.ToggleStreaming();
            }
        }
        void StopStreaming()
        {
            if (this._currentSlobsStreamingState == StreamlabsStreamingStatus.Live)
            {
                this.ToggleStreaming();
            }
        }

        private void ToggleStreaming() => this.ExecuteSlobsMethodSync("toggleStreaming", Constants.StreamingService);

        private Boolean StreamingStateChangeIsInProgress() => false; //this._currentSlobsState == OBSWebsocketDotNet.Types.OutputState.Starting || this._currentSlobsState == OBSWebsocketDotNet.Types.OutputState.Stopping;

        private StreamlabsStreamingStatus _currentSlobsStreamingState = StreamlabsStreamingStatus.NONE;

        private void OnObsStreamingStateChange(Object sender, StreamingStateArgs v)
        {
            var newState = v.Value;
            this.Plugin.Log.Info($"OBS StreamingStateChange, new state {newState}");

            this._currentSlobsStreamingState = newState;

            if ((newState == StreamlabsStreamingStatus.Live) || (newState == StreamlabsStreamingStatus.Starting))
            {
                this.AppEvtStreamingOn?.Invoke(this, new EventArgs());
            }
            else
            {
                this.AppEvtStreamingOff?.Invoke(this, new EventArgs());
            }
        }

    }
}
