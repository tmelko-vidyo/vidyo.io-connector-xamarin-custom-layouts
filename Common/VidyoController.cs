using System;
using System.ComponentModel;
using VidyoClient;
using Xamarin.Forms;
using VidyoConnector.Controls;
#if __ANDROID__
using Android.App;
#endif

namespace VidyoConnector
{
    public class VidyoController : IVidyoController, INotifyPropertyChanged, Connector.IConnect,
    Connector.IRegisterLogEventListener, Connector.IRegisterLocalCameraEventListener, Connector.IRegisterLocalMicrophoneEventListener, Connector.IRegisterLocalSpeakerEventListener,
    Connector.IRegisterRemoteCameraEventListener, Connector.IRegisterRemoteMicrophoneEventListener
    {
        /* Shared instance */
        private static readonly VidyoController _instance = new VidyoController();
        public static IVidyoController GetInstance() { return _instance; }

        private VidyoController() { }

        private Connector mConnector = null;

        /* Init Vidyo Client only once per app lifecycle */
        private bool mIsVidyoClientInitialized = false;

        private bool mIsDebugEnabled = false;
        private string mExperimentalOptions = null;
        private bool mCameraPrivacyState = false;

        private LocalCamera mLastSelectedCamera = null;
        private bool mAreDevicesDisabledForBackground = false;

        private RemoteCamera mActiveRemoteCamera = null;
        
        private Logger mLogger = Logger.GetInstance();

        private VidyoConnectorState mState;

        private Controls.NativeView mLocalFrame;
        private Controls.NativeView mRemoteFrame;

        public event PropertyChangedEventHandler PropertyChanged;

        public VidyoConnectorState ConnectorState
        {
            get { return mState; }
            set
            {
                mState = value;
                // Raise PropertyChanged event
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs("ConnectorState"));
            }
        }

        /* Initialize Vidyo Client. Called only once */
        private bool Initialize()
        {
            if (mIsVidyoClientInitialized)
            {
                return true;
            }

#if __ANDROID__
            ConnectorPKG.SetApplicationUIContext(Forms.Context as Activity);
#endif
            // Initialize VidyoClient library.
            // This should be called only once throughout the lifetime of the app.
            mIsVidyoClientInitialized = ConnectorPKG.Initialize();
            return mIsVidyoClientInitialized;
        }

        public String Construct(NativeView localView, NativeView remoteView)
        {
            bool result = Initialize();
            if (!result)
            {
                throw new Exception("Client initialization error.");
            }

            string clientVersion = "Failed";

            // Remember the reference to video views
            this.mLocalFrame = localView;
            this.mRemoteFrame = remoteView;

            mConnector = new Connector(IntPtr.Zero, /* Provide zero reference */
                                               Connector.ConnectorViewStyle.ConnectorviewstyleDefault,
                                               15,
                                               "info@VidyoClient info@VidyoConnector warning",
                                               "",
                                               0);
            // Get the version of VidyoClient
            clientVersion = mConnector.GetVersion();

            // If enableDebug is configured then enable debugging
            if (mIsDebugEnabled)
            {
                mConnector.EnableDebug(7776, "warning info@VidyoClient info@VidyoConnector");
            }

            // Set experimental options if any exist
            if (mExperimentalOptions != null)
            {
                ConnectorPKG.SetExperimentalOptions(mExperimentalOptions);
            }

            /* Local */
            if (!mConnector.RegisterLocalCameraEventListener(this))
            {
                mLogger.Log("RegisterLocalCameraEventListener failed!");
            }

            if (!mConnector.RegisterLocalSpeakerEventListener(this))
            {
                mLogger.Log("RegisterLocalSpeakerEventListener failed!");
            }

            if (!mConnector.RegisterLocalMicrophoneEventListener(this))
            {
                mLogger.Log("RegisterLocalMicrophoneEventListener failed!");
            }

            /* Remote */
            if (!mConnector.RegisterRemoteCameraEventListener(this))
            {
                mLogger.Log("RegisterRemoteCameraEventListener failed!");
            }

            if (!mConnector.RegisterRemoteMicrophoneEventListener(this))
            {
                mLogger.Log("RegisterRemoteMicrophoneEventListener failed!");
            }

            /* Register for log callbacks */
            if (!mConnector.RegisterLogEventListener(this, "info@VidyoClient info@VidyoConnector warning"))
            {
                mLogger.Log("VidyoConnector RegisterLogEventListener failed");
            }

            mLogger.Log("Connector instance has been created.");
            return clientVersion;
        }

        /* App state changed to background mode */
        public void OnAppSleep()
        {
            if (mConnector != null)
            {
                mConnector.SetCameraPrivacy(true);
                mConnector.SetMode(Connector.ConnectorMode.ConnectormodeBackground);

                /* Specific for castom layouts logic to handle camera backgrounding */
                if (!mAreDevicesDisabledForBackground && !mCameraPrivacyState && mLocalFrame != null)
                {
                    mAreDevicesDisabledForBackground = true;
                    ReleaseLocalCamera();
                }
            }
        }

        /* App state changed to foreground mode */
        public void OnAppResume()
        {
            if (mConnector != null)
            {
                mConnector.SetCameraPrivacy(this.mCameraPrivacyState);
                mConnector.SetMode(Connector.ConnectorMode.ConnectormodeForeground);

                /* Specific for castom layouts logic to handle camera backgrounding */
                if (this.mLastSelectedCamera != null && !mCameraPrivacyState && mAreDevicesDisabledForBackground)
                {
                    mAreDevicesDisabledForBackground = false;
                    mConnector.SelectLocalCamera(mLastSelectedCamera);
                }
            }
        }

        public bool Connect(string host, string token, string displayName, string resourceId)
        {
            return mConnector.Connect(host, token, displayName, resourceId, this);
        }

        public void Disconnect()
        {
            mConnector.Disconnect();
        }

        public void CleanUp()
        {
            mConnector.UnregisterLogEventListener();

            mConnector.UnregisterLocalCameraEventListener();
            mConnector.UnregisterLocalMicrophoneEventListener();
            mConnector.UnregisterLocalSpeakerEventListener();

            mConnector.UnregisterRemoteCameraEventListener();
            mConnector.UnregisterRemoteMicrophoneEventListener();

            mConnector.SelectLocalMicrophone(null);
            mConnector.SelectLocalSpeaker(null);

            mLastSelectedCamera = null;

            ReleaseLocalCamera();
            ReleaseRemoteCamera();

            mConnector.Disable();
            mConnector = null;

            mLogger.Log("Connector instance has been released.");
        }

        // Set the microphone privacy
        public void SetMicrophonePrivacy(bool privacy)
        {
            mConnector.SetMicrophonePrivacy(privacy);
        }

        // Set the camera privacy
        public void SetCameraPrivacy(bool privacy)
        {
            this.mCameraPrivacyState = privacy;
            mConnector.SetCameraPrivacy(privacy);

            if (this.mCameraPrivacyState)
            {
                mConnector.SelectLocalCamera(null);
                mConnector.HideView(mLocalFrame.Handle);
            }
            else
            {
                mConnector.SelectLocalCamera(this.mLastSelectedCamera);
            }
        }

        // Cycle the camera
        public void CycleCamera()
        {
            mConnector.CycleCamera();
        }

        // Refresh renderer holders
        public void RefreshUI()
        {
            RenderLocalCamera();
            RenderRemoteCamera();
        }

        /**
         * Handle custom views
         */

        private void RenderLocalCamera()
        {
            if (mLocalFrame == null || mConnector == null || mLastSelectedCamera == null)
            {
                mLogger.Log("Unable to refresh local frame.");
                return;
            }

            uint actualWidth = mLocalFrame.NativeWidth;
            uint actualHeight = mLocalFrame.NativeHeight;

            if (actualWidth != 0 && actualHeight != 0 && mLocalFrame.Handle != IntPtr.Zero)
            {
                mConnector.AssignViewToLocalCamera(mLocalFrame.Handle, this.mLastSelectedCamera, true, true);
                mConnector.ShowViewAtPoints(mLocalFrame.Handle, 0, 0, actualWidth, actualHeight);

                mLogger.Log("VidyoConnector ShowViewAtPoints Local View: x = 0, y = 0, w = " + actualWidth + ", h = " + actualHeight);
            }
            else
            {
                mLogger.Log("Local frame is not yet ready");
            }
        }

        private void ReleaseLocalCamera()
        {
            if (this.mConnector != null && this.mLocalFrame != null)
            {
                this.mConnector.SelectLocalCamera(null);
                this.mConnector.HideView(mLocalFrame.Handle);
            }
        }

        private void RenderRemoteCamera()
        {
            if (mConnector == null || mRemoteFrame == null || mActiveRemoteCamera == null)
            {
                mLogger.Log("Unable to refresh remote frame.");
                return;
            }

            uint actualWidth = this.mRemoteFrame.NativeWidth;
            uint actualHeight = this.mRemoteFrame.NativeHeight;

            if (actualWidth != 0 && actualHeight != 0)
            {
                mConnector.AssignViewToRemoteCamera(mRemoteFrame.Handle, mActiveRemoteCamera, true, true);
                mConnector.ShowViewAtPoints(mRemoteFrame.Handle, 0, 0, actualWidth, actualHeight);

                mLogger.Log("VidyoConnector ShowViewAtPoints Remote View: x = 0, y = 0, w = " + actualWidth + ", h = " + actualHeight);
            }
            else
            {
                mLogger.Log("Remote view is not available.");
            }
        }

        private void ReleaseRemoteCamera()
        {
            if (this.mConnector != null && mRemoteFrame != null)
            {
                mConnector.HideView(mRemoteFrame.Handle);
            }
        }

        /*
         * Library callback Functions. Called on library thread.
         */

        public void OnSuccess()
        {
            mLogger.Log("OnSuccess");
            ConnectorState = VidyoConnectorState.VidyoConnectorStateConnected;
        }

        public void OnFailure(Connector.ConnectorFailReason reason)
        {
            mLogger.Log("OnFailure");
            ConnectorState = VidyoConnectorState.VidyoConnectorStateConnectionFailure;
        }

        public void OnDisconnected(Connector.ConnectorDisconnectReason reason)
        {
            mLogger.Log("OnDisconnected");
            ConnectorState = (reason == Connector.ConnectorDisconnectReason.ConnectordisconnectreasonDisconnected) ?
                VidyoConnectorState.VidyoConnectorStateDisconnected : VidyoConnectorState.VidyoConnectorStateDisconnectedUnexpected;
        }

        public void OnLog(LogRecord logRecord)
        {
            mLogger.LogClientLib(logRecord.message);
        }

        public void EnableDebugging()
        {
            mConnector.EnableDebug(7776, "warning info@VidyoClient info@VidyoConnector");
        }

        public void DisableDebugging()
        {
            mConnector.DisableDebug();
        }

        private void AssertRemoteCamera(RemoteCamera camera, Participant participant, String action)
        {
            if (camera == null)
            {
                throw new Exception("Remote camera was null at action: " + action);
            }

            if (participant == null)
            {
                throw new Exception("Remote participant was null at action: " + action);
            }
        }

        private void AssertRemoteMic(RemoteMicrophone microphone, Participant participant, String action)
        {
            if (microphone == null)
            {
                throw new Exception("Remote microphone was null at action: " + action);
            }

            if (participant == null)
            {
                throw new Exception("Remote participant was null at action: " + action);
            }
        }

        /* Local camera callbacks */

        public void OnLocalCameraAdded(LocalCamera localCamera)
        {
            mLogger.Log("OnLocalCameraAdded");
        }

        public void OnLocalCameraRemoved(LocalCamera localCamera)
        {
            mLogger.Log("OnLocalCameraRemoved");
            mLastSelectedCamera = null;
        }

        public void OnLocalCameraSelected(LocalCamera localCamera)
        {
            mLogger.Log("OnLocalCameraSelected");
            if (localCamera != null)
            {
                this.mLastSelectedCamera = localCamera;

                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    RenderLocalCamera();
                });
            }
        }

        public void OnLocalCameraStateUpdated(LocalCamera localCamera, VidyoClient.Device.DeviceState state)
        {
            mLogger.Log("OnLocalCameraStateUpdated");
        }

        /* Remote camera callbacks */

        public void OnRemoteCameraAdded(RemoteCamera remoteCamera, Participant participant)
        {
            AssertRemoteCamera(remoteCamera, participant, "Added");
            mLogger.Log("OnRemoteCameraAdded");
            
            if (remoteCamera != null)
            {
                this.mActiveRemoteCamera = remoteCamera;
                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    RenderRemoteCamera();
                });
            }
        }

        public void OnRemoteCameraRemoved(RemoteCamera remoteCamera, Participant participant)
        {
            AssertRemoteCamera(remoteCamera, participant, "Removed");
            mLogger.Log("OnRemoteCameraRemoved");

            this.mActiveRemoteCamera = null;
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                ReleaseRemoteCamera();
            });
        }

        public void OnRemoteCameraStateUpdated(RemoteCamera remoteCamera, Participant participant, VidyoClient.Device.DeviceState state)
        {
            mLogger.Log("OnRemoteCameraStateUpdated");
        }

        public void OnRemoteMicrophoneAdded(RemoteMicrophone remoteMicrophone, Participant participant)
        {
            AssertRemoteMic(remoteMicrophone, participant, "Added");
        }

        public void OnRemoteMicrophoneRemoved(RemoteMicrophone remoteMicrophone, Participant participant)
        {
            AssertRemoteMic(remoteMicrophone, participant, "Removed");
        }

        public void OnRemoteMicrophoneStateUpdated(RemoteMicrophone remoteMicrophone, Participant participant, VidyoClient.Device.DeviceState state)
        {

        }

        public void OnLocalMicrophoneAdded(LocalMicrophone localMicrophone)
        {

        }

        public void OnLocalMicrophoneRemoved(LocalMicrophone localMicrophone)
        {

        }

        public void OnLocalMicrophoneSelected(LocalMicrophone localMicrophone)
        {

        }

        public void OnLocalMicrophoneStateUpdated(LocalMicrophone localMicrophone, VidyoClient.Device.DeviceState state)
        {

        }

        public void OnLocalSpeakerAdded(LocalSpeaker localSpeaker)
        {

        }

        public void OnLocalSpeakerRemoved(LocalSpeaker localSpeaker)
        {

        }

        public void OnLocalSpeakerSelected(LocalSpeaker localSpeaker)
        {

        }

        public void OnLocalSpeakerStateUpdated(LocalSpeaker localSpeaker, VidyoClient.Device.DeviceState state)
        {

        }
    }
}