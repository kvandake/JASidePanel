using System;

using UIKit;

namespace test
{
    public partial class MyViewController : UIViewController
    {
        public MyViewController() : base("MyViewController", null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            var b = new UIButton();
            b.SetTitle("asdasd" , UIControlState.Normal);
            b.Center = this.View.Center;
            b.TouchUpInside += (sender, e) => {
                NavigationController.PushViewController(new UIViewController()
                {
                    View = new UIView()
                    {
                        BackgroundColor = UIColor.Blue
                    }
                }, true);
            };
            this.View.AddSubview(b);
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }
    }
}


