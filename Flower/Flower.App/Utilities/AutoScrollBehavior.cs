using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Flower.App.Utilities
{
    public static class AutoScrollBehavior
    {
        public static readonly AttachedProperty<bool> AutoScrollToEndProperty =
            AvaloniaProperty.RegisterAttached<TextBox, bool>(
                "AutoScrollToEnd",
                typeof(AutoScrollBehavior),     // <-- required ownerType
                defaultValue: false);

        private static readonly AttachedProperty<IDisposable?> SubscriptionProperty =
            AvaloniaProperty.RegisterAttached<TextBox, IDisposable?>(
                "AutoScroll_Subscription",
                typeof(AutoScrollBehavior),     // <-- required ownerType
                defaultValue: null);

        static AutoScrollBehavior()
        {
            AutoScrollToEndProperty.Changed.AddClassHandler<TextBox>((tb, e) =>
            {
                var enabled = e.GetNewValue<bool>();
                if (enabled) Attach(tb);
                else Detach(tb);
            });
        }

        public static void SetAutoScrollToEnd(TextBox element, bool value) =>
            element.SetValue(AutoScrollToEndProperty, value);

        public static bool GetAutoScrollToEnd(TextBox element) =>
            element.GetValue(AutoScrollToEndProperty);

        private static void Attach(TextBox tb)
        {
            if (tb.GetValue(SubscriptionProperty) is not null) return;

            var sub = tb.GetObservable(TextBox.TextProperty).Subscribe(_ => MoveCaretAndScroll(tb));
            tb.SetValue(SubscriptionProperty, sub);

            tb.AttachedToVisualTree += OnAttached;
            tb.DetachedFromVisualTree += OnDetached;

            MoveCaretAndScroll(tb);
        }

        private static void Detach(TextBox tb)
        {
            tb.GetValue(SubscriptionProperty)?.Dispose();
            tb.SetValue(SubscriptionProperty, null);

            tb.AttachedToVisualTree -= OnAttached;
            tb.DetachedFromVisualTree -= OnDetached;
        }

        private static void OnAttached(object? s, VisualTreeAttachmentEventArgs e)
        {
            if (s is TextBox tb) MoveCaretAndScroll(tb);
        }

        private static void OnDetached(object? s, VisualTreeAttachmentEventArgs e)
        {
            if (s is TextBox tb) Detach(tb);
        }

        private static void MoveCaretAndScroll(TextBox tb)
        {
            var len = tb.Text?.Length ?? 0;
            tb.CaretIndex = len;
            tb.SelectionStart = len;
            tb.SelectionEnd = len;

            Dispatcher.UIThread.Post(() =>
            {
                var sv = tb.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
                if (sv is null) return;

                var newY = Math.Max(0, sv.Extent.Height - sv.Viewport.Height);
                sv.Offset = new Avalonia.Vector(sv.Offset.X, newY);
            }, DispatcherPriority.Background);
        }
    }
}
