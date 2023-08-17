namespace Loupedeck.StreamlabsPlugin.Actions
{
    using System;

    internal class ReplayBufferToggleCommand : GenericOnOffSwitch
    {
        public ReplayBufferToggleCommand()
            : base(
                name: "ReplayBufferToggle",
                displayName: "Replay Buffer Toggle",
                description: "Starts/Stops the Replay Buffer in OBS Studio",
                groupName: "",
                offStateName: "Start recording into the Replay Buffer",
                onStateName: "Stop recording into the Replay Buffer",
                offStateImage: "STREAM_StartReplayBuffer.png",
                onStateImage: "STREAM_StopReplayBuffer.png")
        {
        }

        protected override void ConnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            StreamlabsPlugin.Proxy.AppEvtReplayBufferOff += eventSwitchedOff;
            StreamlabsPlugin.Proxy.AppEvtReplayBufferOn += eventSwitchedOn;
        }

        protected override void DisconnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            StreamlabsPlugin.Proxy.AppEvtReplayBufferOff -= eventSwitchedOff;
            StreamlabsPlugin.Proxy.AppEvtReplayBufferOn -= eventSwitchedOn;
        }

        protected override void RunCommand(TwoStateCommand command)
        {
            switch (command)
            {
                case TwoStateCommand.TurnOff:
                    StreamlabsPlugin.Proxy.AppStopReplayBuffer();
                    break;

                case TwoStateCommand.TurnOn:
                    StreamlabsPlugin.Proxy.AppStartReplayBuffer();
                    break;

                case TwoStateCommand.Toggle:
                    StreamlabsPlugin.Proxy.AppToggleReplayBuffer();
                    break;
            }
        }

    }
}
