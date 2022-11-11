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
        //Note.  Transition is also covered in this module
        public event EventHandler<EventArgs> AppEvtStudioModeOn;

        public event EventHandler<EventArgs> AppEvtStudioModeOff;

        //Caching studio mode
        private Boolean _studioMode = false;

        private void OnObsStudioModeStateChange(Object sender, Boolean enabled)
        {
            this.Trace($"OBS StudioMode State change, enabled={enabled}");
            this._studioMode = enabled;
            if (enabled)
            {
                this.AppEvtStudioModeOn?.Invoke(this, new EventArgs());
            }
            else
            {
                this.AppEvtStudioModeOff?.Invoke(this, new EventArgs());
            }
        }

        public void AppRunTransition()
        {
           if(this.IsAppConnected && this._studioMode )
           {
                if( Helpers.TryExecuteSafe(() => { this.TransitionToProgram(); }))
                {
                    this.Trace("Transition executed successfully");
                }
                else
                {
                    this.Trace("Cannot run transition");
                }

            }
           
        }

        public void AppToggleStudioMode()
        {
            if (this.IsAppConnected)
            {
                this.Trace("Toggling studio mode");
                Helpers.TryExecuteSafe(() => this.ToggleStudioMode());
            }
        }
    }
}
