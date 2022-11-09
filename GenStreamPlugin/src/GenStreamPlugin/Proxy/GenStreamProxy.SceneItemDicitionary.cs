﻿namespace Loupedeck.GenStreamPlugin
 {
    using System;
    using System.Collections.Generic;
    using OBSWebsocketDotNet;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    public partial class GenStreamProxy 
    {
        // NOTE: The searching ONLY in the current scene
        private Boolean AddSceneItemToDictionary(String sceneName, String sourceName)
        {
            foreach (var item in this.CurrentScene.Items)
            {
                if (item.SourceName == sourceName)
                {
                    return this.AddSceneItemToDictionary(sceneName, item);
                }
            }

            this.Trace($"Cannot get sceneItem for item {sourceName} of scene {sceneName}");

            return false;
        }

        private Boolean AddSceneItemToDictionary(String sceneName, OBSWebsocketDotNet.Types.SceneItem item)
        {
            var sourceDictItem = SceneItemDescriptor.CreateSourceDictItem(this.CurrentSceneCollection, sceneName, item, this);

            if (sourceDictItem != null)
            {
                this.allSceneItems.Add(SceneItemKey.Encode(this.CurrentSceneCollection, sceneName, item.SourceName),
                                    sourceDictItem);
                return true;
            }
            else
            {
                this.Trace($"Cannot get props for item {item.SourceName} of scene {sceneName}");
                return false;
            }

        }

        //Retreives all scene items for all scenes in current collection
        private void OnObsSceneCollectionChange_FetchSceneItems()
        {
            this.allSceneItems.Clear();

            this.Trace("Adding scene items");

            //sources 
            foreach (var scene in this.Scenes)
            {
                if (!Helpers.TryExecuteFunc(() => { return this.GetSceneItemList(scene.Name); }, out var sceneDetailsList))
                {
                    this.Trace($"Warning: Cannot get SceneList for scene {scene.Name}");
                    continue;
                }

                foreach (var item in sceneDetailsList)
                {
                    var sceneItem = scene.Items.Find(x => x.SourceName == item.SourceName);
                    if (sceneItem != null)
                    {
                        var sourceDictItem = SceneItemDescriptor.CreateSourceDictItem(this.CurrentSceneCollection, scene.Name, sceneItem, this, null, item);
                        if (sourceDictItem != null)
                        {
                            this.allSceneItems.Add(SceneItemKey.Encode(this.CurrentSceneCollection, scene.Name, item.SourceName), sourceDictItem);
                        }
                        else
                        {
                            this.Trace($"Warning: Cannot get CreateSourceDictItem for scene {scene.Name}, item {sceneItem.SourceName}");
                        }
                    }
                    else
                    {
                        this.Trace($"Warning: Cannot get SceneItemList for scene {scene.Name}");
                    }
                }

            }
        }

        /// <summary>
        /// Our own dictionary of scene items of all scenes in current collection, with all properties
        /// Note: Scene item is an instance of the source in particular scene.  Most of the source 
        /// properties are shared among scenes with just a few (like visibility) being scene-specif
        /// 
        /// </summary>
         //'Main' dictionary, with Scene-Item ID being a key
        //Dictionary 
        public Dictionary<String, SceneItemDescriptor> allSceneItems;

        public class SceneItemDescriptor
        {
            public String CollectionName;
            public String SceneName;
            public String SceneNameProp => this.SceneItemProps.ItemName;
            public String SourceName => this.SceneItemDetails.SourceName;

            public Boolean Visible { get { return this.SceneItemProps.Visible; } set { this.SceneItemProps.Visible = value; } }

            /// <summary>
            /// Adjusts VolumeByLD to diff % (diff/100) . Ensures the volume is between 0.0 and 1.0
            /// </summary>
            /// <param name="adjustmentDiff"></param>
            /// <returns></returns>
            
            public Single AdjustVolume(Int32 adjustmentDiff)
            {
                this.VolumeByLD += adjustmentDiff / 100;
                this.VolumeByLD = (Single)(this.VolumeByLD < 0.0 ? 0.0 : this.VolumeByLD);
                this.VolumeByLD = (Single)(this.VolumeByLD > 1.0 ? 1.0 : this.VolumeByLD);

                return this.VolumeByLD;
            }

            //Volume:  Set by Loupedeck and set from OBS. 
            public Single VolumeByLD;
            public Single VolumebyObs
            {
                get => this._originalVolume;
                set
                {
                    this._originalVolume = value;
                    this.VolumeByLD = value;
                }
            }

            public Boolean ObsVolumeUpdatePending => this._originalMuted != this.MutedByLD;

            private Single _originalVolume;

            //Mute:  Set by Loupedeck and set from OBS. 
            public Boolean MutedByLD;

            public Boolean ObsMuteUpdatePending => this._originalMuted != this.MutedByLD;

            //To Disinguish what was set from OBS (MutedByObs) and MutedByLD
            public Boolean MutedByObs
            {
                get => this._originalMuted;
                set
                {
                    this._originalMuted = value;
                    this.MutedByLD = value;
                }
            }

            private Boolean _originalMuted;

            public Boolean Is_volume_controlled
            {
                get
                {
                    //FIXME: In theory, we should harness new GetMediaType request to ensure all new types are there too 
                    // FIXME: Validate if this this.SceneItem.AudioVolume != 1 mantra was actually needed
                    return  (this.SceneItem.InternalType == "browser_source") ||
                            (this.SceneItem.InternalType == "wasapi_input_capture") ||
                            (this.SceneItem.InternalType == "wasapi_output_capture") ||
                            (this.SceneItem.InternalType == "dshow_input") ||
                            (this.SceneItem.InternalType == "vlc_source") ||
                            (this.SceneItem.InternalType == "ffmpeg_source") ||
                            (this.SceneItem.InternalType == "audio_capture") ||
                            (this.SceneItem.InternalType == "coreaudio_input_capture") ||
                            (this.SceneItem.InternalType == "coreaudio_output_capture") ||
                            (this.SceneItem.InternalType == "jack_output_capture");
                }
            }

            //private readonly volumeinfo;
            private readonly OBSWebsocketDotNet.Types.SceneItem SceneItem;
            private readonly OBSWebsocketDotNet.Types.SceneItemDetails SceneItemDetails;
            private readonly OBSWebsocketDotNet.Types.SceneItemProperties SceneItemProps;

            /// <summary>
            /// Creates a single Source Dictionary item, optionally feching SceneItemProperties  and SceneItemDetails
            /// </summary>
            /// <param name="in_collection">Collection</param>
            /// <param name="in_sceneName">SceneName</param>
            /// <param name="in_sceneItem">SceneItem descriptor</param>
            /// <param name="_obs">OBS Websocket</param>
            /// <param name="in_props">properties</param>
            /// <param name="in_details">details</param>
            /// <returns></returns>
            public static SceneItemDescriptor CreateSourceDictItem(String in_collection, String in_sceneName, OBSWebsocketDotNet.Types.SceneItem in_sceneItem, OBSWebsocketDotNet.OBSWebsocket _obs,
                                    OBSWebsocketDotNet.Types.SceneItemProperties in_props = null, OBSWebsocketDotNet.Types.SceneItemDetails in_details = null)
            {
                try
                {
                    var props = in_props ?? _obs.GetSceneItemProperties(in_sceneItem.SourceName, in_sceneName);
                    var details = in_details;

                    if (in_details == null)
                    {
                        var list = _obs.GetSceneItemList(in_sceneName);
                        foreach (var detail in list)
                        {
                            if (detail.SourceName == in_sceneItem.SourceName)
                            {
                                in_details = detail;
                                break;
                            }
                        }
                    }

                    if (in_details == null)
                    {
                        throw new Exception("Cannot find details for source");
                    }

                    var source = new SceneItemDescriptor(in_collection, in_sceneName, in_sceneItem, details, props);

                    return source;
                }
                catch (Exception ex)
                {
                    Tracer.Trace($"Warning: Exception {ex.Message} in creating source item for item {in_sceneItem.SourceName} of scene {in_sceneName}  ");
                }

                return null;
            }

            protected SceneItemDescriptor(String coll, String scene, OBSWebsocketDotNet.Types.SceneItem item, OBSWebsocketDotNet.Types.SceneItemDetails details, OBSWebsocketDotNet.Types.SceneItemProperties props)
            {
                this.CollectionName = coll;
                this.SceneItem = item;
                this.SceneName = scene;
                this.SceneItemDetails = details;
                this.SceneItemProps = props;

                if (scene != this.SceneNameProp)
                {
                    Tracer.Trace($"SourceDictItem ctor: Scene name {scene} Scene Name in details: { this.SceneNameProp}");
                }
            }
        }

    }
}


/// Stripped version of all Scene Item-related classes of OBS
////
#if FALSE
    public class SceneItem
    {
        public string SourceName;
        public string InternalType;
        public float AudioVolume;
        public float XPos;
        public float YPos;
        public int SourceWidth;
        public int SourceHeight;
        public float Width;
        public float Height;
        public bool Locked { set; get; }
        public bool Render { set; get; }
        public int ID { set; get; }
        public string ParentGroupName { set; get; }
        public List<SceneItem> GroupChildren { set; get; }
}

   public class SceneItemProperties
    {
        public SceneItemCropInfo Crop { set; get; }
        public SceneItemBoundsInfo Bounds { set; get; }
        public SceneItemPointInfo Scale { set; get; }
        public SceneItemPositionInfo Position { set; get; }
        public string ItemName { set; get; }
        public string Item { set; get; }
        public double Height { set; get; }
        public double Width { set; get; }
        public bool Locked { set; get; }
        public bool Visible { set; get; }
        public int SourceHeight { set; get; }
        public int SourceWidth { set; get; }
        public double Rotation { set; get; }

    }

 public class SceneItemDetails
    {
        public int ItemId { set; get; }
        public string SourceKind { set; get; }
        public string SourceName { set; get; }
        public SceneItemSourceType SourceType { set; get; }
    }

#endif

