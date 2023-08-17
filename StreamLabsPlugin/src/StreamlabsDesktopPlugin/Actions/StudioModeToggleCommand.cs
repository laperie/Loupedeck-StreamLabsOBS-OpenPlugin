namespace Loupedeck.StreamlabsPlugin.Actions
{
    using System;

    internal class StudioModeToggleCommand : GenericOnOffSwitch
    {
        public StudioModeToggleCommand()
                  : base(
                      name: "StudioMode",
                      displayName: "Studio Mode Toggle",
                      description: "Switches the OBS Studio Mode on/off, allowing you to change and edit Scenes in the background",
                      groupName: "",
                      offStateName: "Enable Studio Mode",
                      onStateName: "Disable Studio Mode",
                      offStateImage: "STREAM_EnableStudioMode.png",
                      onStateImage: "STREAM_DisableStudioMode2.png")
        {
        }

        protected override void ConnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            StreamlabsPlugin.Proxy.AppEvtStudioModeOff += eventSwitchedOn;
            StreamlabsPlugin.Proxy.AppEvtStudioModeOn += eventSwitchedOff;
        }

        protected override void DisconnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            StreamlabsPlugin.Proxy.AppEvtStudioModeOff -= eventSwitchedOn;
            StreamlabsPlugin.Proxy.AppEvtStudioModeOn -= eventSwitchedOff;
        }

        
        protected override void RunCommand(TwoStateCommand command)
        {
            switch (command)
            {
                case TwoStateCommand.TurnOff:
                    StreamlabsPlugin.Proxy.AppStopStudioMode();
                    break;

                case TwoStateCommand.TurnOn:
                    StreamlabsPlugin.Proxy.AppStartStudioMode();
                    break;

                case TwoStateCommand.Toggle:
                    StreamlabsPlugin.Proxy.AppToggleStudioMode();
                    break;
            }
        }
    }
}
