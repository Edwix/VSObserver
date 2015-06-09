using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Windows.Controls.Primitives;


namespace VSObserver.Behavior
{
    class DropDownImageBehavior : Behavior<Image>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.AddHandler(Image.MouseLeftButtonDownEvent, new RoutedEventHandler(AssociatedObject_Click), true);
        }

        void AssociatedObject_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Image source = sender as Image;
            if (source != null && source.ContextMenu != null)
            {
                // If there is a drop-down assigned to this button, then position and display it 
                source.ContextMenu.PlacementTarget = source;
                source.ContextMenu.Placement = PlacementMode.Bottom;
                source.ContextMenu.IsOpen = true;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.RemoveHandler(Image.MouseLeftButtonDownEvent, new RoutedEventHandler(AssociatedObject_Click));
        }
    }
}
