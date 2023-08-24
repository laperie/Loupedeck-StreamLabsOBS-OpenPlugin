namespace Loupedeck.StreamlabsPlugin
{

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    using SLOBSharp.Client;
    using SLOBSharp.Client.Requests;
    using SLOBSharp.Client.Responses;
    using SLOBSharp.Domain.Services;
    using SLOBSharp.Client.Responses;
    using Newtonsoft.Json.Linq;
    using SLOBSharp.Client.Requests.Scenes;


    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    internal partial class AppProxy : SlobsPipeClient
    {
        // Our 'own' events
        public event EventHandler<EventArgs> AppConnected;
        public event EventHandler<EventArgs> AppDisconnected;

        //Events coming from SLOBS
        public event EventHandler<EventArgs> Connected;
        public event EventHandler<EventArgs> Disconnected;


        public Boolean IsConnected;

        public Plugin Plugin { get; private set; }

        // Properties
        public Boolean IsAppConnected => this.IsConnected;

        // Folders to select from when we try saving screenshots
        public static readonly Environment.SpecialFolder[] ScreenshotFolders =
            {
                Environment.SpecialFolder.MyPictures,
                Environment.SpecialFolder.MyDocuments,
                Environment.SpecialFolder.Personal,
                Environment.SpecialFolder.CommonPictures
            };

        public AppProxy(Plugin _plugin) : base("slobs")
        {
            this.Plugin = _plugin;

            // Trying to set screenshot save-to path
            for (var i = 0; (i < ScreenshotFolders.Length) && String.IsNullOrEmpty(AppProxy.ScreenshotsSavingPath); i++)
            {
                var folder = Environment.GetFolderPath(ScreenshotFolders[i]);
                if (Directory.Exists(folder))
                {
                    AppProxy.ScreenshotsSavingPath = folder;
                }
            }


        }
        public void RegisterAppEvents()
        {
            this.subscriptionEvt += this.OnStreamlabsEvent;
            this.InitSubscriptionOnEvents();
        }

        public void UnregisterAppEvents()
        {
            this.subscriptionEvt -= this.OnStreamlabsEvent;
            this.DisposeSubscriptionOnEvents();
        }


        // Note, now we just try connecting -- later on it should be some simple status transaction to see wheher app is up and running
        public void Connect()
        {
            var response = this.TryConnecting();
            if (response)
            {
                this.Plugin.Log.Info("Invoke Appconnected");
                //Just calling Connected event handler directly 
                this.OnAppConnected(this, EventArgs.Empty);
            }
            else
            {
                this.Plugin.Log.Info("Not Connected");
            }
        }

        private Boolean _scene_collection_events_subscribed = false;

        private void UnsubscribeFromSceneCollectionEvents()
        {

            if (!this._scene_collection_events_subscribed)
            {
                this.SceneListChanged -= this.OnObsSceneListChanged;
                this.CurrentSceneChanged -= this.OnObsSceneChanged;
#if false

                this.PreviewSceneChanged -= this.OnObsPreviewSceneChanged;
        
                this.SceneItemVisibilityChanged -= this.OnObsSceneItemVisibilityChanged;
                this.SceneItemAdded -= this.OnObsSceneItemAdded;
                this.SceneItemRemoved -= this.OnObsSceneItemRemoved;

                this.SourceMuteStateChanged -= this.OnObsSourceMuteStateChanged;
                this.SourceVolumeChanged -= this.OnObsSourceVolumeChanged;

                this.SourceCreated -= this.OnObsSourceCreated;
                this.SourceDestroyed -= this.OnObsSourceDestroyed;

                this.SourceAudioActivated -= this.OnObsSourceAudioActivated;
                this.SourceAudioDeactivated -= this.OnObsSourceAudioDeactivated;
#endif
                this._scene_collection_events_subscribed = true;
            }
        }

        private void SubscribeToSceneCollectionEvents()
        {
            if (this._scene_collection_events_subscribed)
            {
                this.SceneListChanged += this.OnObsSceneListChanged;
                this.CurrentSceneChanged += this.OnObsSceneChanged;
#if false
                
                this.PreviewSceneChanged += this.OnObsPreviewSceneChanged;

                this.SceneItemVisibilityChanged += this.OnObsSceneItemVisibilityChanged;
                this.SceneItemAdded += this.OnObsSceneItemAdded;
                this.SceneItemRemoved += this.OnObsSceneItemRemoved;

                this.SourceMuteStateChanged += this.OnObsSourceMuteStateChanged;
                this.SourceVolumeChanged += this.OnObsSourceVolumeChanged;

                this.SourceCreated += this.OnObsSourceCreated;
                this.SourceDestroyed += this.OnObsSourceDestroyed;

                this.SourceAudioActivated += this.OnObsSourceAudioActivated;
                this.SourceAudioDeactivated += this.OnObsSourceAudioDeactivated;
#endif
                this._scene_collection_events_subscribed = false;
            }
        }

        internal void InitializeObsData(Object sender, EventArgs e)
        {

            // Retreiving current streaming and recording statuses
            {
                var result = this.ExecuteSlobsMethodSync("getModel", "StreamingService");
                this._currentStreamingState = result.Result.FirstOrDefault();

            }
            if (this._currentStreamingState == null)
            {
                this.Plugin.Log.Info("Cannot retreive streaming status!");
            }

            this.Plugin.Log.Info($"Current streaming status: {this._currentStreamingState}");

            // Retreiving Audio types.
#if false
        this.OnAppConnected_RetreiveSourceTypes();
#endif

            if (this.GetCurrentStreamingStatus() != StreamlabsStreamingStatus.NONE)
            {
                this.OnObsStreamingStateChange(this, new StreamingStateArgs(this.GetCurrentStreamingStatus()));
            }

            if (this.GetCurrentRecordingStatus() != StreamlabsRecordingStatus.NONE)
            {
                this.OnObsRecordingStateChange(this, new RecordingStateArgs(this.GetCurrentRecordingStatus()));
            }

            if (this.GetCurrentReplayBufferStatus() != StreamlabsReplayBufferStatus.NONE)
            {
                this.OnObsReplayBufferStateChange(this, new ReplayBufferEventArgs(this.GetCurrentReplayBufferStatus()));
            }

            //Retreiving current studio mode status
            try
            {
                var result = this.ExecuteSlobsMethodSync("getModel", "TransitionsService");
                //FIXME, this looks suspicious [although it works]
                dynamic jobject = JObject.Parse(result.JsonResponse);
                this.Plugin.Log.Info($"Retreived studio mode: {(Boolean)jobject.result.studioMode}");
                this.OnObsStudioModeStateChange(sender, new BoolParamArgs((Boolean)jobject.result.studioMode));
            }
            catch (Exception ex)
            {
                this.Plugin.Log.Error(ex, "Cannot retreive studio mode status!");
            }

#if false
        if (vcamstatus != null && vcamstatus.IsActive)
        {
            this.OnObsVirtualCameraStarted(sender, e);
        }
        else
        {
            this.OnObsVirtualCameraStopped(sender, e);
        }
#endif
            this.Plugin.Log.Info("Init: OnObsSceneCollectionListChanged");


            this.OnObsSceneCollectionListChanged(sender, new OldNewStringChangeEventArgs("", ""));

            this.Plugin.Log.Info("Init: OnObsSceneCollectionChanged");
            // This should initiate retreiving of all data
            // to indicate that we need to force rescan of all scenes and all first parameter is null 
            this.OnObsSceneCollectionChanged(null, e);

        }

        private void OnAppConnected(Object sender, EventArgs e)
        {
            this.Plugin.Log.Info("Entering AppConnected");

            this.IsConnected = true;

            // Subscribing to App events
            // In obs we called that in OnLoad
            this.RegisterAppEvents();
            // Notifying all subscribers on App Connected
            // Fetching initial states for controls

            this.RecordingStateChanged += this.OnObsRecordingStateChange;
            this.RecordingPaused += this.OnObsRecordPaused;
            this.RecordingResumed += this.OnObsRecordResumed;
            this.StreamingStateChanged += this.OnObsStreamingStateChange;
            this.StudioModeSwitched += this.OnObsStudioModeStateChange;
            this.ReplayBufferStateChanged += this.OnObsReplayBufferStateChange;
            this.SceneCollectionChanged += this.OnObsSceneCollectionChanged;
            this.SceneCollectionListChanged += this.OnObsSceneCollectionListChanged;
#if false
            this.TransitionEnd += this.OnObsTransitionEnd;
            
            
            this.VirtualCameraStarted += this.OnObsVirtualCameraStarted;
            this.VirtualCameraStopped += this.OnObsVirtualCameraStopped;
            
#endif
            this.AppConnected?.Invoke(sender, e);

            this.Plugin.Log.Info("AppConnected: Initializing data");
            _ = Helpers.TryExecuteSafe(() =>
            {
                this.InitializeObsData(sender, e);
            });

            // Subscribing to all the events that are depenendent on Scene Collection change
            this._scene_collection_events_subscribed = true;
            this.SubscribeToSceneCollectionEvents();

        }

        private void OnAppDisconnected(Object sender, EventArgs e)
        {
            this.Plugin.Log.Info("Entering AppDisconnected");


            // Unsubscribing from App events here
            this.RecordingStateChanged -= this.OnObsRecordingStateChange;
            this.RecordingPaused -= this.OnObsRecordPaused;
            this.RecordingResumed -= this.OnObsRecordResumed;

            this.StreamingStateChanged -= this.OnObsStreamingStateChange;
            this.StudioModeSwitched -= this.OnObsStudioModeStateChange;
            this.ReplayBufferStateChanged -= this.OnObsReplayBufferStateChange;

            this.SceneCollectionChanged -= this.OnObsSceneCollectionChanged;
            this.SceneCollectionListChanged -= this.OnObsSceneCollectionListChanged;

#if false
            
            this.TransitionEnd -= this.OnObsTransitionEnd;
            this.VirtualCameraStarted -= this.OnObsVirtualCameraStarted;
            this.VirtualCameraStopped -= this.OnObsVirtualCameraStopped;
#endif

            this._scene_collection_events_subscribed = false;
            this.UnsubscribeFromSceneCollectionEvents();


            // Unsubscribing from all the events that are depenendent on Scene Collection change
            this.UnregisterAppEvents();
            this.AppDisconnected?.Invoke(sender, e);
        }

        private void SafeRunConnected(Action action, String warning)
        {
            if (this.IsAppConnected)
            {
                if (!Helpers.TryExecuteSafe(action))
                {
                    this.Plugin.Log.Warning(warning);
                }
            }
        }
        /*****************************/
        //SOME MASSAGING OF SLOBS DATA. NEEDS TO BE MOVED TO THE RIGHT PLACE 
        private static Int32 _rpc_messageId = 0;
        private static ISlobsRequest PrepareRequest(String methodName, String serviceName, Boolean compactMode, Object[] parameters)
        {
            var slobsRequest = SlobsRequestBuilder.NewRequest()
                .SetRequestId(AppProxy._rpc_messageId++.ToString())
                .SetMethod(methodName)
                .SetResource(serviceName)
                .SetCompactMode(compactMode);

            if (parameters != null && parameters.Any())
            {
                slobsRequest.AddArgs(parameters);
            }

            return slobsRequest.BuildRequest();
        }

        public SlobsRpcResponse ExecuteSlobsMethodSync(String methodName, String serviceName, Boolean compactMode = false, params Object[] parameters) => this.ExecuteRequest(PrepareRequest(methodName, serviceName, compactMode, parameters));

        public async Task<SlobsRpcResponse> ExecuteSlobsMethod(String methodName, String serviceName, Boolean compactMode = false, params Object[] parameters) => await this.ExecuteRequestAsync(PrepareRequest(methodName, serviceName, compactMode, parameters)).ConfigureAwait(false);

        //Combined status with Recording, Streaming and Replay Buffer
        private SlobsResult _currentStreamingState = null;

        private void SetCurrentScene(String newScene) 
            => Helpers.TryExecuteSafe(() => this.ExecuteSlobsMethodSync("makeSceneActive", Constants.SceneCollectionsService, false, new Object[] { newScene }));

        private void SetPreviewScene(String newScene) =>
                //FIXME: Double check with SLOBS folks that this actually works like that
                this.SetCurrentScene(newScene);

        private Scene GetCurrentScene() 
            => Helpers.TryExecuteFunc(() => this.ExecuteSlobsMethodSync("activeScene", Constants.ScenesService).ToString(), out var sceneName) ? new Scene(sceneName) : null;
        private List<Scene> GetSceneList() 
            => Helpers.TryExecuteFunc(() => this.ExecuteSlobsMethodSync("getScenes", Constants.ScenesService).Result.Select(s => new Scene(s.Name)).ToList(), out var list) ? list : new List<Scene>();

        private List<LDSceneItem> GetSceneItemList(String scene) 
            => /*Helpers.TryExecuteFunc(() =>
            { var res = this.ExecuteSlobsMethodSync("getScene", Constants.ScenesService, false, new Object[] { scene });
              return res.Result.Select(s => new LDSceneItem(s.Name)).ToList(); 
            }
            , out var list) ? list : */new List<LDSceneItem>();

        private List<String> ListSceneCollections()
        {
            return Helpers.TryExecuteFunc(() => this.ExecuteSlobsMethodSync("collections", Constants.SceneCollectionsService).Result.Select(s => s.Name).ToList(), out var sceneCollectionList)
            ? sceneCollectionList
            : new List<String>();
        }


        private String GetCurrentSceneCollection() 
            => Helpers.TryExecuteFunc(() => this.ExecuteSlobsMethodSync("activeCollection", Constants.SceneCollectionsService).Result.FirstOrDefault().Name, out var name) ? name : "";

        private void SetCurrentSceneCollection(String newCollection)
            => Helpers.TryExecuteSafe(() => this.ExecuteSlobsMethodSync("load", Constants.SceneCollectionsService, false, new Object[] { newCollection }));

    }
}


