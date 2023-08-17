namespace Loupedeck.StreamlabsPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;

    using SLOBSharp.Client.Requests;
    using SLOBSharp.Client.Responses;
    using SLOBSharp.Domain.Mapping;
    using SLOBSharp.Domain.Services;

    /// <summary>
    /// </summary>
    /// 

    public static class Constants
    {
        public const String img_name_stream_starting = "Streaming/STREAM_StreamStartingYellow.png";
        public const String img_name_stream_started = "Streaming/STREAM_StopStreamingRed.png";
        public const String img_name_stream_stopping = "Streaming/STREAM_StreamStartingYellow.png";
        public const String img_name_stream_stopped = "Streaming/STREAM_StartStreamingGreen.png";

        public const String img_name_rec_starting = "Streaming/STREAM_Starting-Stopping.png";
        public const String img_name_rec_started = "Streaming/STREAM_ToggleRecord1.png";
        public const String img_name_rec_stopping = "Streaming/STREAM_Starting-Stopping.png";
        public const String img_name_rec_stopped = "Streaming/STREAM_ToggleRecord2.png";

        public const String img_name_active_scene = "Streaming/SceneOn.png";
        public const String img_name_inactive_scene = "Streaming/SceneOff.png";

        public const String img_name_active_source = "Streaming/SourceOn.png";
        public const String img_name_inactive_source = "Streaming/SourceOff.png";

        public const String img_name_studio_mode_enable = "Streaming/STREAM_EnableStudioMode.png";
        public const String img_name_studio_mode_disable = "Streaming/STREAM_DisableStudioMode2.png";

        public const String img_name_start_replay_buffer = "Streaming/STREAM_StartReplayBuffer.png";
        public const String img_name_stop_replay_buffer = "Streaming/STREAM_StopReplayBuffer.png";

        public const String img_name_software_not_found = "Streaming/SoftwareNotFound.png";

        public const String img_name_streaming_audio_on = "Streaming/AudioOn.png";
        public const String img_name_streaming_audio_off = "Streaming/AudioOff.png";

        public const String streamlabs_process_name = "Streamlabs OBS";
        public const String streamlabs_is_not_running = "Streamlabs is not running";

        public const String dynamic_scenes = "DynamicScenes";
        public const String dynamic_sources = "DynamicSources";

        public const String dynamic_mixers = "DynamicMixers";
        public const String reset_dynamic_mixers = "ResetDynamicMixers";

        public const String toggle_streaming = "ToggleStreaming";
        public const String toggle_recording = "ToggleRecording";

        public const String save_replay = "SaveReplay";
        public const String toggle_replay_buffer = "ToggleReplayBuffer";

        public const String studio_mode = "StudioMode";

        // ------- subscription services ------------------------------------------

        public const String SceneSwitched = "ScenesService.sceneSwitched";
        public const String SceneAdded = "ScenesService.sceneAdded";
        public const String SceneRemoved = "ScenesService.sceneRemoved";

        public const String SourceAdded = "SourcesService.sourceAdded";
        public const String SourceRemoved = "SourcesService.sourceRemoved";
        public const String SourceUpdated = "SourcesService.sourceUpdated";

        public const String ItemAdded = "ScenesService.itemAdded";
        public const String ItemRemoved = "ScenesService.itemRemoved";
        public const String ItemUpdated = "ScenesService.itemUpdated";

        public const String StreamingStatusChanged = "StreamingService.streamingStatusChange";
        public const String RecordingStatusChanged = "StreamingService.recordingStatusChange";
        public const String ReplayBufferStatusChanged = "StreamingService.replayBufferStatusChange";

        public const String СollectionSwitched = "SceneCollectionsService.collectionSwitched";

        public const String StudioModeChanged = "TransitionsService.studioModeChanged";

        // ------------------------------------------------------------------------------------------

        public const String ScenesService = "ScenesService";
        public const String SourcesService = "SourcesService";
        public const String AudioService = "AudioService";
        public const String StreamingService = "StreamingService";
        public const String TransitionsService = "TransitionsService";
        public const String SceneCollectionsService = "SceneCollectionsService";
        public const String RecentEventsService = "RecentEventsService";
        public const String GameOverlayService = "GameOverlayService";

        public const String get_model = "getModel";
        public const String active_scene = "activeScene";
        public const String get_scenes = "getScenes";

        public const String MakeTransition = "MakeTransition";
        public const String ToggleAlertQueue = "ToggleAlertQueue";
        public const String SkipAlert = "SkipAlert";
        public const String ToggleMuteEventSounds = "ToggleMuteEventSounds";

        public const String Unsubscribe = "unsubscribe";

        public const String NotAvailable = "not available";

        public const String EVENT = "EVENT";
        public const String STREAM = "STREAM";
        public const String PROMISE = "PROMISE";
        public const String SUBSCRIPTION = "SUBSCRIPTION";
    }

    internal partial class AppProxy
    {
        private BackgroundTask responseListener;
        public void InitSubscriptionOnEvents()
        {
            this.InitSubscriptionPipe();

            if (this.responseListener == null)
            {
                this.SubscribeToKnownEvents();

                this.responseListener = new BackgroundTask(this.StartListeningSubscriptionResponse, "SLOBSlistener");

                this.responseListener.Start();
                this.responseListener.Signal();

                this.Plugin.Log.Info($"SL: InitSubscriptionOnEvents. after init: responseListener.IsRunning: {this.responseListener.IsRunning}");
            }
        }

        private void StopEventsListener()
        {
            var isrunning = this.responseListener == null ? false : this.responseListener.IsRunning;

            this.Plugin.Log.Info($"SL:  Streamlabs: responseListener == null: {this.responseListener == null}, responseListener.IsRunning: {isrunning}");

            if (this.responseListener != null)
            {
                this.responseListener.Dispose();
                this.responseListener = null;
            }
        }

        private void DisposeSubscriptionOnEvents()
        {
            this.StopEventsListener();

            this.CloseSubscriptionPipe();      // null can happen when app is closed while plugin was locked and then touching 'unlock'
            this.DisposeSubscriptionPipe();
        }

        private void SubscribeToKnownEvents()
        {
            var sceneSwitched = GetJsonRequest("sceneSwitched", Constants.ScenesService);
            var sceneAdded = GetJsonRequest("sceneAdded", Constants.ScenesService);
            var sceneRemoved = GetJsonRequest("sceneRemoved", Constants.ScenesService);

            var itemAdded = GetJsonRequest("itemAdded", Constants.ScenesService);
            var itemRemoved = GetJsonRequest("itemRemoved", Constants.ScenesService);
            var itemUpdated = GetJsonRequest("itemUpdated", Constants.ScenesService);

            var sourceAdded = GetJsonRequest("sourceAdded", Constants.SourcesService);
            var sourceRemoved = GetJsonRequest("sourceRemoved", Constants.SourcesService);
            var sourceUpdated = GetJsonRequest("sourceUpdated", Constants.SourcesService);

            var streamingStatusChange = GetJsonRequest("streamingStatusChange", Constants.StreamingService);
            var recordingStatusChange = GetJsonRequest("recordingStatusChange", Constants.StreamingService);
            var replayBufferStatusChange = GetJsonRequest("replayBufferStatusChange", Constants.StreamingService);

            var collectionSwitched = GetJsonRequest("collectionSwitched", Constants.SceneCollectionsService);

            var studioModeChange = GetJsonRequest("studioModeChanged", Constants.TransitionsService);

            var requests = new List<String>
            {
                sceneSwitched,
                sceneAdded,
                sceneRemoved,

                itemAdded,
                itemRemoved,
                itemUpdated,

                sourceAdded,
                sourceRemoved,
                sourceUpdated,

                streamingStatusChange,
                recordingStatusChange,
                replayBufferStatusChange,

                collectionSwitched,

                studioModeChange,
            };

            var request = String.Join("\n", requests);

            this.ExecuteSubscription(request);
        }

        internal void UnSubscribeFromKnownEvents()
        {
            var sceneSwitched = GetJsonRequest(Constants.Unsubscribe, $"{Constants.ScenesService}.sceneSwitched");
            var sceneAdded = GetJsonRequest(Constants.Unsubscribe, $"{Constants.ScenesService}.sceneAdded");
            var sceneRemoved = GetJsonRequest(Constants.Unsubscribe, $"{Constants.ScenesService}.sceneRemoved");

            var itemAdded = GetJsonRequest(Constants.Unsubscribe, $"{Constants.ScenesService}.itemAdded");
            var itemRemoved = GetJsonRequest(Constants.Unsubscribe, $"{Constants.ScenesService}.itemRemoved");
            var itemUpdated = GetJsonRequest(Constants.Unsubscribe, $"{Constants.ScenesService}.itemUpdated");

            var sourceAdded = GetJsonRequest(Constants.Unsubscribe, $"{Constants.SourcesService}.sourceAdded");
            var sourceRemoved = GetJsonRequest(Constants.Unsubscribe, $"{Constants.SourcesService}.sourceRemoved");
            var sourceUpdated = GetJsonRequest(Constants.Unsubscribe, $"{Constants.SourcesService}.sourceUpdated");

            var streamingStatusChange = GetJsonRequest(Constants.Unsubscribe, $"{Constants.StreamingService}.streamingStatusChange");
            var recordingStatusChange = GetJsonRequest(Constants.Unsubscribe, $"{Constants.StreamingService}.recordingStatusChange");
            var replayBufferStatusChange = GetJsonRequest(Constants.Unsubscribe, $"{Constants.StreamingService}.replayBufferStatusChange");

            var collectionSwitched = GetJsonRequest(Constants.Unsubscribe, $"{Constants.SceneCollectionsService}.collectionSwitched");

            var studioModeChange = GetJsonRequest(Constants.Unsubscribe, $"{Constants.TransitionsService}.studioModeChanged");

            var requests = new List<String>
            {
                sceneSwitched,
                sceneAdded,
                sceneRemoved,

                itemAdded,
                itemRemoved,
                itemUpdated,

                sourceAdded,
                sourceRemoved,
                sourceUpdated,

                streamingStatusChange,
                recordingStatusChange,
                replayBufferStatusChange,

                collectionSwitched,

                studioModeChange,
            };

            var request = String.Join("\n", requests);

            this.ExecuteSubscription(request);
        }

        private static String GetJsonRequest(String method, String service)
        {
            return SlobsRequestBuilder.NewRequest()
                .SetMethod(method)
                .SetResource(service)
                .BuildRequest()
                .ToJson();
        }

        public event EventHandler<RecordingStateArgs> RecordingStateChanged; 
        public event EventHandler<StreamingStateArgs> StreamingStateChanged;
        public event EventHandler<ReplayBufferEventArgs> ReplayBufferStateChanged;
        public event EventHandler<EventArgs> RecordingPaused;
        public event EventHandler<EventArgs> RecordingResumed;
        public event EventHandler<BoolParamArgs> StudioModeSwitched;
        
        public class SlobEventOuterJson
        {
            [JsonProperty("id")]
            public String Id { get; set; }

            [JsonProperty("result")]
            [JsonConverter(typeof(SingleOrArrayConverter<SlobEventInnerJson>))]
            public IEnumerable<SlobEventInnerJson> Result { get; set; }
        }

        internal class SlobEventInnerJson
        {
            [JsonProperty("id")]
            public String Id { get; set; }

            [JsonProperty("_type")]
            public String Type { get; set; }

            [JsonProperty("resourceId")]
            public String ResourceId { get; set; }

            [JsonProperty("data")]
            public String Data{ get; set; }

        }

        private void OnStreamlabsEvent(Object sender, OneStringEventArgs arg)
        {
            try
            {
                var x = JsonConvert.DeserializeObject<SlobEventOuterJson>(arg.s);
                SlobEventInnerJson result = x.Result.FirstOrDefault();

                this.Plugin.Log.Info($"SL: OnStreamlabsEvent: {arg.s}");

                if (result.Type == "EVENT")
                {
                    switch (result.ResourceId)
                    {
                        case Constants.StreamingStatusChanged:
                            if (AppProxy._streamingStatusDictionary.ContainsKey(result.Data))
                            {
                                this.StreamingStateChanged?.Invoke(this, new StreamingStateArgs(AppProxy._streamingStatusDictionary[result.Data]));
                            }
                            break;
                        case Constants.RecordingStatusChanged:
                            if (AppProxy._recordingStatusDictionary.ContainsKey(result.Data))
                            {
                                this.RecordingStateChanged?.Invoke(this, new RecordingStateArgs(AppProxy._recordingStatusDictionary[result.Data]));
                            }
                            break;
                        case Constants.StudioModeChanged:
                            //The .Data  string contains boolean we need to read it 
                            if ( Boolean.TryParse(result.Data, out var studioModeState) )
                            {
                                this.StudioModeSwitched?.Invoke(this, new BoolParamArgs(studioModeState));    
                            }
                            break;

                        case Constants.ReplayBufferStatusChanged:
                            if (AppProxy._replayBufferStatusDictionary.ContainsKey(result.Data))
                            {
                                this.ReplayBufferStateChanged?.Invoke(this, new ReplayBufferEventArgs(AppProxy._replayBufferStatusDictionary[result.Data]));
                            }
                            break;

                        case Constants.SceneSwitched: break;
                        case Constants.SceneAdded: break;
                        case Constants.SceneRemoved: break;

                        case Constants.SourceAdded:
                            break;
                        case Constants.SourceRemoved:
                            break;
                        case Constants.SourceUpdated:
                            break;

                        case Constants.ItemAdded:
                            break;
                        case Constants.ItemRemoved:
                            break;
                        case Constants.ItemUpdated:
                            break;

                        case Constants.СollectionSwitched:
                            break;
                    }
                }
            } 
            catch(Exception e)
            {
                this.Plugin.Log.Error($"SL: OnStreamlabsEvent: {e.Message}");
            }
            
        }
    }
}
