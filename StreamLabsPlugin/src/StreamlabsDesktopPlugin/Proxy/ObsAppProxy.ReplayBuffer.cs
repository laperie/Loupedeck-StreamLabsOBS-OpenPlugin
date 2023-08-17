namespace Loupedeck.StreamlabsPlugin
{
    using System;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    internal partial class AppProxy
    {
        public event EventHandler<EventArgs> AppEvtReplayBufferOn;

        public event EventHandler<EventArgs> AppEvtReplayBufferOff;

        
        public void AppToggleReplayBuffer() => this.SafeRunConnected(() => this.ToggleReplayBuffer(), "Cannot toggle Replay Buffer");

        public void AppStartReplayBuffer() => this.SafeRunConnected(() => this.StartReplayBuffer(), "Cannot start Replay Buffer");

        public void AppStopReplayBuffer() => this.SafeRunConnected(() => this.StopReplayBuffer(), "Cannot stop Replay Buffer");

        public void AppSaveReplayBuffer()
        {
            if (this.IsAppConnected)
            {
                if (!Helpers.TryExecuteSafe(() => this.SaveReplayBuffer()))
                {
                    this.Plugin.Log.Warning("Cannot save replayBuffer");
                }
            }
        }

        private void SaveReplayBuffer() => this.ExecuteSlobsMethodSync("saveReplay", Constants.StreamingService);
        private void ToggleReplayBuffer()
        {
            if (this._currentReplayBuferStatus == StreamlabsReplayBufferStatus.Running)
            {
                this.StopReplayBuffer();
            }
            else if (this._currentReplayBuferStatus == StreamlabsReplayBufferStatus.Offline)
            {
                this.StartReplayBuffer();
            }
        }
                  
        private void StopReplayBuffer() => this.ExecuteSlobsMethodSync("stopReplayBuffer", Constants.StreamingService);
        private void StartReplayBuffer() => this.ExecuteSlobsMethodSync("startReplayBuffer", Constants.StreamingService);

        private StreamlabsReplayBufferStatus _currentReplayBuferStatus = StreamlabsReplayBufferStatus.NONE;
        private void OnObsReplayBufferStateChange(Object sender, ReplayBufferEventArgs arg)
        {
            this._currentReplayBuferStatus = arg.Value;

            this.Plugin.Log.Info($"OBS Replay buffer state change, new state {this._currentReplayBuferStatus}");
            
            if (this._currentReplayBuferStatus == StreamlabsReplayBufferStatus.Running)
            {
                this.AppEvtReplayBufferOn?.Invoke(this, new EventArgs());
            }
            else
            {
                this.AppEvtReplayBufferOff?.Invoke(this, new EventArgs());
            }
        }

    }
}
