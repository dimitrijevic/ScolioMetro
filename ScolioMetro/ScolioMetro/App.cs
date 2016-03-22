using System;
using System.Collections.Generic;
using DeviceMotion.Plugin;
using DeviceMotion.Plugin.Abstractions;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Xamarin.Forms;
using Plugin.Share;
using Xamarin.Forms;

namespace ScolioMetro
{
    public class App : Application
    {
        private double _accelPitch;
        private double _accelRoll;

        private double averageRoll;
        private double min, max, level;

        private readonly PlotModel model;
        private readonly PieSeries psOuter;
        private readonly PieSeries psInner;

        private readonly List<double> Rolls;
        private TimeSpan accelTimeSpan;
        private DateTime tempTime;

        private bool averaging = false;
        private bool calibrating = false;
        private double accelsPerSec = 25;

        public App()
        {
            Rolls = new List<double>();
            model = new PlotModel();
            ToggleAverageMode();

            psOuter = new PieSeries();
            psOuter.Slices.Add(new PieSlice("", 1) { IsExploded = true });
            psOuter.OutsideLabelFormat = "";
            psOuter.TextColor = OxyColor.Parse("#FF000000");
            psOuter.InnerDiameter = 0;
            psOuter.FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label));
            psOuter.InsideLabelFormat = "0°";
            psOuter.ExplodedDistance = 0.0;
            psOuter.Selectable = true;
            psOuter.Stroke = OxyColors.White;
            psOuter.StrokeThickness = 2.0;
            psOuter.InsideLabelPosition = 0.5;
            psOuter.InsideLabelColor = OxyColor.Parse("#FF000000");
            psOuter.AngleSpan = 0;
            psOuter.StartAngle = -90;
            psOuter.Diameter = 0.85;
            model.Series.Add(psOuter);

            psInner = new PieSeries();
            psInner.Slices.Add(new PieSlice("", 1) { IsExploded = true });
            psInner.TextColor = OxyColor.Parse("#FF000000");
            psInner.OutsideLabelFormat = "";
            psInner.TickRadialLength = 0;
            psInner.TickHorizontalLength = 0;
            psInner.FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label));
            psInner.InnerDiameter = 0.75;
            psInner.ExplodedDistance = 0.0;
            psInner.Selectable = false;
            psInner.Stroke = OxyColors.Black;
            psInner.StrokeThickness = 2.0;
            psInner.InsideLabelPosition = 0.0;
            psInner.AreInsideLabelsAngled = true;
            psInner.AngleSpan = 2;
            psInner.StartAngle = -89;

            model.Series.Add(psInner);

            psOuter.TouchCompleted += Ps_TouchCompleted;
            psOuter.MouseDown += Ps_MouseDown;
            psInner.TouchCompleted += Ps_TouchCompleted;
            psInner.MouseDown += Ps_MouseDown;

            MainPage = new NavigationPage(new ContentPage
            {
                Title = "ScolioMetro",
                Padding = new Thickness(0, 0, 0, 0),
                Content = new AbsoluteLayout
                {
                    VerticalOptions = LayoutOptions.Fill,
                    HorizontalOptions = LayoutOptions.Fill,
                    Children =
                    {
                        {
                            new PlotView
                            {
                                Model = model,
                                BackgroundColor = Color.White,
                                VerticalOptions = LayoutOptions.Fill,
                                HorizontalOptions = LayoutOptions.Fill
                            },
                            new Rectangle(0, 0, 1, 1),
                            AbsoluteLayoutFlags.All
                        },
                        {
                            new StackLayout
                            {
                                Children =
                                {
                                    new Label
                                    {
                                        Text =
                                            "ScolioMetro meassures patient's trunk asymmetry and its angle of rotation",
                                        TextColor = Color.Aqua,
                                        FontAttributes = FontAttributes.Bold,
                                        FontSize = Device.GetNamedSize(NamedSize.Small, typeof (Label)),
                                        VerticalOptions = LayoutOptions.CenterAndExpand,
                                        HorizontalOptions = LayoutOptions.CenterAndExpand
                                    },
                                    new Label
                                    {
                                        Text =
                                            "hold phone to the patient's back, placing it on spine vertebrae and read the angles",
                                        TextColor = Color.Aqua,
                                        FontAttributes = FontAttributes.Italic,
                                        FontSize = Device.GetNamedSize(NamedSize.Small, typeof (Label)),
                                        VerticalOptions = LayoutOptions.CenterAndExpand,
                                        HorizontalOptions = LayoutOptions.CenterAndExpand
                                    }
                                }
                            },
                            new Rectangle(0.5, 1, 1, 0.5),
                            AbsoluteLayoutFlags.All
                        }
                    }
                }
            })
            {
                BarBackgroundColor = Color.White,
                BarTextColor = Color.Aqua,
                Title = "ScolioMetro",
                BackgroundColor = Color.White
            };

            MainPage.ToolbarItems.Add(new ToolbarItem("Reset", Device.OnPlatform("", "", "Assets/Icons/reset.png"), () =>
            {
                max = 0;
                min = 0;
                level = 0;
            }, ToolbarItemOrder.Primary, 0));
            MainPage.ToolbarItems.Add(new ToolbarItem("Calibrate",
                string.Format("{0}{1}", Device.OnPlatform("Icons/", "", "Assets/Icons/"), "action.png"), () =>
                {
                    max = 0;
                    min = 0;
                    level = _accelRoll;
                }, ToolbarItemOrder.Primary, 0));
            MainPage.ToolbarItems.Add(new ToolbarItem("About...", "",
                () =>
                {
                    if (MainPage.Navigation.NavigationStack.Count == 1) MainPage.Navigation.PushAsync(new AboutPage());
                },
                ToolbarItemOrder.Secondary));
            MainPage.ToolbarItems.Add(new ToolbarItem("Toggle average", "", ToggleAverageMode,
                ToolbarItemOrder.Secondary));
            MainPage.ToolbarItems.Add(new ToolbarItem("Share your app...", "", () =>
            {
                CrossShare.Current.ShareLink("http://smarturl.it/ScolioMetro", "Try the app", "ScolioMetro mimics scoliometer");
            }, ToolbarItemOrder.Secondary));
        }

        private void ToggleAverageMode()
        {

            averaging ^= true;
            calibrating = true;
            tempTime = DateTime.Now;
            if (calibrating) CrossDeviceMotion.Current.SensorValueChanged += Calibrating;

        }

        private void Calibrating(object sender, SensorValueChangedEventArgs e)
        {
            if (averaging && calibrating)
            {
                accelTimeSpan = DateTime.Now - tempTime;
                if (accelTimeSpan.Milliseconds != 0)
                {
                    accelsPerSec = Math.Round(2000f / accelTimeSpan.Milliseconds, 0);
                    //Debug.WriteLine("Acceleromters per seconds:" + accelsPerSec);
                    calibrating = false;
                }
                else
                {
                    accelsPerSec = 25;
                    calibrating = false;
                }
            }

            if (!averaging || !calibrating)
                CrossDeviceMotion.Current.SensorValueChanged -= Calibrating;
        }

        public MotionVector Accelerometer { get; set; }
        public MotionVector Gyroscope { get; set; }

        private void Ps_MouseDown(object sender, OxyMouseDownEventArgs e)
        {
            min = 0;
            max = 0;
            level = 0;
            e.Handled = true;
            //Debug.WriteLine("calibration mousedown event...");
        }

        private void Ps_TouchCompleted(object sender, OxyTouchEventArgs e)
        {
            min = 0;
            max = 0;
            level = 0;
            e.Handled = true;
            //Debug.WriteLine("calibration touch event...");
        }

        protected override void OnStart()
        {
            //CrossDeviceMotion.Current.Start(MotionSensorType.Gyroscope);
            CrossDeviceMotion.Current.Start(MotionSensorType.Accelerometer, MotionSensorDelay.Fastest);
            CrossDeviceMotion.Current.SensorValueChanged += Current_SensorValueChanged;
        }

        protected override void OnSleep()
        {
            CrossDeviceMotion.Current.SensorValueChanged -= Current_SensorValueChanged;
            CrossDeviceMotion.Current.Stop(MotionSensorType.Accelerometer);
            //CrossDeviceMotion.Current.Stop(MotionSensorType.Gyroscope);
        }

        protected override void OnResume()
        {
            //CrossDeviceMotion.Current.Start(MotionSensorType.Gyroscope);
            CrossDeviceMotion.Current.Start(MotionSensorType.Accelerometer, MotionSensorDelay.Fastest);
            CrossDeviceMotion.Current.SensorValueChanged += Current_SensorValueChanged;
        }

        private void Current_SensorValueChanged(object sender, SensorValueChangedEventArgs a)
        {
            switch (a.SensorType)
            {
                case MotionSensorType.Accelerometer:
                    
                    Accelerometer = (MotionVector)a.Value;

                    _accelPitch = 180 / Math.PI *
                                  Math.Atan(Accelerometer.X /
                                            Math.Sqrt(Math.Pow(Accelerometer.Y, 2) + Math.Pow(Accelerometer.Z, 2)));
                    _accelRoll = 180 / Math.PI *
                                 Math.Atan(Accelerometer.Y /
                                           Math.Sqrt(Math.Pow(Accelerometer.X, 2) + Math.Pow(Accelerometer.Z, 2))) -
                                 level;
                    break;

                case MotionSensorType.Gyroscope:

                    Gyroscope = (MotionVector)a.Value;
                    break;
            }


            if (averaging)
            {
                min = _accelRoll;
                max = _accelRoll;
                Rolls.Add(_accelRoll);
                foreach (var m in Rolls)
                {
                    if (min > m) min = m;
                }                       
                foreach (var m in Rolls)
                {
                    if (max < m) max = m;
                }
                foreach (var roll in Rolls)
                {
                    _accelRoll += roll;
                }

                _accelRoll /= (Rolls.Count + 1);
                if (Rolls.Count > accelsPerSec) Rolls.RemoveAt(0);

            }
            else
            {
                if (_accelRoll * Math.Sign(_accelPitch) > 0)
                {
                    if (Math.Abs(_accelRoll) > max) max = Math.Abs(_accelRoll);
                }
                else
                {
                    if (Math.Abs(_accelRoll) > min) min = Math.Abs(_accelRoll);
                }
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                if (Math.Abs(min) < 5 && Math.Abs(max) < 5)
                {
                    psOuter.Slices[0].Fill = OxyColor.FromArgb(255, 0, 255, 0);
                }
                else
                {
                    if (Math.Abs(min) < 7 && Math.Abs(max) < 7)
                        psOuter.Slices[0].Fill = OxyColor.FromArgb(255, 255, 255, 0);
                    else
                        psOuter.Slices[0].Fill = OxyColor.FromArgb(255, 255, 0, 0);
                }

                if (averaging)
                {
                    psOuter.AngleSpan = Math.Abs(Math.Abs(max) - Math.Abs(min)) + 5;
                    psOuter.StartAngle = -90 - max * Math.Sign(_accelPitch) - 1.5;
                }
                else
                {
                    psOuter.AngleSpan = min + max;
                    psOuter.StartAngle = -90 - max;
                }

                psInner.StartAngle = -90 - _accelRoll * Math.Sign(_accelPitch);
                psOuter.InsideLabelFormat = Math.Round(Math.Abs(_accelRoll), 1) + "°";
                psOuter.OutsideLabelFormat = Math.Round(Math.Abs(min), 1) + "°-" + Math.Round(Math.Abs(max), 1) + "°";
                model.Series.Clear();
                model.Series.Add(psOuter);
                model.Series.Add(psInner);
                model.InvalidatePlot(true);
            });
        }
    }
}