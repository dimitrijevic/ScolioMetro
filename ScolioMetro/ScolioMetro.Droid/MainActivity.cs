using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using OxyPlot.Xamarin.Forms.Platform.Android;
using Xamarin.Forms.Platform.Android;

namespace ScolioMetro.Droid
{
    [Activity(Label = "ScolioMetro", Icon = "@drawable/icon",
        ScreenOrientation = ScreenOrientation.SensorLandscape,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : FormsApplicationActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            Window.AddFlags(WindowManagerFlags.Fullscreen);
            Window.ClearFlags(WindowManagerFlags.ForceNotFullscreen);
            Forms.Init();
            Xamarin.Forms.Forms.Init(this, bundle);
            LoadApplication(new App());
        }
    }
}