using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace SuperCom.Utils
{
    public class VisualHelper
    {
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject obj) where T : DependencyObject
        {
            if (obj != null)
            {
                if (obj is T)
                    yield return obj as T;

                foreach (DependencyObject child in LogicalTreeHelper.GetChildren(obj).OfType<DependencyObject>())
                    foreach (T c in FindVisualChildren<T>(child))
                        yield return c;
            }
        }
        public static DependencyObject FindChild(DependencyObject parent, string name)
        {
            // confirm parent and name are valid.
            if (parent == null || string.IsNullOrEmpty(name)) return null;

            if (parent is FrameworkElement && (parent as FrameworkElement).Name == name) return parent;

            DependencyObject result = null;

            if (parent is FrameworkElement) (parent as FrameworkElement).ApplyTemplate();

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                result = FindChild(child, name);
                if (result != null) break;
            }

            return result;
        }

        /// <summary>
        /// Looks for a child control within a parent by type
        /// </summary>
        public static T FindChild<T>(DependencyObject parent)
            where T : DependencyObject
        {
            // confirm parent is valid.
            if (parent == null) return null;
            if (parent is T) return parent as T;

            DependencyObject foundChild = null;

            if (parent is FrameworkElement) (parent as FrameworkElement).ApplyTemplate();

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                foundChild = FindChild<T>(child);
                if (foundChild != null) break;
            }

            return foundChild as T;
        }

        public static childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }
        public static T FindElementByName<T>(FrameworkElement element, string sChildName) where T : FrameworkElement
        {
            T childElement = null;
            if (element == null) return childElement;
            var nChildCount = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < nChildCount; i++)
            {
                FrameworkElement child = VisualTreeHelper.GetChild(element, i) as FrameworkElement;

                if (child == null)
                    continue;

                if (child is T && child.Name.Equals(sChildName))
                {
                    childElement = (T)child;
                    break;
                }

                childElement = FindElementByName<T>(child, sChildName);

                if (childElement != null)
                    break;
            }

            return childElement;
        }
    }
}
