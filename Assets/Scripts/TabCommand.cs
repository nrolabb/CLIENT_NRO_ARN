using System;

namespace Game1
{
    
    public class TabCommand
    {
        private string caption;
        public int x, y, w = 32, h = 32;
        private bool isFocus;
        private Action action;
        private static Image menu, menu1;
        public TabCommand(string caption, Action action)
        {
            this.caption = caption;
            this.action = action;
        }
        public static void loadBG()
        {
            menu = GameCanvas.loadImage("/mainImage/myTexture2dnut.png");
            menu1 = GameCanvas.loadImage("/mainImage/myTexture2dnutf.png");
        }
        public void paint(mGraphics g)
        {
            g.drawImage(isFocus ? menu1 : menu, x, y);
            mFont.tahoma_7b_dark.drawString(g, caption, x + w / 2 + 1, y + h / 2 - mFont.tahoma_7b_dark.getHeight() / 2, 3);
        }
    
        public bool isPointerInside()
        {
            isFocus = false;
            if (GameCanvas.isPointerHoldIn(x, y, w, h))
            {
                if (GameCanvas.isPointerDown)
                {
                    isFocus = true;
                }
                if (GameCanvas.isPointerClick && GameCanvas.isPointerJustRelease)
                {
                    return true;
                }
            }
            return false;
        }
        public void Invoke()
        {
            GameCanvas.clearAllPointerEvent();
            action?.Invoke();
        }
    }
}
