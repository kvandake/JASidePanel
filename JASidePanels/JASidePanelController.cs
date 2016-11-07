using System;
using UIKit;
using CoreAnimation;
using Foundation;
using CoreGraphics;

namespace JASidePanels
{
    public class JASidePanelController : UIViewController, IUIGestureRecognizerDelegate
    {
        private UIViewController leftPanel;
        private UIViewController rightPanel;
        private UIViewController centerPanel;
        private UIView tapView;
        private JASidePanelStyle style;
        private JASidePanelState state;
        private UIImage defaultImage;
        private UIBarButtonItem leftBarButtonItem;
        private bool centerPanelHidden;
        private IDisposable centerViewNotificationToken;
        private IDisposable centerViewControllerNotificationToken;

        public JASidePanelController()
        {
            this.Initialize();
        }

        public UIViewController LeftPanel
        {
            get { return this.leftPanel; }
            set
            {
                if (this.leftPanel != value)
                {
                    this.leftPanel?.WillMoveToParentViewController(null);
                    this.leftPanel?.View.RemoveFromSuperview();
                    this.leftPanel?.RemoveFromParentViewController();
                    this.leftPanel = value;
                    if (this.leftPanel != null)
                    {
                        this.AddChildViewController(this.leftPanel);
                        this.leftPanel.DidMoveToParentViewController(this);
                        this.PlaceButtonForLeftPanel();
                    }

                    if (this.State == JASidePanelState.LeftVisible)
                    {
                        this.VisiblePanel = this.leftPanel;
                    }

                }

            }
        }

        public UIViewController CenterPanel
        {
            get { return this.centerPanel; }
            set
            {
                var previous = this.centerPanel;
                if (this.centerPanel != value)
                {
                    this.centerViewNotificationToken?.Dispose();
                    this.centerViewControllerNotificationToken?.Dispose();
                    this.centerPanel = value;
                    this.centerViewControllerNotificationToken = this.centerPanel.AddObserver("viewControllers", 0, (obj) =>
                    {
                        this.PlaceButtonForLeftPanel();
                    });

                    this.centerViewNotificationToken = this.centerPanel.AddObserver("view", NSKeyValueObservingOptions.Initial, (obj) =>
                    {
                        if (this.centerPanel.IsViewLoaded && this.RecognizesPanGesture)
                        {
                            this.AddPanGestureToView(this.CenterPanel.View);
                        }
                    });

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
                    UIView.AnimateNotify(0.2f, () =>
                    {
                        if (this.BounceOnCenterPanelChange)
                        {
                            var x = (previousState == JASidePanelState.LeftVisible)
                                ? this.View.Bounds.Width
                                      : -this.View.Bounds.Width;
                            this.CenterPanelRestingFrame.X = x;
                        }

                        this.CenterPanelContainer.Frame = this.CenterPanelRestingFrame;
                    }, finished =>
                    {
                        this.SwapCenter(previous, previousState, this.centerPanel);
                        this.ShowCenterPanel(true, false);
                    });
                }
            }
        }

        public UIViewController RightPanel
        {
            get { return this.rightPanel; }
            set
            {
                if (this.rightPanel != value)
                {
                    this.rightPanel?.WillMoveToParentViewController(null);
                    this.rightPanel?.View.RemoveFromSuperview();
                    this.rightPanel?.RemoveFromParentViewController();
                    this.rightPanel = value;
                    if (this.rightPanel != null)
                    {
                        this.AddChildViewController(this.rightPanel);
                        this.rightPanel.DidMoveToParentViewController(this);
                    }

                    if (this.State == JASidePanelState.RightVisible)
                    {
                        this.VisiblePanel = this.rightPanel;
                    }

                }

            }
        }

        public UIViewController VisiblePanel { get; set; }

        public UIView TapView
        {
            get { return this.tapView; }
            set
            {
                if (this.tapView != value)
                {
                    this.tapView?.RemoveFromSuperview();
                    this.tapView = value;
                    if (this.tapView != null)
                    {
                        this.tapView.Frame = this.CenterPanelContainer.Bounds;
                        this.tapView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
                        this.AddTapGestureToView(this.tapView);
                        if (this.RecognizesPanGesture)
                        {
                            this.AddPanGestureToView(this.tapView);
                        }

                        this.CenterPanelContainer.AddSubview(this.tapView);
                    }

                }

            }
        }

        public CGPoint LocationBeforePan { get; set; }

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
                            if (this.RightPanelContainer != null)
                                this.RightPanelContainer.UserInteractionEnabled = false;
                            break;
                        case JASidePanelState.LeftVisible:
                            this.VisiblePanel = this.LeftPanel;
                            this.LeftPanelContainer.UserInteractionEnabled = true;
                            break;
                        case JASidePanelState.RightVisible:
                            this.VisiblePanel = this.RightPanel;
                            if (this.RightPanelContainer != null)
                                this.RightPanelContainer.UserInteractionEnabled = true;
                            break;
                    }
                }
            }
        }

        public float LeftGapPercentage { get; set; }
        public float RightGapPercentage { get; set; }
        public float MinimumMovePercentage { get; set; }
        public float MaximumAnimationDuration { get; set; }
        public float BounceDuration { get; set; }
        public float BouncePercentage { get; set; }
        public bool PanningLimitedToTopViewController { get; set; }
        public bool RecognizesPanGesture { get; set; }
        public bool BounceOnSidePanelOpen { get; set; }
        public bool BounceOnSidePanelClose { get; set; }
        public bool BounceOnCenterPanelChange { get; set; }
        public bool ShouldDelegateAutorotateToVisiblePanel { get; set; }
        public bool AllowLeftSwipe { get; set; }
        public bool AllowLeftOverpan { get; set; }
        public bool AllowRightSwipe { get; set; }
        public bool AllowRightOverpan { get; set; }

        public bool ShouldResizeLeftPanel { get; set; }
        public bool ShouldResizeRightPanel { get; set; }
        public bool PushesSidePanels { get; set; }
        public bool CanUnloadLeftPanel { get; set; }
        public bool CanUnloadRightPanel { get; set; }
        public nfloat LeftFixedWidth { get; set; }
        public nfloat RightFixedWidth { get; set; }
        public CGRect CenterPanelRestingFrame;
        public nfloat ShadowRadius { get; set; }
        public float ShadowOpacity { get; set; }

        public bool ShowShadow { get; set; }
        public bool ShowStyling { get; set; }

        public UIImage DefaultImage
        {
            get
            {
                if (this.defaultImage == null)
                {
                    UIGraphics.BeginImageContextWithOptions(new CGSize(20, 13), false, 0);
                    UIColor.Black.SetFill();
                    UIBezierPath.FromRect(new CGRect(0, 0, 20, 1)).Fill();
                    UIBezierPath.FromRect(new CGRect(0, 5, 20, 1)).Fill();
                    UIBezierPath.FromRect(new CGRect(0, 10, 20, 1)).Fill();
                    UIColor.White.SetFill();
                    UIBezierPath.FromRect(new CGRect(0, 1, 20, 2)).Fill();
                    UIBezierPath.FromRect(new CGRect(0, 6, 20, 2)).Fill();
                    UIBezierPath.FromRect(new CGRect(0, 11, 20, 2)).Fill();
                    this.defaultImage = UIGraphics.GetImageFromCurrentImageContext();
                    UIGraphics.EndImageContext();
                }
                return this.defaultImage;
            }
        }

        public UIBarButtonItem LeftBarButtonItem
        {
            get
            {
                if (this.leftBarButtonItem == null)
                {
                    this.leftBarButtonItem = new UIBarButtonItem(this.DefaultImage,
                                                                 UIBarButtonItemStyle.Plain,
                                                                 (s, e) => this.ToggleLeftPanel());
                }
                return this.leftBarButtonItem;
            }
            set
            {
                this.leftBarButtonItem = value;
            }
        }

        public bool CenterPanelHidden
        {
            get { return this.centerPanelHidden; }
            set
            {
                this.centerPanelHidden = value;
                this.SetCenterPanelHidden(this.centerPanelHidden, false, 0);
            }
        }

        public nfloat LeftVisibleWidth
        {
            get
            {
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

        public nfloat RightVisibleWidth
        {
            get
            {
                if (this.CenterPanelHidden && this.ShouldResizeRightPanel)
                {
                    return this.View.Bounds.Width;
                }
                else
                {
                    return this.RightFixedWidth != 0
                               ? this.RightFixedWidth
                                   : (nfloat)Math.Floor(this.View.Bounds.Width * this.RightGapPercentage);
                }

            }
        }

        private UIView LeftPanelContainer { get; set; }
        private UIView CenterPanelContainer { get; set; }
        private UIView RightPanelContainer { get; set; }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            this.View.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;

            this.CenterPanelContainer = new UIView(this.View.Bounds);
            this.CenterPanelRestingFrame = this.CenterPanelContainer.Frame;
            this.CenterPanelHidden = false;

            this.LeftPanelContainer = new UIView(this.View.Bounds);
            this.LeftPanelContainer.Hidden = true;

            this.RightPanelContainer = new UIView(this.View.Bounds);
            this.RightPanelContainer.Hidden = true;

            this.ConfigureContainers();

            this.View.AddSubview(this.CenterPanelContainer);
            this.View.AddSubview(this.LeftPanelContainer);
            this.View.AddSubview(this.RightPanelContainer);

            this.State = JASidePanelState.CenterVisible;

            this.SwapCenter(null, 0, this.CenterPanel);
            this.View.BringSubviewToFront(this.CenterPanelContainer);
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            this.LayoutSideContainers(false, 0);
            this.LayoutSidePanels();
            this.CenterPanelContainer.Frame = this.AdjustCenterFrame();
            this.StyleContainer(this.CenterPanelContainer, false, 0);
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            this.AdjustCenterFrame();
        }

        public override bool ShouldAutorotateToInterfaceOrientation(UIInterfaceOrientation toInterfaceOrientation)
        {
            var visiblePanel = this.VisiblePanel;
            if (this.ShouldDelegateAutorotateToVisiblePanel)
            {
                return visiblePanel.ShouldAutorotateToInterfaceOrientation(toInterfaceOrientation);
            }
            else
            {
                return true;
            }

        }

        public override bool ShouldAutorotate()
        {
            var visiblePanel = this.VisiblePanel;
            if (this.ShouldDelegateAutorotateToVisiblePanel && visiblePanel != null)
            {
                return visiblePanel.ShouldAutorotate();
            }
            else
            {
                return true;
            }
        }

        public override void WillAnimateRotation(UIInterfaceOrientation toInterfaceOrientation, double duration)
        {
            this.CenterPanelContainer.Frame = this.AdjustCenterFrame();
            this.LayoutSideContainers(true, (float)duration);
            this.LayoutSidePanels();
            this.StyleContainer(this.CenterPanelContainer, true, (float)duration);
            if (this.CenterPanelHidden)
            {
                var frame = this.CenterPanelContainer.Frame;
                frame.X = this.State == JASidePanelState.LeftVisible
                    ? this.CenterPanelContainer.Frame.Width
                    : -this.CenterPanelContainer.Frame.Width;
                this.CenterPanelContainer.Frame = frame;
            }

        }

        public virtual void StylePanel(UIView panel)
        {
            if (this.ShowStyling)
            {
                panel.Layer.CornerRadius = 6;
                panel.ClipsToBounds = true;
            }
        }

        public void ToggleLeftPanel()
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

        public void ShowRightPanel(bool animated, bool shouldBounce)
        {
            this.State = JASidePanelState.RightVisible;
            this.LoadRightPanel();
            this.AdjustCenterFrame();
            if (animated)
            {
                this.AnimateCenterPanel(shouldBounce, null);
            }
            else
            {
                this.CenterPanelContainer.Frame = this.CenterPanelRestingFrame;
                this.StyleContainer(this.CenterPanelContainer, false, 0);

                if (this.Style == JASidePanelStyle.MultipleActive || this.PushesSidePanels)
                {
                    this.LayoutSideContainers(false, 0);
                }
            }

            if (this.Style == JASidePanelStyle.SingleActive)
            {
                this.TapView = new UIView();
            }

            this.ToggleScrollsToTopForCenter(false, false, true);
        }

        public void ShowCenterPanel(bool animated, bool shouldBounce)
        {
            this.State = JASidePanelState.CenterVisible;


            this.AdjustCenterFrame();

            if (animated)
            {
                this.AnimateCenterPanel(shouldBounce, () =>
                {
                    this.LeftPanelContainer.Hidden = true;
                    this.UnloadPanels();
                });
            }
            else
            {
                this.CenterPanelContainer.Frame = this.CenterPanelRestingFrame;
                this.StyleContainer(this.CenterPanelContainer, false, 0);
                if (this.Style == JASidePanelStyle.MultipleActive || this.PushesSidePanels)
                {
                    this.LayoutSideContainers(false, 0);
                }

                this.LeftPanelContainer.Hidden = true;
                this.UnloadPanels();
            }

            this.TapView = null;
            this.ToggleScrollsToTopForCenter(true, false, false);
        }

        public void ShowLeftPanel(bool animate, bool shouldBounce)
        {
            this.State = JASidePanelState.LeftVisible;
            this.LoadLeftPanel();

            this.AdjustCenterFrame();

            if (animate)
            {
                this.AnimateCenterPanel(shouldBounce, null);
            }
            else
            {
                this.CenterPanelContainer.Frame = this.CenterPanelRestingFrame;
                this.StyleContainer(this.CenterPanelContainer, false, 0);
                if (this.Style == JASidePanelStyle.MultipleActive || this.PushesSidePanels)
                {
                    this.LayoutSideContainers(false, 0);
                }

            }

            if (this.Style == JASidePanelStyle.SingleActive)
            {
                this.TapView = new UIView();
            }

            this.ToggleScrollsToTopForCenter(false, true, false);
        }

        [Export("gestureRecognizerShouldBegin:")]
        public bool ShouldBegin(UIGestureRecognizer recognizer)
        {
            if (recognizer.View == this.TapView)
            {
                return true;
            }
            else if (this.PanningLimitedToTopViewController && !this.IsOnTopLevelViewController(this.CenterPanel))
            {
                return false;
            }
            else if (recognizer is UIPanGestureRecognizer)
            {
                var pan = (UIPanGestureRecognizer)recognizer;
                var translate = pan.TranslationInView(this.CenterPanelContainer);

                //determine if right swipe is allowed
                if (translate.X < 0 && !this.AllowRightSwipe)
                {
                    return false;
                }

                //determine is left swipe is allowed
                if (translate.X > 0 && !this.AllowLeftSwipe)
                {
                    return false;
                }

                bool possible = translate.X != 0 && (Math.Abs(translate.Y) / Math.Abs(translate.X)) < 1.0f;
                if (possible
                    && (translate.X > 0 && this.LeftPanel != null)
                    || (translate.X < 0 && this.RightPanel != null))
                {
                    return true;
                }

            }

            return false;
        }

        private void Initialize()
        {
            this.Style = JASidePanelStyle.SingleActive;
            this.LeftGapPercentage = 0.9f;
            this.RightGapPercentage = 0.8f;
            this.MinimumMovePercentage = 0.15f;
            this.MaximumAnimationDuration = 0.2f;
            this.BounceDuration = 0.1f;
            this.BouncePercentage = 0.075f;
            this.ShadowRadius = 10f;
            this.ShadowOpacity = 0.75f;
            this.PanningLimitedToTopViewController = true;
            this.RecognizesPanGesture = true;
            this.AllowLeftOverpan = true;
            this.AllowRightOverpan = false;
            this.BounceOnSidePanelOpen = true;
            this.BounceOnSidePanelClose = false;
            this.BounceOnCenterPanelChange = false;
            this.ShouldDelegateAutorotateToVisiblePanel = true;
            this.AllowLeftSwipe = true;
            this.AllowRightSwipe = false;
            this.ShowShadow = true;
            this.ShowStyling = true;
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
            var rightFrame = this.View.Bounds;
            if (this.Style == JASidePanelStyle.MultipleActive)
            {
                // left panel container
                leftFrame.Width = this.LeftVisibleWidth;
                leftFrame.X = this.CenterPanelContainer.Frame.X - leftFrame.Width;

                rightFrame.Width = this.RightVisibleWidth;
                rightFrame.X = this.CenterPanelContainer.Frame.X + this.CenterPanelContainer.Frame.Width;
            }
            else if (this.PushesSidePanels && !this.CenterPanelHidden)
            {
                leftFrame.X = this.CenterPanelContainer.Frame.X - this.LeftVisibleWidth;
                rightFrame.X = this.CenterPanelContainer.Frame.X + this.CenterPanelContainer.Frame.Width;
            }
            this.LeftPanelContainer.Frame = leftFrame;
            this.RightPanelContainer.Frame = rightFrame;
            this.StyleContainer(this.LeftPanelContainer, animate, duration);
            this.StyleContainer(this.RightPanelContainer, animate, duration);
        }

        private void StyleContainer(UIView container, bool animate, float duration)
        {
            if (this.ShowShadow)
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
                container.Layer.ShadowRadius = this.ShadowRadius;
                container.Layer.ShadowOpacity = this.ShadowOpacity;
                container.ClipsToBounds = false;
            }
        }

        private void SwapCenter(UIViewController previous, JASidePanelState previousState, UIViewController next)
        {
            if (previous != next)
            {
                previous?.WillMoveToParentViewController(null);
                previous?.View.RemoveFromSuperview();
                previous?.RemoveFromParentViewController();

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
            this.PlaceButtonForLeftPanel();

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

        private void PlaceButtonForLeftPanel()
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

                if (buttonController != null
                    && buttonController.NavigationItem != null
                    && buttonController.NavigationItem.LeftBarButtonItem == null)
                {
                    buttonController.NavigationItem.LeftBarButtonItem = this.LeftBarButtonItem;
                }
            }
        }

        private void ToggleScrollsToTopForCenter(bool center, bool left, bool right)
        {
            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
            {
                this.ToggleScrollsToTop(center, this.CenterPanelContainer);
                this.ToggleScrollsToTop(left, this.LeftPanelContainer);
            }
        }

        private bool ToggleScrollsToTop(bool enabled, UIView view)
        {
            if (view is UIScrollView)
            {
                var scrollView = (UIScrollView)view;
                scrollView.ScrollsToTop = enabled;
                return true;
            }
            else
            {
                foreach (var subview in view.Subviews)
                {
                    if (this.ToggleScrollsToTop(enabled, subview))
                    {
                        return true;
                    }
                }
            }
            return false;
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
                case JASidePanelState.RightVisible:
                    frame.X = -this.RightVisibleWidth;
                    if (this.Style == JASidePanelStyle.MultipleActive)
                    {
                        frame.X = 0;
                        frame.Width = this.View.Bounds.Width - this.RightVisibleWidth;
                    }
                    break;
            }
            this.CenterPanelRestingFrame = frame;
            return this.CenterPanelRestingFrame;
        }

        private void LoadRightPanel()
        {
            this.LeftPanelContainer.Hidden = true;
            if (this.RightPanelContainer.Hidden && this.RightPanel != null)
            {
                if (this.RightPanel.View.Superview == null)
                {
                    this.LayoutSidePanels();
                    this.RightPanel.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
                    this.StylePanel(this.RightPanel.View);
                    this.RightPanelContainer.AddSubview(this.RightPanel.View);
                }

                this.RightPanelContainer.Hidden = false;
            }
        }

        private void LoadLeftPanel()
        {
            if (this.LeftPanelContainer.Hidden && this.LeftPanel != null)
            {
                if (this.LeftPanel.View.Superview == null)
                {
                    this.LayoutSidePanels();
                    this.LeftPanel.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
                    this.StylePanel(this.LeftPanel.View);
                    this.LeftPanelContainer.AddSubview(this.LeftPanel.View);
                }

                this.LeftPanelContainer.Hidden = false;
            }

        }

        private void LayoutSidePanels()
        {
            if (this.LeftPanel.IsViewLoaded)
            {
                var frame = this.LeftPanelContainer.Bounds;
                if (this.ShouldResizeLeftPanel)
                {
                    frame.Width = this.LeftVisibleWidth;
                }

                this.LeftPanel.View.Frame = frame;
            }

        }

        private void UnloadPanels()
        {
            if (this.CanUnloadLeftPanel && this.LeftPanel.IsViewLoaded)
            {
                this.LeftPanel.View.RemoveFromSuperview();
            }

            if (this.CanUnloadRightPanel && this.RightPanel.IsViewLoaded)
            {
                this.RightPanel.View.RemoveFromSuperview();
            }

        }

        private void AnimateCenterPanel(bool shouldBounce, Action completion)
        {
            var bounceDistance = (this.CenterPanelRestingFrame.X - this.CenterPanelContainer.Frame.X) * this.BouncePercentage;
            if (this.CenterPanelRestingFrame.Width > this.CenterPanelContainer.Frame.Width)
            {
                shouldBounce = false;
            }

            var duration = this.CalculateDuration();
            UIView.AnimateNotify(duration, 0, UIViewAnimationOptions.CurveLinear | UIViewAnimationOptions.LayoutSubviews,
                                 () =>
            {
                this.CenterPanelContainer.Frame = this.CenterPanelRestingFrame;
                this.StyleContainer(this.CenterPanelContainer, true, duration);
                if (this.Style == JASidePanelStyle.MultipleActive || this.PushesSidePanels)
                {
                    this.LayoutSideContainers(false, 0);
                }
            },
                                 (finished) =>
            {
                if (shouldBounce)
                {
                    if (this.State == JASidePanelState.CenterVisible)
                    {
                        if (bounceDistance > 0)
                        {
                            this.LoadLeftPanel();
                        }

                    }

                    UIView.AnimateNotify(BounceDuration, 0, UIViewAnimationOptions.CurveEaseOut,
                                         () =>
                    {
                        var bounceFrame = this.CenterPanelRestingFrame;
                        bounceFrame.X += bounceDistance;
                        this.CenterPanelContainer.Frame = bounceFrame;
                    },
                                         (finished1) =>
                    {
                        UIView.AnimateNotify(this.BounceDuration, 0, UIViewAnimationOptions.CurveEaseIn,
                                            () =>
                        {
                            this.CenterPanelContainer.Frame = this.CenterPanelRestingFrame;
                        },
                                             null);
                    });

                }
                else if (completion != null)
                {
                    completion();
                }

            });
        }

        private float CalculateDuration()
        {
            float remaining = (float)Math.Abs(this.CenterPanelContainer.Frame.X - this.CenterPanelRestingFrame.X);
            float max = (float)(this.LocationBeforePan.X == this.CenterPanelRestingFrame.X
                          ? remaining
                          : Math.Abs(this.LocationBeforePan.X - this.CenterPanelRestingFrame.X));
            return max > 0.0f
                          ? this.MaximumAnimationDuration * (remaining / max)
                              : this.MaximumAnimationDuration;
        }

        private void AddTapGestureToView(UIView view)
        {
            var tapGesture = new UITapGestureRecognizer((obj) =>
            {
                this.CenterPanelTapped();
            });
            view.AddGestureRecognizer(tapGesture);
        }

        private void CenterPanelTapped()
        {
            this.ShowCenterPanel(true, false);
        }

        private bool IsOnTopLevelViewController(UIViewController root)
        {
            if (root is UINavigationController)
            {
                var nav = (UINavigationController)root;
                return nav.ViewControllers.Length == 1;
            }
            else if (root is UITabBarController)
            {
                var tab = (UITabBarController)root;
                return this.IsOnTopLevelViewController(tab.SelectedViewController);
            }
            return false;
        }

        private void AddPanGestureToView(UIView view)
        {
            var panGesture = new UIPanGestureRecognizer(this.HandlePan);
            panGesture.Delegate = this;
            panGesture.MaximumNumberOfTouches = 1;
            panGesture.MinimumNumberOfTouches = 1;
            view.AddGestureRecognizer(panGesture);
        }


        CGRect frameUniq = CGRect.Empty;

        private void HandlePan(UIPanGestureRecognizer pan)
        {
            if (!this.RecognizesPanGesture)
            {
                return;
            }

            switch (pan.State)
            {
                case UIGestureRecognizerState.Began:
                    this.LocationBeforePan = this.CenterPanelContainer.Frame.Location;
                    break;

                case UIGestureRecognizerState.Changed:
                    var translate = pan.TranslationInView(this.CenterPanelContainer);
                    frameUniq = this.CenterPanelRestingFrame;
                    frameUniq.X += (nfloat)Math.Round(this.CorrectMovement(translate.X));

                    if (frameUniq.X <= 0)
                    {
                        this.CenterPanelContainer.Frame = new CGRect(0,
                                                                     this.CenterPanelContainer.Frame.Y,
                                                                     this.CenterPanelContainer.Frame.Width,
                                                                     this.CenterPanelContainer.Frame.Height);
                        return;
                    }

                    if (this.Style == JASidePanelStyle.MultipleActive)
                    {
                        frameUniq.Width = this.View.Bounds.Width - frameUniq.X;
                    }

                    this.CenterPanelContainer.Frame = frameUniq;
                    if (this.State == JASidePanelState.CenterVisible)
                    {
                        if (frameUniq.X > 0)
                        {
                            this.LoadLeftPanel();
                        }
                        else if (frameUniq.X < 0)
                        {
                            this.LoadRightPanel();
                        }
                    }

                    if (this.Style == JASidePanelStyle.MultipleActive || this.PushesSidePanels)
                    {
                        this.LayoutSideContainers(false, 0);
                    }
                    break;

                case UIGestureRecognizerState.Ended:
                    var deltaX = frameUniq.X - this.LocationBeforePan.X;
                    if (this.ValidateThreshold(deltaX))
                    {
                        this.CompletePan(deltaX);
                    }
                    else
                    {
                        this.UndoPan();
                    }
                    break;

                case UIGestureRecognizerState.Cancelled:
                    this.UndoPan();
                    break;
            }
        }

        private void CompletePan(nfloat deltaX)
        {
            switch (this.State)
            {
                case JASidePanelState.CenterVisible:
                    if (deltaX > 0)
                    {
                        this.ShowLeftPanel(true, this.BounceOnSidePanelOpen);
                    }
                    else
                    {
                        this.ShowRightPanel(true, this.BounceOnSidePanelOpen);
                    }
                    break;
                case JASidePanelState.LeftVisible:
                    this.ShowCenterPanel(true, this.BounceOnSidePanelClose);
                    break;
                case JASidePanelState.RightVisible:
                    this.ShowCenterPanel(true, this.BounceOnSidePanelOpen);
                    break;
            }
        }

        private void UndoPan()
        {
            switch (this.State)
            {
                case JASidePanelState.CenterVisible:
                    this.ShowCenterPanel(true, false);
                    break;
                case JASidePanelState.LeftVisible:
                    this.ShowLeftPanel(true, false);
                    break;
                case JASidePanelState.RightVisible:
                    this.ShowRightPanel(true, false);
                    break;
            }
        }

        private nfloat CorrectMovement(nfloat movement)
        {
            var position = this.CenterPanelRestingFrame.X + movement;
            if (this.State == JASidePanelState.CenterVisible)
            {
                if ((position > 0 && this.LeftPanel == null) || (position < 0 && this.RightPanel == null))
                {
                    return 0;
                }
                else if (!this.AllowLeftOverpan && position > this.LeftVisibleWidth)
                {
                    return this.LeftVisibleWidth;
                }
                else if (!this.AllowRightOverpan && position < -this.RightVisibleWidth)
                {
                    return -this.RightVisibleWidth;
                }
            }
            else if (this.State == JASidePanelState.RightVisible && !this.AllowRightOverpan)
            {
                if (position < -this.RightVisibleWidth)
                {
                    return 0;
                }
                else if ((this.Style == JASidePanelStyle.MultipleActive || this.PushesSidePanels) && position > 0)
                {
                    return -this.CenterPanelRestingFrame.X;
                }
                else if (position > this.RightPanelContainer.Frame.X)
                {
                    return this.RightPanelContainer.Frame.X - this.CenterPanelRestingFrame.X;
                }
            }
            else if (this.State == JASidePanelState.LeftVisible && !this.AllowLeftOverpan)
            {
                if (position > this.LeftVisibleWidth)
                {
                    return 0;
                }
                else if ((this.Style == JASidePanelStyle.MultipleActive || this.PushesSidePanels) && position < 0)
                {
                    return -this.CenterPanelRestingFrame.X;
                }
                else if (position < this.LeftPanelContainer.Frame.X)
                {
                    return this.LeftPanelContainer.Frame.X - this.CenterPanelRestingFrame.X;
                }
            }

            return movement;
        }

        private bool ValidateThreshold(nfloat movement)
        {
            var minimum = Math.Floor(this.View.Bounds.Width * this.MinimumMovePercentage);
            switch (this.State)
            {
                case JASidePanelState.CenterVisible:
                    return Math.Abs(movement) >= minimum;

                case JASidePanelState.LeftVisible:
                    return movement <= -minimum;
                case JASidePanelState.RightVisible:
                    return movement >= minimum;
            }
            return false;
        }

        private void SetCenterPanelHidden(bool centerPanelHidden1, bool animate, float duration)
        {
            if (centerPanelHidden1 != this.CenterPanelHidden && this.State == JASidePanelState.CenterVisible)
            {
                this.centerPanelHidden = centerPanelHidden1;
                duration = animate ? duration : 0;
                if (centerPanelHidden1)
                {
                    UIView.Animate(duration, () =>
                    {
                        var frame = this.CenterPanelContainer.Frame;
                        frame.X = this.State == JASidePanelState.LeftVisible
                            ? this.CenterPanelContainer.Frame.Width
                            : -this.CenterPanelContainer.Frame.Width;
                        this.CenterPanelContainer.Frame = frame;
                        this.LayoutSideContainers(false, 0);
                        if (this.ShouldResizeLeftPanel)
                        {
                            this.LayoutSidePanels();
                        }

                    },
                                  () =>
                    {
                        if (this.CenterPanelHidden)
                        {
                            this.HideCenterPanel();
                        }

                    });
                }
                else
                {
                    this.UnhideCenterPanel();
                    UIView.Animate(duration, () =>
                    {
                        if (this.State == JASidePanelState.LeftVisible)
                        {
                            this.ShowLeftPanel(false, false);
                        }

                        if (this.ShouldResizeLeftPanel)
                        {
                            this.LayoutSidePanels();
                        }

                    });
                }

            }

        }

        private void HideCenterPanel()
        {
            this.CenterPanelContainer.Hidden = true;
            if (this.CenterPanel.IsViewLoaded)
            {
                this.CenterPanel.View.RemoveFromSuperview();
            }

        }

        private void UnhideCenterPanel()
        {
            this.CenterPanelContainer.Hidden = false;
            if (this.CenterPanel.View.Superview == null)
            {
                this.CenterPanel.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
                this.CenterPanel.View.Frame = this.CenterPanelContainer.Bounds;
                this.StylePanel(this.CenterPanel.View);
                this.CenterPanelContainer.AddSubview(this.CenterPanel.View);
            }

        }
    }
}