using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Automation;
using System.Windows.Forms;

namespace LinkLimiter
{
    class Chrome
    {

        [DllImport("user32.dll")]
        private static extern bool EnumThreadWindows(uint dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("User32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr windowHandle, StringBuilder stringBuilder, int nMaxCount);

        [DllImport("user32.dll", EntryPoint = "GetWindowTextLength", SetLastError = true)]
        internal static extern int GetWindowTextLength(IntPtr hwnd);

        private static List<IntPtr> windowList;
        private static string _className;
        private static StringBuilder apiResult = new StringBuilder(256); //256 Is max class name length.
        private delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        public delegate bool Win32Callback(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll")]
        protected static extern bool EnumWindows(Win32Callback enumProc, IntPtr lParam);

        private static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            List<IntPtr> pointers = GCHandle.FromIntPtr(pointer).Target as List<IntPtr>;
            pointers.Add(handle);
            return true;
        }

        public int CheckChrome(string localApp, string domain)
        {
            int count = 0;
            string title = "";
            List<IntPtr> Windows = GetAllWindows();

            foreach (IntPtr window in Windows)
            {
                title = GetTitle(window, domain);
                if (title.Contains(localApp))
                {
                    count += InspectChromeObject(window, domain) ? 1 : 0;
                }
            }

            return count;

        }

        public List<IntPtr> GetAllWindows()
        {
            Win32Callback enumCallback = new Win32Callback(EnumWindow);
            List<IntPtr> pointers = new List<IntPtr>();
            GCHandle listHandle = GCHandle.Alloc(pointers);
            try
            {
                EnumWindows(enumCallback, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated) listHandle.Free();
            }
            return pointers;
        }
        public string GetTitle(IntPtr handle, string domain)
        {
            int length = GetWindowTextLength(handle);
            StringBuilder sb = new StringBuilder(length + 1);
            GetWindowText(handle, sb, sb.Capacity);
            return sb.ToString();
        }

        private List<string> WalkEnabledElements(AutomationElement rootElement, List<string> ret)
        {
            Condition condition1 = new PropertyCondition(AutomationElement.IsControlElementProperty, true);
            Condition condition2 = new PropertyCondition(AutomationElement.IsEnabledProperty, true);
            TreeWalker walker = new TreeWalker(new AndCondition(condition1, condition2));
            AutomationElement elementNode = walker.GetFirstChild(rootElement);
            AutomationElement elementNode2 = walker.GetNextSibling(rootElement);
            while (elementNode != null)
            {
                ret.Add(elementNode.Current.ControlType.LocalizedControlType);
                WalkEnabledElements(elementNode, ret);
                elementNode = walker.GetNextSibling(elementNode);
            }
            while (elementNode2 != null)
            {
                ret.Add(elementNode2.Current.ControlType.LocalizedControlType);
                WalkEnabledElements(elementNode2, ret);
                elementNode2 = walker.GetNextSibling(elementNode2);
            }
            return ret;
        }


        private static List<AutomationElement> GetEditElement(AutomationElement rootElement, List<AutomationElement> ret)
        {

            Condition isControlElementProperty = new PropertyCondition(AutomationElement.IsControlElementProperty, true);
            Condition isEnabledProperty = new PropertyCondition(AutomationElement.IsEnabledProperty, true);
            TreeWalker walker = new TreeWalker(new AndCondition(isControlElementProperty, isEnabledProperty));
            AutomationElement elementNode = walker.GetFirstChild(rootElement);
            while (elementNode != null)
            {
                if (elementNode.Current.ControlType.ProgrammaticName == "ControlType.Edit")
                    ret.Add(elementNode);
                GetEditElement(elementNode, ret);
                elementNode = walker.GetNextSibling(elementNode);
            }
            return ret;
        }
        private static bool InspectChromeObject(IntPtr handle, string domain)
        {
            AutomationElement elm = AutomationElement.FromHandle(handle);

            AutomationElement elmUrlBar = null;
            try
            {
                var elm1 = elm.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, "Google Chrome"));
                if (elm1 == null) { return false; } 

                List<AutomationElement> ret = new List<AutomationElement>();
                elmUrlBar = GetEditElement(elm1, ret)[0];
            }
            catch
            {
                return false;
            }

            if (elmUrlBar == null)
            {
                return false;
            }

            if ((bool)elmUrlBar.GetCurrentPropertyValue(AutomationElement.HasKeyboardFocusProperty))
            {
                return false;
            }
            AutomationPattern[] patterns = elmUrlBar.GetSupportedPatterns();
            if (patterns.Length == 1)
            {
                string ret = "";
                try
                {
                    ret = ((ValuePattern)elmUrlBar.GetCurrentPattern(patterns[0])).Current.Value;
                }
                catch { }
                if (ret != "")
                {
                    if (Regex.IsMatch(ret, @"^(https:\/\/)?[a-zA-Z0-9\-\.]+(\.[a-zA-Z]{2,4}).*$"))
                    {
                        if (!ret.StartsWith("http"))
                        {
                            ret = "http://" + ret;
                        }
                        var uri = new Uri(domain);
                        var ieUri = new Uri(ret);
                        if (ieUri.Host.Contains(uri.Host.ToString()))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            return false;

        }

    }
}
