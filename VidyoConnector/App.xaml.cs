using System.Diagnostics;
using Xamarin.Forms;

namespace VidyoConnector
{
    public partial class App : Application
    {
        private IVidyoController mVidyoController = null;

        /* Need this in order to see preview in App.xaml interface builder */
        public App()
        {
            InitializeComponent();
            MainPage = new HomePage();
        }

        public App(IVidyoController vidyoController)
        {
            InitializeComponent();
            this.mVidyoController = vidyoController;

            HomePage homePage = new HomePage(vidyoController);
            MainPage = homePage;
        }

        /* Receive background state event */
        protected override void OnSleep()
        {
            // Handle when your app sleeps
            Debug.WriteLine("OnSleep");
            mVidyoController.OnAppSleep();
        }

        /* Receive foreground state event */
        protected override void OnResume()
        {
            // Handle when your app resumes
            Debug.WriteLine("OnResume");
            mVidyoController.OnAppResume();
        }
    }
}