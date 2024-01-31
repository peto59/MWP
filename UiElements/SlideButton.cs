using System;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace MWP
{
    public delegate void SlideEventHandler(object sender, EventArgs e);
    
    public class SlideButton : SeekBar
    {
        private Drawable? thumb;

        public SlideButton(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public override void SetThumb(Drawable thumb)
        {
            this.thumb = thumb;
            this.Background = thumb;
        }
        
        
        public override bool OnTouchEvent(MotionEvent? ev)
        {
            if (ev is { Action: MotionEventActions.Down })
            {
                if (thumb != null && thumb.Bounds.Contains((int)ev.RawX, (int)ev.RawY))
                {
                    base.OnTouchEvent(ev);
                }
                else
                {
                    return false;
                }
            }
            else if (ev is { Action: MotionEventActions.Up })
            {
                if (Progress > 70)
                {
                    HandleSlide();
                }

                Progress = 0;
            }
            else
            {
                base.OnTouchEvent(ev);
            }

            return true;
        }
        
        
        public event SlideEventHandler Slide;

        protected virtual void OnSlide()
        {
            Slide?.Invoke(this, EventArgs.Empty);
        }

        private void HandleSlide()
        {
            OnSlide();
        }

     
    }
}