﻿namespace Loupedeck.ObsPlugin
{
    using System;
    using System.Collections.Generic;

    using OBSWebsocketDotNet;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    public partial class ObsAppProxy
    {
        public event EventHandler<EventArgs> AppEvtReplayBufferOn;

        public event EventHandler<EventArgs> AppEvtReplayBufferOff;

        private void OnObsReplayBufferStateChange(OBSWebsocket sender, OBSWebsocketDotNet.Types.OutputState newState)
        {
            this.Trace($"OBS Replay buffer state change, new state {newState}");

            if ((newState == OBSWebsocketDotNet.Types.OutputState.Started) || (newState == OBSWebsocketDotNet.Types.OutputState.Starting))
            {
                this.AppEvtReplayBufferOn?.Invoke(this, new EventArgs());
            }
            else
            {
                this.AppEvtReplayBufferOff?.Invoke(this, new EventArgs());
            }
        }

        public void AppToggleReplayBuffer()
        {
            if (this.IsAppConnected)
            {
                if(!Helpers.TryExecuteSafe(() => this.ToggleReplayBuffer()))
                {
                    this.Trace("Warning: Cannot toggle replayBuffer");
                }
            }
        }
        public void AppSaveReplayBuffer()
        {
            if (this.IsAppConnected)
            {
                if (!Helpers.TryExecuteSafe(() => this.SaveReplayBuffer()))
                {
                    this.Trace("Warning: Cannot save replayBuffer");
                }
            }

        }

    }
}
