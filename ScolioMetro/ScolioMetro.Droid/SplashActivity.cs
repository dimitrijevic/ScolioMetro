using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using ScolioMetro.Droid;

namespace com.ScolioMetro.Droid
{
    [Activity(Theme = "@style/Theme.Splash",
        MainLauncher = true,
        NoHistory = true,
        ScreenOrientation = ScreenOrientation.SensorLandscape,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class SplashActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            Window.AddFlags(WindowManagerFlags.Fullscreen);
            Window.ClearFlags(WindowManagerFlags.ForceNotFullscreen);
            System.Threading.Thread.Sleep(1000);
            StartActivity(typeof (MainActivity));
            // Create your application here
        }
    }
}