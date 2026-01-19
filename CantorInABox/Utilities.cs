using System.Collections;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Mooseware.CantorInABox
{
    /// <summary>
    /// Utility functions for system interop and drag-drop facilitation
    /// </summary>
    public partial class Utilities
    {
        // Kernel functions needed to request that the machine not go into power plan induced sleep or hibernation...
        [Flags]
        public enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
            // Legacy flag, should not be used.
            // ES_USER_PRESENT = 0x00000004
        }
        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        /// <summary>
        /// Tell the OS not to let the session go to sleep or shut off the screen (while the app is running)
        /// </summary>
        public static void SetAlwaysOnPowerHandling()
        {
            try
            {
                SetThreadExecutionState(EXECUTION_STATE.ES_DISPLAY_REQUIRED | EXECUTION_STATE.ES_AWAYMODE_REQUIRED | EXECUTION_STATE.ES_CONTINUOUS);
            }
            catch (Exception ex)
            {   // For debugging only...
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// The the OS that it is OK to let the session go to sleep or shut off the screen again (app closing)
        /// </summary>
        public static void ReturnToNormalPowerHandling()
        {
            try
            {
                SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
            }
            catch (Exception ex)
            {   // For debugging only...
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        // Finds the orientation of the panel of the ItemsControl that contains the itemContainer passed as a parameter.
        // The orientation is needed to figure out where to draw the adorner that indicates where the item will be dropped.
        public static bool HasVerticalOrientation(FrameworkElement itemContainer)
        {
            bool hasVerticalOrientation = true;
            if (itemContainer != null)
            {
                Panel? panel = VisualTreeHelper.GetParent(itemContainer) as Panel;
                StackPanel? stackPanel;
                WrapPanel? wrapPanel;

                if ((stackPanel = panel as StackPanel) is not null)
                {
                    hasVerticalOrientation = (stackPanel.Orientation == Orientation.Vertical);
                }
                else if ((wrapPanel = panel as WrapPanel) != null)
                {
                    hasVerticalOrientation = (wrapPanel.Orientation == Orientation.Vertical);
                }
                // You can add support for more panel types here.
            }
            return hasVerticalOrientation;
        }

        public static void InsertItemInItemsControl(ItemsControl itemsControl, object itemToInsert, int insertionIndex)
        {
            if (itemToInsert != null)
            {
                IEnumerable itemsSource = itemsControl.ItemsSource;

                if (itemsSource == null)
                {
                    itemsControl.Items.Insert(insertionIndex, itemToInsert);
                }
                // Is the ItemsSource IList or IList<T>? If so, insert the dragged item in the list.
                else if (itemsSource is IList list)
                {
                    list.Insert(insertionIndex, itemToInsert);
                }
                else
                {
                    Type type = itemsSource.GetType();
                    Type? genericIListType = type.GetInterface("IList`1");
                    if (genericIListType is not null)
                    {
                        type.GetMethod("Insert")!.Invoke(itemsSource, [insertionIndex, itemToInsert]);
                    }
                }
            }
        }

        public static int RemoveItemFromItemsControl(ItemsControl itemsControl, object itemToRemove)
        {
            int indexToBeRemoved = -1;
            if (itemToRemove != null)
            {
                indexToBeRemoved = itemsControl.Items.IndexOf(itemToRemove);

                if (indexToBeRemoved != -1)
                {
                    IEnumerable itemsSource = itemsControl.ItemsSource;
                    if (itemsSource == null)
                    {
                        itemsControl.Items.RemoveAt(indexToBeRemoved);
                    }
                    // Is the ItemsSource IList or IList<T>? If so, remove the item from the list.
                    else if (itemsSource is IList list)
                    {
                        list.RemoveAt(indexToBeRemoved);
                    }
                    else
                    {
                        Type type = itemsSource.GetType();
                        Type? genericIListType = type.GetInterface("IList`1");
                        if (genericIListType is not null)
                        {
                            type.GetMethod("RemoveAt")!.Invoke(itemsSource, [indexToBeRemoved]);
                        }
                    }
                }
            }
            return indexToBeRemoved;
        }

        public static bool IsInFirstHalf(FrameworkElement container, Point clickedPoint, bool hasVerticalOrientation)
        {
            if (hasVerticalOrientation)
            {
                return clickedPoint.Y < container.ActualHeight / 2;
            }
            return clickedPoint.X < container.ActualWidth / 2;
        }

        public static bool IsMovementBigEnough(Point initialMousePosition, Point currentPosition)
        {
            return (Math.Abs(currentPosition.X - initialMousePosition.X) >= SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(currentPosition.Y - initialMousePosition.Y) >= SystemParameters.MinimumVerticalDragDistance);
        }

        public static string FormattedDurationFromSeconds(int? totalSeconds = 0)
        {
            // Build up the minimal result...
            string result = "0:00";
            if (totalSeconds is not null)
            {
                TimeSpan length = TimeSpan.FromSeconds((int)totalSeconds);

                double secondsPlusFraction = (double)length.Seconds + ((double)length.Milliseconds / 1000);
                result = string.Format("{0:00}", (int)Math.Round(secondsPlusFraction));
                if ((int)length.Minutes > 0)
                {
                    result = string.Format("{0:0}", (int)length.Minutes) + ":" + result;
                }
                else
                {   // Is this because we are even 0 minutes with more than one hour or because we're shorter than a minute?
                    if ((int)length.TotalMinutes == 0)
                    {   // Always show 0 minutes if the clip is less than 1 minute...
                        result = "0:" + result;
                    }
                    else
                    {   // Even 00 ...
                        result = "00:" + result;
                    }
                }
                if ((int)length.TotalHours > 0)
                {
                    result = string.Format("{0:0}", (int)length.TotalHours) + ":" + result;
                }
            }
            return result;
        }
    }
}
