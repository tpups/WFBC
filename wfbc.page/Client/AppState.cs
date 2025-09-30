using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace WFBC.Client
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
                DrawerClosed = true;
                DrawerMinified = false;
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
        
        // COMMENTED OUT - Using mobile behavior up to 900px instead of tablet logic
        // public void SetIsMedium(bool tablet)
        // {
        //     if (tablet == true)
        //     {
        //         IsMedium = true;
        //         IsXSmall = false;
        //         IsLarge = false;
        //         DrawerClosed = false;
        //         DrawerMinified = true;
        //         DrawerCssClass = "drawer-mini";
        //         SideMenuCssClass = "slide-out";
        //         MobileCssClass = "tablet";
        //         DrawerActive = "active";
        //         Minified = "minified";
        //         NotifyStateChanged();
        //     }
        // }
        public bool IsLarge { get; private set; }
        public void SetIsLarge(bool large)
        {
            if (large == true)
            {
                IsLarge = true;
                IsMedium = false;
                IsXSmall = false;
                DrawerClosed = false;
                DrawerMinified = false;
                DrawerCssClass = "drawer-full";
                SideMenuCssClass = "slide-in";
                DrawerActive = "active";
                Minified = "";
                NotifyStateChanged();
            }
        }
        // Manager Menu
        public bool OpenManagerMenu { get; private set; }
        public void SetOpenManagerMenu(bool toggle = false, bool close = false)
        {
            OpenManagerMenu = toggle ? !OpenManagerMenu : false;
            OpenManagerMenu = close ? false : OpenManagerMenu;
            ManagerMenuCssClass = OpenManagerMenu ? "manager-menu-open" : "manager-menu-closed";
            NotifyStateChanged();
        }
        // Drawer open/closed state
        public bool DrawerClosed { get; private set; }
        public void SetDrawerClosed(bool closed = false, bool toggle = false)
        {
            DrawerClosed = toggle ? !DrawerClosed : closed;
            DrawerCssClass = DrawerClosed ? "drawer-closed" : "drawer-full";
            SideMenuCssClass = DrawerClosed ? "slide-out" : "slide-in";
            DrawerMinified = false;
            DrawerActive = DrawerClosed ? "" : "active";
            Minified = "";
            NotifyStateChanged();
        }
        // Drawer minified state
        public bool DrawerMinified { get; private set; }
        public void SetDrawerMinified(bool minify = false, bool toggle = false)
        {
            DrawerMinified = toggle ? !DrawerMinified : minify;
            DrawerCssClass = DrawerMinified ? "drawer-mini" : "drawer-full";
            SideMenuCssClass = DrawerMinified ? "slide-out" : "slide-in";
            DrawerClosed = false;
            DrawerActive = "active";
            Minified = DrawerMinified ? "minified" : "";
            NotifyStateChanged();
        }
        public string DrawerCssClass { get; private set; }
        public string SideMenuCssClass { get; private set; }
        public string MobileCssClass { get; private set; }
        public string ManagerMenuCssClass { get; private set; }
        public string DrawerActive { get; private set; }
        public string Minified { get; protected set; }

        public event Action OnChange;

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
