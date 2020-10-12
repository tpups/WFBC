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
                MobileCssClass = "mobile";
                DrawerActive = "";
                Minified = "";
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
                DrawerActive = "active";
                Minified = "minified";
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
                DrawerActive = "active";
                Minified = "";
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
            DrawerActive = CloseDrawer ? "" : "active";
            Minified = "";
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
            DrawerActive = "active";
            Minified = MinifyDrawer ? "minified" : "";
            NotifyStateChanged();
        }
        public string DrawerCssClass { get; private set; }
        public string SideMenuCssClass { get; private set; }
        public string MobileCssClass { get; private set; }
        public string DrawerActive { get; private set; }
        public string Minified { get; protected set; }

        public event Action OnChange;

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
