namespace Loupedeck.StreamlabsPlugin.Actions
{
    using System;

    internal class ScreenshotCommand : PluginDynamicCommand
    {
        private const String IMGAction = "Workspaces.png";

        private const String InvalidScreenshotFolder = "Cannot find folder for screenshot saving, feature disabled";

        public ScreenshotCommand()
            : base(displayName: "Screenshot",
                   description: String.IsNullOrEmpty(AppProxy.ScreenshotsSavingPath) ? InvalidScreenshotFolder  : "Takes a screenshot of currently active scene and saves it to " + AppProxy.ScreenshotsSavingPath,
                   groupName: "") => this.Name = "Screenshot";

        protected override Boolean OnLoad()
        {
            StreamlabsPlugin.Proxy.AppConnected += this.OnAppConnected;
            StreamlabsPlugin.Proxy.AppDisconnected += this.OnAppDisconnected;
            this.IsEnabled = false;
            return true;
        }

        protected override Boolean OnUnload()
        {
            StreamlabsPlugin.Proxy.AppConnected -= this.OnAppConnected;
            StreamlabsPlugin.Proxy.AppDisconnected -= this.OnAppDisconnected;
            return true;
        }

        private void OnAppConnected(Object sender, EventArgs e) => this.IsEnabled = !String.IsNullOrEmpty(AppProxy.ScreenshotsSavingPath);

        private void OnAppDisconnected(Object sender, EventArgs e) => this.IsEnabled = false;

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize) => (this.Plugin as StreamlabsPlugin).GetPluginCommandImage(imageSize, IMGAction);

        protected override void RunCommand(String actionParameter) => StreamlabsPlugin.Proxy.AppTakeScreenshot();
    }
}
