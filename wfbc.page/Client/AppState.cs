using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace wfbc.page.Client
{
    public class AppState
    {
        // <576px portrait phones
        public bool IsXSmall { get; private set; }
        public void SetIsXSmall(bool mobile)
        {
            if (mobile == true)
            {
                IsXSmall = true;
                IsMedium = false;
                CloseDrawer = true;
                MinifyDrawer = false;
                DrawerCssClass = "drawer-closed";
                SideMenuCssClass = "slide-out";
                NotifyStateChanged();
            }
        }
        // <992px landscape phones, tablets, small desktop windows
        public bool IsMedium { get; private set; }
        public void SetIsMedium(bool tablet)
        {
            if (tablet == true)
            {
                IsMedium = true;
                IsXSmall = false;
                IsLarge = false;
                CloseDrawer = false;
                MinifyDrawer = true;
                DrawerCssClass = "drawer-mini";
                SideMenuCssClass = "slide-out";
                NotifyStateChanged();
            }
        }
        public bool IsLarge { get; private set; }
        public void SetIsLarge(bool large)
        {
            if (large == true)
            {
                IsLarge = true;
                IsMedium = false;
                IsXSmall = false;
                CloseDrawer = false;
                MinifyDrawer = false;
                DrawerCssClass = "drawer-full";
                SideMenuCssClass = "slide-in";
                NotifyStateChanged();
            }
        }
        // drawer open/closed state
        public bool CloseDrawer { get; private set; }
        public void SetCloseDrawer(bool closed = false, bool toggle = false)
        {
            CloseDrawer = toggle ? !CloseDrawer : closed;
            DrawerCssClass = CloseDrawer ? "drawer-closed" : "drawer-full";
            SideMenuCssClass = CloseDrawer ? "slide-out" : "slide-in";
            MinifyDrawer = false;
            NotifyStateChanged();
        }
        // drawer minified state
        public bool MinifyDrawer { get; private set; }
        public void SetMinifyDrawer(bool minify = false, bool toggle = false)
        {
            MinifyDrawer = toggle ? !MinifyDrawer : minify;
            DrawerCssClass = MinifyDrawer ? "drawer-mini" : "drawer-full";
            SideMenuCssClass = MinifyDrawer ? "slide-out" : "slide-in";
            CloseDrawer = false;
            NotifyStateChanged();
        }
        public string DrawerCssClass { get; private set; }
        public string SideMenuCssClass { get; private set; }
        public string MobileCssClass { get; private set; }

        public event Action OnChange;

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
