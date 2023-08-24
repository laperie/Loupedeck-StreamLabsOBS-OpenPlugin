namespace Loupedeck.StreamlabsPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Web.UI.WebControls;
    using System.Xml.Linq;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using SLOBSharp.Client.Requests;
    using SLOBSharp.Client.Responses;
    using SLOBSharp.Domain.Mapping;
    using SLOBSharp.Domain.Services;

    /// <summary>
    /// </summary>
    /// 

    public static class Constants
    {

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
        public const String СollectionAdded = "SceneCollectionsService.collectionAdded";
        public const String СollectionRemoved = "SceneCollectionsService.collectionRemoved";
        public const String СollectionUpdated = "SceneCollectionsService.collectionUpdated";

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
        public const String VirtualWebcamService = "VirtualWebcamService";

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

            this.UnSubscribeFromKnownEvents();  

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

        private static List<String> SubscribedEvents = new List<String>()
        {
            Constants.SceneSwitched,
            Constants.SceneAdded,
            Constants.SceneRemoved,
            Constants.ItemAdded,
            Constants.ItemRemoved,
            Constants.ItemUpdated,
            Constants.SourceAdded,
            Constants.SourceRemoved,
            Constants.SourceUpdated,
            Constants.StreamingStatusChanged,
            Constants.RecordingStatusChanged,
            Constants.ReplayBufferStatusChanged,
            Constants.СollectionSwitched,
            Constants.СollectionAdded,
            Constants.СollectionRemoved,
            Constants.СollectionUpdated,

            Constants.StudioModeChanged,
            /*Virtual cam event to be added */
            
        };

        private void HandleEventsSubscription(Boolean Subscribe)
        {
            var request = "";
            foreach (var subscriber in SubscribedEvents)
            {

                //Get Service and Event name from the subscribed event
                request += (request.Length > 0 ? "\n":"")
                        + (Subscribe
                            ? MakeRequest(subscriber.Split('.')[1], subscriber.Split('.')[0])
                            : MakeRequest(Constants.Unsubscribe, subscriber));
            }

            this.ExecuteSubscription(request);
        }

        private void SubscribeToKnownEvents() => this.HandleEventsSubscription(true);

        private void UnSubscribeFromKnownEvents() => this.HandleEventsSubscription(false);

        private static String MakeRequest(String method, String service)
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
        /*Note using OldNewStringChange args here for compatibility*/
        public event EventHandler<OldNewStringChangeEventArgs> SceneCollectionChanged;
        /*For all additions, removals etc*/
        public event EventHandler<EventArgs> SceneCollectionListChanged;
        public event EventHandler<EventArgs> SceneListChanged;
        public event EventHandler<OneStringEventArgs> CurrentSceneChanged;
        private void OnStreamlabsEvent(Object sender, OneStringEventArgs arg)
        {
            try
            {
                var response = JObject.Parse(arg.Value);
                JToken result = response["result"];
                if ( response == null || result == null || result["_type"] == null || result["_type"].ToString() != "EVENT")
                {
                    this.Plugin.Log.Warning($"SL: No event data in OnStreamlabsEvent: {arg.Value}");
                    return;
                }

                var resourceId = result["resourceId"].ToString();

                this.Plugin.Log.Info($"SL: OnStreamlabsEvent from: {resourceId}");

                switch (resourceId)
                {
                        
                    case Constants.StreamingStatusChanged:
                        //We don't really have to check if the key is in dictionary -- worst case it'll throw an exception and we catch it
                        this.StreamingStateChanged?.Invoke(this, new StreamingStateArgs(AppProxy._streamingStatusDictionary[result["data"].ToString()]));
                        
                        break;
                    case Constants.RecordingStatusChanged:
                        this.RecordingStateChanged?.Invoke(this, new RecordingStateArgs(AppProxy._recordingStatusDictionary[result["data"].ToString()]));
                        break;
                    case Constants.ReplayBufferStatusChanged:
                        this.ReplayBufferStateChanged?.Invoke(this, new ReplayBufferEventArgs(AppProxy._replayBufferStatusDictionary[result["data"].ToString()]));
                        break;

                    case Constants.StudioModeChanged:
                        this.StudioModeSwitched?.Invoke(this, new BoolParamArgs(Boolean.Parse(result["data"].ToString())));
                        break;

                    case Constants.СollectionSwitched:
                        this.SceneCollectionChanged?.Invoke(this, new OldNewStringChangeEventArgs(this.CurrentSceneCollection, result["data"]["name"].ToString()));
                        break;
                    case Constants.СollectionAdded:
                    case Constants.СollectionRemoved:
                    case Constants.СollectionUpdated:
                        this?.SceneCollectionListChanged?.Invoke(this, null);
                        break;


                    case Constants.SceneSwitched:
                        this.CurrentSceneChanged?.Invoke(this, new OneStringEventArgs(result["data"]["name"].ToString()));
                        break;
                    case Constants.SceneAdded:
                    case Constants.SceneRemoved:

                    case Constants.ItemAdded:
                    case Constants.ItemRemoved:
                    case Constants.ItemUpdated:

                    case Constants.SourceAdded:
                    case Constants.SourceRemoved:
                    case Constants.SourceUpdated:

                    

                    default:
                        this.Plugin.Log.Info($"SL: OnStreamlabsEvent: No handler for {resourceId}. Result[data]={result["data"]}");
                        break;
            
                }
            } 
            catch(Exception e)
            {
                this.Plugin.Log.Error($"SL:OnStreamlabsEvent exception: {e.Message}\n\tInput: {arg.Value}, ");
            }
            
        }
    }
}
