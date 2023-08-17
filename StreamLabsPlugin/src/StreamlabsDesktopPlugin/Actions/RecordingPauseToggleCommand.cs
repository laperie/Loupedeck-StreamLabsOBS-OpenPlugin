namespace Loupedeck.StreamlabsPlugin.Actions
{
    using System;

    internal class RecordingPauseToggleCommand : GenericOnOffSwitch
    {
        
        public RecordingPauseToggleCommand()
                : base(
                    name: "RecordingPause",
                    displayName: "Recording Pause",
                    description: "Pauses/resumes recording",
                    groupName: "",
                    offStateName: "Pause recording",
                    onStateName: "Resume recording",
                    offStateImage: "STREAM_RecordPause.png",
                    onStateImage: "STREAM_RecordResume.png")
        {
        }

        private void OnAppRecordingStarted(Object sender, EventArgs e)
        {
            // Note the command is only enabled if there is recording!
            this.TurnOff();
            this.IsEnabled = true;
        }

        private void OnAppRecordingStopped(Object sender, EventArgs e)
        {
            this.TurnOff();
            this.IsEnabled = false;
        }

        private void OnAppRecordingResumed(Object sender, EventArgs e)
        {
            // Note this can be called from OnLoad as well (eventSwitchedOn) in which case we check if we are InRecording
            this.IsEnabled = StreamlabsPlugin.Proxy.InRecording;
            this.TurnOff();
        }

        private void OnAppRecordingPaused(Object sender, EventArgs e) => this.TurnOn();

        protected override void ConnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            StreamlabsPlugin.Proxy.AppEvtRecordingResumed += eventSwitchedOff;
            StreamlabsPlugin.Proxy.AppEvtRecordingPaused += eventSwitchedOn;

            StreamlabsPlugin.Proxy.AppEvtRecordingOff += this.OnAppRecordingStopped;
            StreamlabsPlugin.Proxy.AppEvtRecordingOn += this.OnAppRecordingStarted;
            StreamlabsPlugin.Proxy.AppEvtRecordingPaused += this.OnAppRecordingPaused;
            StreamlabsPlugin.Proxy.AppEvtRecordingResumed += this.OnAppRecordingResumed;
        }

        protected override void DisconnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn)
        {
            StreamlabsPlugin.Proxy.AppEvtRecordingResumed -= eventSwitchedOff;
            StreamlabsPlugin.Proxy.AppEvtRecordingPaused -= eventSwitchedOn;
            StreamlabsPlugin.Proxy.AppEvtRecordingOff -= this.OnAppRecordingStopped;
            StreamlabsPlugin.Proxy.AppEvtRecordingOn -= this.OnAppRecordingStarted;
            StreamlabsPlugin.Proxy.AppEvtRecordingPaused -= this.OnAppRecordingPaused;
            StreamlabsPlugin.Proxy.AppEvtRecordingResumed -= this.OnAppRecordingResumed;
        }
        protected override void RunCommand(TwoStateCommand command)
        {
            switch (command)
            {
                case TwoStateCommand.TurnOff:
                    StreamlabsPlugin.Proxy.AppResumeRecording();
                    break;

                case TwoStateCommand.TurnOn:
                    StreamlabsPlugin.Proxy.AppPauseRecording();
                    break;

                case TwoStateCommand.Toggle:
                    StreamlabsPlugin.Proxy.AppToggleRecordingPause();
                    break;
            }
        }
    }
}
