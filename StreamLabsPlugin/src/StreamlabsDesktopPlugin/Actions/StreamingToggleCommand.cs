namespace Loupedeck.StreamlabsPlugin.Actions
{
    using System;

    internal class StreamingToggleCommand : GenericOnOffSwitch
    {
        // TODO: As needed, add handling for Starting and Stopping states
        public StreamingToggleCommand()
                     : base(
                         name: "ToggleStreaming",
                         displayName: "Streaming Toggle",
                         description: "Starts/Stops a livestream in OBS Studio",
                         groupName: "",
                         offStateName: "Start streaming",
                         onStateName: "Stop streaming",
                         offStateImage: "STREAM_StartStreamingGreen.png",
                         onStateImage:  "STREAM_StartStreamingRed.png")
        {
        }

        protected override void ConnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            StreamlabsPlugin.Proxy.AppEvtStreamingOff += eventSwitchedOn;
            StreamlabsPlugin.Proxy.AppEvtStreamingOn += eventSwitchedOff;
        }

        protected override void DisconnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            StreamlabsPlugin.Proxy.AppEvtStreamingOff -= eventSwitchedOn;
            StreamlabsPlugin.Proxy.AppEvtStreamingOn -= eventSwitchedOff;
        }

        protected override void RunCommand(TwoStateCommand command)
        {
            switch (command)
            {
                case TwoStateCommand.TurnOff:
                    StreamlabsPlugin.Proxy.AppStopStreaming();
                    break;

                case TwoStateCommand.TurnOn:
                    StreamlabsPlugin.Proxy.AppStartStreaming();
                    break;

                case TwoStateCommand.Toggle:
                    StreamlabsPlugin.Proxy.AppToggleStreaming();
                    break;
            }
        }
    }
}
