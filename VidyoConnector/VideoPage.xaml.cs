using System;
using System.ComponentModel;
using Xamarin.Forms;

namespace VidyoConnector
{
    public partial class VideoPage : ContentPage
    {
        IVidyoController mVidyoController = null;

        ViewModel mViewModel = ViewModel.GetInstance();

        Logger mLogger = Logger.GetInstance();

        double mPageWidth = 0;
        double mPageHeight = 0;

        bool mAllowReconnect = true;

        public VideoPage()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, true);

            BindingContext = this.mViewModel;
        }

        public void Initialize(IVidyoController vidyoController)
        {
            this.mVidyoController = vidyoController;
            this.mViewModel.ClientVersion = this.mVidyoController.Construct(_localView, _remoteView);

            INotifyPropertyChanged i = (INotifyPropertyChanged)this.mVidyoController;
            i.PropertyChanged += new PropertyChangedEventHandler(VidyoControllerPropertyChanged);
        }

        void VidyoControllerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ConnectorState")
            {
                VidyoConnectorState = mVidyoController.ConnectorState;
            }
        }

        void OnConnectButtonClicked(object sender, EventArgs args)
        {
            if (mViewModel.CallAction == VidyoCallAction.VidyoCallActionConnect)
            {
                mViewModel.ToolbarStatus = "Connecting...";

                if (!mVidyoController.Connect(mViewModel.Host, mViewModel.Token, mViewModel.DisplayName, mViewModel.ResourceId))
                {
                    VidyoConnectorState = VidyoConnectorState.VidyoConnectorStateConnectionFailure;
                }
                else
                {
                    mViewModel.CallAction = VidyoCallAction.VidyoCallActionDisconnect;

                    // Display the loading animation
                    _loadingForm.IsVisible = true;
                }
            }
            else
            {
                mViewModel.ToolbarStatus = "Disconnecting...";
                mVidyoController.Disconnect();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            mLogger.Log("On Disappearing.");

            if (mVidyoController != null) mVidyoController.CleanUp();
        }

        void OnCameraPrivacyButtonClicked(object sender, EventArgs args)
        {
            mVidyoController.SetCameraPrivacy(mViewModel.ToggleCameraPrivacy());
        }

        void OnMicrophonePrivacyButtonClicked(object sender, EventArgs args)
        {
            mVidyoController.SetMicrophonePrivacy(mViewModel.ToggleMicrophonePrivacy());
        }

        void OnCycleCameraButtonClicked(object sender, EventArgs args)
        {
            mVidyoController.CycleCamera();
        }

        void DiagnosticsButtonClicked(object sender, EventArgs args)
        {
            mViewModel.DisplayDiagnostics = !mViewModel.DisplayDiagnostics;
            if (mViewModel.DisplayDiagnostics)
            {
                ClientVersionLabel.IsVisible = true;
                mVidyoController.EnableDebugging();
            }
            else
            {
                ClientVersionLabel.IsVisible = false;
                mVidyoController.DisableDebugging();
            }
        }

        VidyoConnectorState VidyoConnectorState
        {
            set
            {
                // UI updates should be made on main thread
                Device.BeginInvokeOnMainThread(() =>
                {
                    // Update the toggle connect button to either start call or end call image
                    mViewModel.CallAction = value == VidyoConnectorState.VidyoConnectorStateConnected ?
                        VidyoCallAction.VidyoCallActionDisconnect : VidyoCallAction.VidyoCallActionConnect;

                    // Set the status text in the toolbar
                    mViewModel.ToolbarStatus = VidyoDefs.StateDescription[value];

                    if (value == VidyoConnectorState.VidyoConnectorStateConnected)
                    {

                    }
                    else
                    {
                        // VidyoConnector is disconnected

                        // If the allow-reconnect flag is set to false and a normal (non-failure) disconnect occurred,
                        // then disable the toggle connect button, in order to prevent reconnection.
                        if (!mAllowReconnect && (value == VidyoConnectorState.VidyoConnectorStateDisconnected))
                        {
                            _toggleConnectButton.IsEnabled = false;
                            mViewModel.ToolbarStatus = "Call ended";
                        }
                    }

                    // Hide the loading animation
                    _loadingForm.IsVisible = false;
                });
            }
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            mLogger.Log("Size allocated called.");

            if (Math.Abs(width - mPageWidth) > 0.001 || Math.Abs(height - mPageHeight) > 0.001)
            {
                mPageWidth = width;
                mPageHeight = height;

                if (mVidyoController != null)
                {
                    mVidyoController.RefreshUI();
                }
            }
        }
    }
}