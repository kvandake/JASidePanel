using System;
using UIKit;
using CoreAnimation;
using Foundation;
using CoreGraphics;

namespace JASidePanels
{
	public class JASidePanelController : UIViewController, IUIGestureRecognizerDelegate
	{
		public UIViewController LeftPanel { get; set; }

		private UIViewController centerPanel;
		public UIViewController CenterPanel 
		{ 
			get { return this.centerPanel; } 
			set {
				var previous = value;
				if (this.centerPanel != value)
				{
					this.centerPanel.RemoveObserver(this, "view");
					this.centerPanel.RemoveObserver(this, "viewControllers");
					this.centerPanel = value;
					this.centerPanel.AddObserver(this, "viewControllers", 0, this.Handle);
					this.centerPanel.AddObserver(this, "view", NSKeyValueObservingOptions.Initial, this.Handle);

					if (this.State == JASidePanelState.CenterVisible)
					{
						this.VisiblePanel = this.centerPanel;
					}

				}

			    if (this.IsViewLoaded && this.State == JASidePanelState.CenterVisible) 
				{
					this.SwapCenter(previous, 0, this.centerPanel);
			    } 
				else if (this.IsViewLoaded) 
				{
			        // update the state immediately to prevent user interaction on the side panels while animating
		        	JASidePanelState previousState = this.State;
					this.State = JASidePanelState.CenterVisible;
					UIView.Animate(0.2f, () =>
					{
						if (this.BounceOnCenterPanelChange) 
						{
							var x = (previousState == JASidePanelState.LeftVisible) ? this.View.Bounds.Width : -this.View.Bounds.Width;
							this.CenterPanelRestingFrame.X = x;
						}

						this.CenterPanelContainer.Frame = this.CenterPanelRestingFrame;
					}, () =>
					{
						this.SwapCenter(previous, previousState, this.centerPanel);
						this.ShowCenterPanel(true, false);
					}); 
			    }
			}
		}
		public UIViewController VisiblePanel { get; set; }

		private UIView LeftPanelContainer { get; set; }
		private UIView CenterPanelContainer { get; set; }

		private JASidePanelStyle style;
		public JASidePanelStyle Style
		{
			get { return this.style; }
			set
			{
				if (this.style != value)
				{
					this.style = value;
					if (this.IsViewLoaded)
					{
						this.ConfigureContainers();
						this.LayoutSideContainers(false, 0f);
					}
				}
			}
		}

		private JASidePanelState state;
		public JASidePanelState State
		{
			get { return this.state; }
			set
			{
				if (this.state != value)
				{
					this.state = value;
					switch (this.state)
					{
						case JASidePanelState.CenterVisible:
							this.VisiblePanel = this.CenterPanel;
							this.LeftPanelContainer.UserInteractionEnabled = false;
							break;
						case JASidePanelState.LeftVisible:
							this.VisiblePanel = this.LeftPanel;
							this.LeftPanelContainer.UserInteractionEnabled = true;
							break;
					}
				}
			}
		}

		public float LeftGapPercentage { get; set; }
		public float MinimumMovePercentage { get; set; }
		public float MaximumAnimationDuration { get; set; }
		public float BounceDuration { get; set; }
		public float BouncePercentage { get; set; }
		public bool PanningLimitedToTopViewController { get; set; }
		public bool RecognizesPanGesture { get; set; }
		public bool AllowLeftOverpan { get; set; }
		public bool BounceOnSidePanelOpen { get; set; }
		public bool BounceOnSidePanelClose { get; set; }
		public bool BounceOnCenterPanelChange { get; set; }
		public bool ShouldDelegateAutorotateToVisiblePanel { get; set; }
		public bool AllowLeftSwipe { get; set; }
		public bool CenterPanelHidden { get; set; }
		public bool ShouldResizeLeftPanel { get; set; }
		public nfloat LeftFixedWidth { get; set; }
		public CGRect CenterPanelRestingFrame { get; set; }

		public nfloat LeftVisibleWidth {
			get {
				if (this.CenterPanelHidden && this.ShouldResizeLeftPanel)
				{
					return this.View.Bounds.Width;
				}
				else 
				{
					return this.LeftFixedWidth != 0
							   ? this.LeftFixedWidth
								   : (nfloat)Math.Floor(this.View.Bounds.Width * this.LeftGapPercentage);
				}
			}
		}

		public JASidePanelController()
		{
			this.Init();
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			this.View.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;

			this.CenterPanelContainer = new UIView(this.View.Bounds);
			this.CenterPanelRestingFrame = this.CenterPanelContainer.Frame;
			this.CenterPanelHidden = false;

			this.LeftPanelContainer = new UIView(this.View.Bounds);
			this.LeftPanelContainer.Hidden = true;

			this.ConfigureContainers();

			this.View.AddSubview(this.CenterPanelContainer);
			this.View.AddSubview(this.LeftPanelContainer);

			this.State = JASidePanelState.CenterVisible;

			this.SwapCenter(null, 0, this.CenterPanel);
			this.View.BringSubviewToFront(this.CenterPanelContainer);
		}

		private void Init()
		{
			this.Style = JASidePanelStyle.SingleActive;
			this.LeftGapPercentage = 0.8f;
			this.MinimumMovePercentage = 0.15f;
			this.MaximumAnimationDuration = 0.2f;
			this.BounceDuration = 0.1f;
			this.BouncePercentage = 0.075f;
			this.PanningLimitedToTopViewController = true;
			this.RecognizesPanGesture = true;
			this.AllowLeftOverpan = true;
			this.BounceOnSidePanelOpen = true;
			this.BounceOnSidePanelClose = false;
			this.BounceOnCenterPanelChange = true;
			this.ShouldDelegateAutorotateToVisiblePanel = true;
			this.AllowLeftSwipe = true;
		}

		private void ConfigureContainers()
		{
			this.LeftPanelContainer.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleRightMargin;
			this.CenterPanelContainer.Frame = this.View.Bounds;
			this.CenterPanelContainer.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
		}

		private void LayoutSideContainers(bool animate, float duration)
		{
			var leftFrame = this.View.Bounds;
			if (this.Style == JASidePanelStyle.MultipleActive)
			{
				// left panel container
				leftFrame.Width = this.LeftVisibleWidth;
				leftFrame.X = this.CenterPanelContainer.Frame.X - leftFrame.Width;
			}
			else if (this.PushesSidePanels && !this.CenterPanelHidden)
			{
				leftFrame.X = this.CenterPanelContainer.Frame.X - this.LeftVisibleWidth;
			}
			this.LeftPanelContainer.Frame = leftFrame;

			this.StyleContainer(this.LeftPanelContainer, animate, duration);
		}

		private void StyleContainer(UIView container, bool animate, float duration)
		{
			UIBezierPath shadowPath = UIBezierPath.FromRoundedRect(container.Bounds, 0);
			if (animate)
			{
				CABasicAnimation animation = CABasicAnimation.FromKeyPath("shadowPath");
				animation.From = NSValue.FromObject(container.Layer.ShadowPath);
				animation.To = NSValue.FromObject(shadowPath.CGPath);
				animation.Duration = duration;

				container.Layer.AddAnimation(animation, "shadowPath");
			}
			container.Layer.ShadowPath = shadowPath.CGPath;
			container.Layer.ShadowColor = UIColor.Black.CGColor;
			container.Layer.ShadowRadius = 10.0f;
			container.Layer.ShadowOpacity = 0.75f;
			container.ClipsToBounds = false;
		}

		private void SwapCenter(UIViewController previous, JASidePanelState previousState, UIViewController next)
		{
			if (previous != next)
			{
				previous.WillMoveToParentViewController(null);
				previous.View.RemoveFromSuperview();
				previous.RemoveFromParentViewController();

				if (next != null)
				{
					this.LoadCenterPanelWithPreviousState(previousState);
					this.AddChildViewController(next);
					this.CenterPanelContainer.AddSubview(next.View);
					next.DidMoveToParentViewController(this);
				}
			}
		}

		private void LoadCenterPanelWithPreviousState(JASidePanelState previousState)
		{
			this._placeButtonForLeftPanel();
    
		    // for the multi-active style, it looks better if the new center starts out in it's fullsize and slides in
		    if (this.Style == JASidePanelStyle.MultipleActive) 
			{
				switch (previousState)
				{
					case JASidePanelState.LeftVisible:
						CGRect frame = this.CenterPanelContainer.Frame;
						frame.Width = this.View.Bounds.Width;
						this.CenterPanelContainer.Frame = frame;
						break;

					default:
						break;
				}
		    }
		    
		    this.CenterPanel.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
		    this.CenterPanel.View.Frame = this.CenterPanelContainer.Bounds;
			this.StylePanel(this.CenterPanel.View);
		}

		private void PressButtonForLeftPanel() 
		{
			if (this.LeftPanel != null)
			{
				var buttonController = this.CenterPanel;
				if (buttonController is UINavigationController) 
				{
					UINavigationController nav = (UINavigationController)buttonController;
					if (nav.ViewControllers.Length > 0) 
					{
						buttonController = nav.ViewControllers[0];
		            }

				}

				if (buttonController.NavigationItem.LeftBarButtonItem == null) 
				{   
		            buttonController.NavigationItem.LeftBarButtonItem = this.LeftButtonForCenterPanel();
		        }
    		}	
		}

		private UIBarButtonItem LeftButtonForCenterPanel() 
		{
			return new UIBarButtonItem(UIBarButtonSystemItem.Save, (s, e) => this.ToggleLeftPanel());
		}

		private void ToggleLeftPanel() 
		{
			if (this.State == JASidePanelState.LeftVisible)
			{
				this.ShowCenterPanel(true, false);
		    } 
			else if (this.State == JASidePanelState.CenterVisible) 
			{
		        this.ShowLeftPanel(true, false);
		    }
		}

		private void ShowCenterPanel(bool animated, bool shouldBounce)
		{
			this.State = JASidePanelState.CenterVisible;


			this.AdjustCenterFrame();
    
		    if (animated) {
		        this.AnimateCenterPanel(shouldBounce, ()=>
				{
					this.LeftPanelContainer.Hidden = true;
					this.UnloadPanels();
				});
		    } 
			else 
			{
		        this.VenterPanelContainer.Frame = this.CenterPanelRestingFrame;
				this.StyleContainer(this.CenterPanelContainer, false, 0);
		        if (this.Style == JASidePanelStyle.MultipleActive || this.PushesSidePanels) 
				{
					this.LayoutSideContainers(false, 0);
		        }

		        this.LeftPanelContainer.hidden = true;
		        this.UnloadPanels();
		    }
		    
			this.TapView = null;
			this.ToggleScrollsToTopForCenter(true, false, false);
		}

		private CGRect AdjustCenterFrame() 
		{
			var frame = this.View.Bounds;
			switch (this.State)
			{
				case JASidePanelState.CenterVisible:
						frame.X = 0.0f;
						if (this.Style == JASidePanelStyle.MultipleActive)
						{
							frame.Width = this.View.Bounds.Width;
						}
						break;
				case JASidePanelState.LeftVisible:
						frame.X = this.LeftVisibleWidth;
						if (this.Style == JASidePanelStyle.MultipleActive)
						{
							frame.Width = this.View.Bounds.Width - this.LeftVisibleWidth;
						}
						break;
			}
			this.CenterPanelRestingFrame = frame;
			return this.CenterPanelRestingFrame;
		}
	}
}