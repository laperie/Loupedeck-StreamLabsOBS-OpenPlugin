namespace Loupedeck.StreamlabsPlugin.Actions
{
    using System;

    internal class TransitionCommand : PluginDynamicCommand
    {
        private const String IMGAction = "STREAM_Transition.png";

        public TransitionCommand()
            : base(displayName: "Studio Mode Transition",
                   description: "Changes your preview in Studio Mode to the active program scene",
                   groupName: "") => this.Name = "Transition";

        protected override Boolean OnLoad()
        {
            StreamlabsPlugin.Proxy.AppConnected += this.OnAppConnected;
            StreamlabsPlugin.Proxy.AppDisconnected += this.OnAppDisconnected;

            StreamlabsPlugin.Proxy.AppEvtStudioModeOn += this.OnAppStudioModeOn;
            StreamlabsPlugin.Proxy.AppEvtStudioModeOff += this.OnAppStudioModeOff;
            this.IsEnabled = false;
            return true;
        }

        protected override Boolean OnUnload()
        {
            StreamlabsPlugin.Proxy.AppConnected -= this.OnAppConnected;
            StreamlabsPlugin.Proxy.AppDisconnected -= this.OnAppDisconnected;
            StreamlabsPlugin.Proxy.AppEvtStudioModeOn -= this.OnAppStudioModeOn;
            StreamlabsPlugin.Proxy.AppEvtStudioModeOff -= this.OnAppStudioModeOff;
            return true;
        }

        private void OnAppConnected(Object sender, EventArgs e) => this.OnAppStudioModeOff(sender, e);

        private void OnAppStudioModeOn(Object sender, EventArgs e) => this.IsEnabled = true;

        private void OnAppStudioModeOff(Object sender, EventArgs e) => this.IsEnabled = false;

        private void OnAppDisconnected(Object sender, EventArgs e) => this.OnAppStudioModeOff(sender, e);

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize) => (this.Plugin as StreamlabsPlugin).GetPluginCommandImage(imageSize, IMGAction);

        protected override void RunCommand(String actionParameter) => StreamlabsPlugin.Proxy.AppRunTransition();
    }
}
