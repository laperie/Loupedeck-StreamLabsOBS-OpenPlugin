namespace Loupedeck.StreamlabsPlugin.Actions
{
    using System;

    internal class RecordingToggleCommand : GenericOnOffSwitch
    {
        public RecordingToggleCommand()
                        : base(
                            name: "ToggleRecording",
                            displayName: "Recording Toggle",
                            description: "Toggles Recording on or off",
                            groupName: "",
                            offStateName: "Start Recording",
                            onStateName: "Stop Recording",
                            offStateImage: "STREAM_ToggleRecord1.png",
                            onStateImage: "STREAM_ToggleRecord2.png")
        {
        }

        protected override void ConnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            StreamlabsPlugin.Proxy.AppEvtRecordingOff += eventSwitchedOn;
            StreamlabsPlugin.Proxy.AppEvtRecordingOn += eventSwitchedOff;
        }

        protected override void DisconnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            StreamlabsPlugin.Proxy.AppEvtRecordingOff -= eventSwitchedOn;
            StreamlabsPlugin.Proxy.AppEvtRecordingOn -= eventSwitchedOff;
        }

        protected override void RunCommand(TwoStateCommand command)
        {
            switch (command)
            {
                case TwoStateCommand.TurnOff:
                    StreamlabsPlugin.Proxy.AppStopRecording();
                    break;

                case TwoStateCommand.TurnOn:
                    StreamlabsPlugin.Proxy.AppStartRecording();
                    break;

                case TwoStateCommand.Toggle:
                    StreamlabsPlugin.Proxy.AppToggleRecording();
                    break;
            }
        }

    }
}
