using System;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.Android;
using Android.Runtime;
using Android.App;
using Xamarin.Forms.Platform.Android;
using Android.Views;
using System.ComponentModel;
using System.Reflection;
using Android.Widget;
using ZXing.Mobile;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Android.Hardware;

[assembly: ExportRenderer(typeof(ZXingScannerView), typeof(ZXingScannerViewRenderer))]
namespace ZXing.Net.Mobile.Forms.Android
{
	[Preserve(AllMembers = true)]
	public class ZXingScannerViewRenderer : ViewRenderer<ZXingScannerView, ZXing.Mobile.ZXingSurfaceView>
	{
		public ZXingScannerViewRenderer(global::Android.Content.Context context)
			: base(context)
		{
		}

		public static void Init()
		{
			// Keep linker from stripping empty method
			var temp = DateTime.Now;
		}

		protected ZXingScannerView formsView;

		protected ZXingSurfaceView zxingSurface;
		internal Task<bool> requestPermissionsTask;

		protected override async void OnElementChanged(ElementChangedEventArgs<ZXingScannerView> e)
		{
			base.OnElementChanged(e);

			formsView = Element;

			if (zxingSurface == null)
			{

				// Process requests for autofocus
				formsView.AutoFocusRequested += (x, y) =>
				{
					if (zxingSurface != null)
					{
						if (x < 0 && y < 0)
							zxingSurface.AutoFocus();
						else
							zxingSurface.AutoFocus(x, y);
					}
				};

				var cameraPermission = await Xamarin.Essentials.Permissions.RequestAsync<Xamarin.Essentials.Permissions.Camera>();
				if (cameraPermission != Xamarin.Essentials.PermissionStatus.Granted)
				{
					Console.WriteLine("Missing Camera Permission");
					return;
				}

				if (Xamarin.Essentials.Permissions.IsDeclaredInManifest("android.permission.FLASHLIGHT"))
				{
					var fp = await Xamarin.Essentials.Permissions.RequestAsync<Xamarin.Essentials.Permissions.Flashlight>();
					if (fp != Xamarin.Essentials.PermissionStatus.Granted)
					{
						Console.WriteLine("Missing Flashlight Permission");
						return;
					}
				}

				zxingSurface = new ZXingSurfaceView(Context as Activity, formsView.Options);
                //zxingSurface.LayoutParameters = new LinearLayout.LayoutParams(100, 100);
                
				//zxingSurface.SetX(100);
				//zxingSurface.SetY(100);
                //zxingSurface.LayoutParameters = new ActionBar.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
				//zxingSurface.LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent);

				base.SetNativeControl(zxingSurface);

				//this.AddView(zxingSurface);

				if (formsView.IsScanning)
					zxingSurface.StartScanning(formsView.RaiseScanResult, formsView.Options);

				if (formsView.IsTorchOn)
					zxingSurface.Torch(true);
			}
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (zxingSurface == null)
				return;

			switch (e.PropertyName)
			{
				case nameof(ZXingScannerView.IsTorchOn):
					zxingSurface.Torch(formsView.IsTorchOn);
					break;
				case nameof(ZXingScannerView.IsScanning):
					if (formsView.IsScanning)
						zxingSurface.StartScanning(formsView.RaiseScanResult, formsView.Options);
					else
						zxingSurface.StopScanning();
					break;
				case nameof(ZXingScannerView.IsAnalyzing):
					if (formsView.IsAnalyzing)
						zxingSurface.ResumeAnalysis();
					else
						zxingSurface.PauseAnalysis();
					break;
			}
		}

		volatile bool isHandlingTouch = false;

		public override bool OnTouchEvent(MotionEvent e)
		{
			if (!isHandlingTouch)
			{
				isHandlingTouch = true;

				try
				{
					var x = e.GetX();
					var y = e.GetY();

					if (Control != null)
						Control.AutoFocus((int)x, (int)y);
				}
				finally
				{
					isHandlingTouch = false;
				}
			}

			return base.OnTouchEvent(e);
		}

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            base.OnLayout(changed, l, t, r, b);

            if (zxingSurface.CameraResolution != null)
            {
                var screenHeight = this.Height;
				var screenWidth = this.Width;

                var size = AdjustSize(zxingSurface.CameraResolution.Height, zxingSurface.CameraResolution.Width,
                    screenWidth, screenHeight);

				Control.Layout(0, 0, size.Width, size.Height);
            }

            //GetChildAt(0).Layout(100, 100, 100, 100);
            //GetChildAt(0).Layout(0, 0, r - l, b - t);
        }

        private System.Drawing.Size AdjustSize(int previewWidth, int previewHeight, int targetWidth, int targetHeight)
        {
            var previewAspect = (double)previewWidth / previewHeight;
            var targetAspect = (double)targetWidth / targetHeight;

			int newWidth, newHeight;

            if (previewAspect <= targetAspect)
            {
                newWidth = targetWidth;
                newHeight = (int)(targetWidth / previewAspect);
            }
            else
            {
                newHeight = targetHeight;
				newWidth = (int)(targetHeight * previewAspect);
            }

            return new System.Drawing.Size(newWidth, newHeight);
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

			Control.Measure(widthMeasureSpec, heightMeasureSpec);
            //GetChildAt(0).Measure(widthMeasureSpec, heightMeasureSpec);
        }
    }
}

