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
            if (this.IsAppConnected && this._currentStudioMode)
            {
                this.ExecuteSlobsMethodSync("executeStudioModeTransition", Constants.TransitionsService);
                this.Plugin.Log.Info("Transition executed successfully");
                //else
                //{
                //    this.Plugin.Log.Warning("Cannot run transition");
                //}
            }
        }

        public void AppToggleStudioMode() => this.SafeRunConnected(() => this.ToggleStudioMode(), "Cannot toggle studio mode");

        public void AppStartStudioMode() => this.SafeRunConnected(() => this.EnableStudioMode(), "Cannot start studio mode");

        public void AppStopStudioMode() => this.SafeRunConnected(() => this.DisableStudioMode(), "Cannot stop studio mode");

        void ToggleStudioMode()
        {
            if (this._currentStudioMode)
            {
                this.DisableStudioMode();
            }
            else
            {
                 this.EnableStudioMode();
            }
        }

        void EnableStudioMode() => this.ExecuteSlobsMethodSync("enableStudioMode", Constants.TransitionsService);
        void DisableStudioMode() => this.ExecuteSlobsMethodSync("disableStudioMode", Constants.TransitionsService);


        // Caching studio mode
        private Boolean _currentStudioMode = false;

        private void OnObsStudioModeStateChange(Object sender, BoolParamArgs arg)
        {
            this._currentStudioMode = arg.Value;
            this.Plugin.Log.Info($"OBS StudioMode Value change, enabled={this._currentStudioMode}");
           
            if (this._currentStudioMode)
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
