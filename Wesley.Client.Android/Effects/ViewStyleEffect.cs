﻿using Android.Animation;
using Android.Content.Res;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using Wesley.Client.Droid.Effects;
using Wesley.Client.Effects;
using System;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Color = Xamarin.Forms.Color;
using View = Android.Views.View;

[assembly: ExportEffect(typeof(ViewStyleDroidEffect), nameof(ViewStyleEffect))]
namespace Wesley.Client.Droid.Effects
{
    [Android.Runtime.Preserve(AllMembers = true)]
    public class ViewStyleDroidEffect : PlatformEffect
    {
        private DateTime _tapTime;
        private View _view;
        private Android.Graphics.Color _color;
        private RippleDrawable _ripple;
        private FrameLayout _viewOverlay;
        private Android.Graphics.Rect _rect;
        private bool _touchEndInside;

        public bool EnableRipple => Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop;

        public bool IsFastRenderers = Xamarin.Forms.Forms.Flags.Any(x => x == "FastRenderers_Experimental");

        protected override void OnAttached()
        {
            try
            {
                if (ViewEffect.IsTapFeedbackColorSet(Element))
                {
                    UpdateTapFeedbackColor();
                }

                if (ViewEffect.IsStyleSet(Element))
                {
                    UpdateStyle();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Cannot set property on attached control." + ex);
            }
        }

        protected override void OnDetached()
        {
            var renderer = Container as IVisualElementRenderer;
            if (renderer?.Element != null) // Check disposed
            {
                if (_viewOverlay != null)
                {
                    _viewOverlay.Touch -= OnTouch;
                }

                ViewOverlayCollector.Delete(Container, this);

                if (EnableRipple)
                {
                    RemoveRipple();
                }
            }
        }

        private void UpdateTapFeedbackColor()
        {
            _view = Control ?? Container;

            if (Control is Android.Widget.ListView)
            {
                //Except ListView because of Raising Exception OnClick
                return;
            }

            _viewOverlay = ViewOverlayCollector.Add(Container, this);

            if (EnableRipple)
            {
                AddRipple();
            }
            else
            {
                _viewOverlay.Touch += OnTouch;
            }

            UpdateEffectColor();
        }

        protected override void OnElementPropertyChanged(System.ComponentModel.PropertyChangedEventArgs args)
        {
            base.OnElementPropertyChanged(args);

            if (args.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
            {
                UpdateStyle();
            }
            else if (args.PropertyName == ViewEffect.TouchFeedbackColorProperty.PropertyName)
            {
                UpdateEffectColor();
            }
        }

        private void UpdateStyle()
        {
            var view = Control ?? Container;
            var context = view.Context;

            var bgColor = (Element as VisualElement)?.BackgroundColor ?? Color.Transparent;

            PaintDrawable paint = new PaintDrawable(bgColor.ToAndroid());
            GradientDrawable gradient = new GradientDrawable();
            paint.SetCornerRadius(context.ToPixels((double)Element.GetValue(ViewEffect.CornerRadiusProperty)));
            gradient.SetCornerRadius(context.ToPixels((double)Element.GetValue(ViewEffect.CornerRadiusProperty)));
            gradient.SetColor(Color.Transparent.ToAndroid());
            gradient.SetOrientation(GradientDrawable.Orientation.LeftRight);
            gradient.SetShape(ShapeType.Rectangle);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                view.Elevation = context.ToPixels((double)Element.GetValue(ViewEffect.ShadowOffsetYProperty));
                view.TranslationZ = context.ToPixels((double)Element.GetValue(ViewEffect.ShadowOffsetYProperty));
            }

            gradient.SetStroke(
                (int)context.ToPixels(ViewEffect.GetBorderWidth(Element)),
                ((Color)Element.GetValue(ViewEffect.BorderColorProperty)).ToAndroid());

            var layer = new LayerDrawable(
                new Drawable[]
                {
                    paint,
                    gradient
                });

            view.SetBackground(layer);
        }

        private void OnTouch(object sender, View.TouchEventArgs args)
        {
            switch (args.Event.Action)
            {
                case MotionEventActions.Down:
                    _tapTime = DateTime.Now;
                    _rect = new Android.Graphics.Rect(_viewOverlay.Left, _viewOverlay.Top, _viewOverlay.Right, _viewOverlay.Bottom);
                    TapAnimation(250, 0, 80);
                    break;

                case MotionEventActions.Move:
                    _touchEndInside = _rect.Contains(
                        _viewOverlay.Left + (int)args.Event.GetX(),
                        _viewOverlay.Top + (int)args.Event.GetY());
                    break;

                case MotionEventActions.Up:
                    if (_touchEndInside)
                    {
                        if ((DateTime.Now - _tapTime).Milliseconds > 1500)
                        {
                            _viewOverlay.PerformLongClick();
                        }
                        else
                        {
                            _viewOverlay.CallOnClick();
                        }
                    }

                    goto case MotionEventActions.Cancel;

                case MotionEventActions.Cancel:
                    args.Handled = false;
                    TapAnimation(250, 80);
                    break;
            }
        }

        private void UpdateEffectColor()
        {
            var color = ViewEffect.GetTouchFeedbackColor(Element);
            if (color == Color.Default)
            {
                return;
            }

            _color = color.ToAndroid();
            _color.A = 80;

            if (EnableRipple)
            {
                _ripple.SetColor(GetPressedColorSelector(_color));
            }
        }

        private void AddRipple()
        {
            var color = ViewEffect.GetTouchFeedbackColor(Element);
            if (color == Color.Default)
            {
                return;
            }

            _color = color.ToAndroid();
            _color.A = 80;

            _viewOverlay.Foreground = CreateRipple(Color.Accent.ToAndroid());
            _ripple.SetColor(GetPressedColorSelector(_color));
        }

        private void RemoveRipple()
        {
            if (_viewOverlay != null)
            {
                _viewOverlay.Foreground = null;
            }

            _ripple?.Dispose();
            _ripple = null;
        }

        private RippleDrawable CreateRipple(Android.Graphics.Color color)
        {
            if (Element is Layout)
            {
                var mask = new ColorDrawable(Android.Graphics.Color.White);
                return _ripple = new RippleDrawable(GetPressedColorSelector(color), null, mask);
            }

            var back = _view.Background;
            if (back == null)
            {
                var mask = new ColorDrawable(Android.Graphics.Color.White);
                return _ripple = new RippleDrawable(GetPressedColorSelector(color), null, mask);
            }

            if (back is RippleDrawable)
            {
                _ripple = (RippleDrawable)back.GetConstantState().NewDrawable();
                _ripple.SetColor(GetPressedColorSelector(color));

                return _ripple;
            }

            return _ripple = new RippleDrawable(GetPressedColorSelector(color), back, null);
        }

        private static ColorStateList GetPressedColorSelector(int pressedColor)
        {
            return new ColorStateList(
                new[]
                {
                    new int[]{}
                },
                new[]
                {
                    pressedColor,
                });
        }

        private void TapAnimation(long duration, byte startAlpha = 255, byte endAlpha = 0)
        {
            var start = _color;
            var end = _color;
            start.A = startAlpha;
            end.A = endAlpha;
            var animation = ObjectAnimator.OfObject(_viewOverlay, "BackgroundColor", new ArgbEvaluator(), start.ToArgb(), end.ToArgb());
            animation.SetDuration(duration);
            animation.RepeatCount = 0;
            animation.RepeatMode = ValueAnimatorRepeatMode.Restart;
            animation.Start();
            animation.AnimationEnd += AnimationOnAnimationEnd;
        }

        private void AnimationOnAnimationEnd(object sender, EventArgs eventArgs)
        {
            var anim = ((ObjectAnimator)sender);
            anim.AnimationEnd -= AnimationOnAnimationEnd;
            anim.Dispose();
        }
    }
}