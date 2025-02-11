﻿namespace Loupedeck.StreamlabsPlugin
{
    using System;
    using static Loupedeck.StreamlabsPlugin.AppProxy;

    internal class IntParamArgs : EventArgs
    {
        public IntParamArgs(Int32 v) => this.Value = v;

        public Int32 Value { get; set; }
    }

    internal class BoolParamArgs : EventArgs
    {
        public BoolParamArgs(Boolean v) => this.Value = v;

        public Boolean Value { get; set; }
    }

    internal class RecordingStateArgs: EventArgs
    {
        public RecordingStateArgs(StreamlabsRecordingStatus v) => this.Value = v;

        public StreamlabsRecordingStatus Value { get; set; }
    }
    internal class StreamingStateArgs : EventArgs
    {
        public StreamingStateArgs(StreamlabsStreamingStatus v) => this.Value = v;

        public StreamlabsStreamingStatus Value { get; set; }
    }
    internal class ReplayBufferEventArgs : EventArgs
    {
        public ReplayBufferEventArgs(StreamlabsReplayBufferStatus v) => this.Value = v;

        public StreamlabsReplayBufferStatus Value { get; set; }
    }
    
    internal class MuteEventArgs : EventArgs
    {
        public String SourceName;
        public Boolean isMuted;
        public MuteEventArgs(String name, Boolean muted) 
        { 
            this.SourceName = name; 
            this.isMuted = muted; 
        }
    }

    internal class VolumeEventArgs : EventArgs
    {
        public String SourceName;
        public Single Volume;
        public Single VolumeDb;
        public VolumeEventArgs(String name, Single vol, Single vol_db = 0)
        {
            this.SourceName = name;
            this.Volume = vol;
            this.VolumeDb = vol_db;
        }
    }

    //Commonly used old-new arg class for 'onchange' events 
    internal class TwoStringArgs: EventArgs
    {
        private readonly Tuple<String, String> val;
        public String Item1 { get { return this.val.Item1; } }
        public String Item2 { get { return this.val.Item2; } }
        public TwoStringArgs(String item1, String item2) => this.val = Tuple.Create(item1, item2);
    }

    internal class OldNewStringChangeEventArgs: TwoStringArgs
    {
        public String New => this.Item2;
        public String Old => this.Item1;
        public OldNewStringChangeEventArgs(String old, String _new) : base(old, _new)
        {
        }
    }
    internal class SceneItemVisibilityChangedArgs: TwoStringArgs
    {
        public Boolean Visible { get; private set; }
        public String SceneName => this.Item1;
        public String ItemName=> this.Item2;
        public SceneItemVisibilityChangedArgs(String scene, String item, Boolean isVisible) : base(scene, item) => this.Visible = isVisible;
    }

    internal class SourceNameEventArgs: EventArgs
    {
        public String SourceName { get; private set; }
        public SourceNameEventArgs(String name) => this.SourceName = name;

    }
}
