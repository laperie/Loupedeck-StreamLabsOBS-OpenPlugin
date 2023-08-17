namespace Loupedeck.StreamlabsPlugin
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    internal partial class AppProxy
    {
        // Note.  Transition is also covered in this module
        public event EventHandler<EventArgs> AppEvtStudioModeOn;

        public event EventHandler<EventArgs> AppEvtStudioModeOff;

        public void AppRunTransition()
        {
            if (this.IsAppConnected && this._studioMode)
            {
                if (Helpers.TryExecuteSafe(() => true /* this.TransitionToProgram()*/))
                {
                    this.Plugin.Log.Info("Transition executed successfully");
                }
                else
                {
                    this.Plugin.Log.Warning("Cannot run transition");
                }
            }
        }

        public void AppToggleStudioMode() => this.SafeRunConnected(() => this.ToggleStudioMode(), "Cannot toggle studio mode");

        public void AppStartStudioMode() => this.SafeRunConnected(() => this.EnableStudioMode(), "Cannot start studio mode");

        public void AppStopStudioMode() => this.SafeRunConnected(() => this.DisableStudioMode(), "Cannot stop studio mode");

        void ToggleStudioMode() { }
        void EnableStudioMode() { }
        void DisableStudioMode() { }


        // Caching studio mode
        private Boolean _studioMode = false;

        private void OnObsStudioModeStateChange(Object sender, Boolean enabled)
        {
            this.Plugin.Log.Info($"OBS StudioMode State change, enabled={enabled}");
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


    }
}
