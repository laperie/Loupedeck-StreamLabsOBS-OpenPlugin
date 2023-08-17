namespace Loupedeck.StreamlabsPlugin
{
    using System;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    internal partial class ObsAppProxy
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

        private void SaveReplayBuffer() {}
        private void  ToggleReplayBuffer() {}
        private void StopReplayBuffer() { }
        private void StartReplayBuffer() { }
#if false
        private void OnObsReplayBufferStateChange(OBSWebsocket sender, OBSWebsocketDotNet.Types.OutputState newState)
        {
            this.Plugin.Log.Info($"OBS Replay buffer state change, new state {newState}");

            if ((newState == OBSWebsocketDotNet.Types.OutputState.Started) || (newState == OBSWebsocketDotNet.Types.OutputState.Starting))
            {
                this.AppEvtReplayBufferOn?.Invoke(this, new EventArgs());
            }
            else
            {
                this.AppEvtReplayBufferOff?.Invoke(this, new EventArgs());
            }
        }
#endif
    }
}
