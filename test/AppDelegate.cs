using Foundation;
using JASidePanels;
using UIKit;

namespace test
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the
	// User Interface of the application, as well as listening (and optionally responding) to application events from iOS.
	[Register("AppDelegate")]
	public class AppDelegate : UIApplicationDelegate
	{
		// class-level declarations

		public override UIWindow Window { get; set; }

        JASidePanelController controller;

		public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
		{
            this.controller = new JASidePanelController();
            //this.controller.ShowShadow = false;
            this.controller.ShowStyling = false;
            this.controller.ShouldDelegateAutorotateToVisiblePanel = false;
            this.controller.LeftPanel = new UIViewController()
            {
                View = new UIView
                {
                    BackgroundColor = UIColor.Orange
                }
            };

            this.controller.CenterPanel = new UINavigationController(new MyViewController());

            this.Window = new UIWindow(UIScreen.MainScreen.Bounds);
            this.Window.RootViewController = controller;
            this.Window.MakeKeyAndVisible();
			return true;
		}

		public override void OnResignActivation(UIApplication application)
		{
		}

		public override void DidEnterBackground(UIApplication application)
		{
		}

		public override void WillEnterForeground(UIApplication application)
		{
		}

		public override void OnActivated(UIApplication application)
		{
		}

		public override void WillTerminate(UIApplication application)
		{
		}
	}
}

