using System;
using Android.App;
using Android.Content.PM;
using Android.OS;

namespace VidyoConnector.Android
{
    [Activity(Label = "VidyoConnector Xamarin Custom Layouts",
    Icon = "@drawable/vidyo_io_icon",
    Theme = "@style/MyTheme",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation,
    ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            try
            {
                LoadApplication(new App(VidyoController.GetInstance()));
            }
            catch (Exception e)
            {
                Logger.GetInstance().Log(e.Message);
            }
        }
    }
}