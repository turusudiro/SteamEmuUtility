using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SteamEmuUtility.Controls
{
    /// <summary>
    /// Interaction logic for PropertyRow.xaml
    /// </summary>
    public enum PropertyValueType { String, Integer, ULong, Double }

    public partial class PropertyRow : UserControl
    {
        public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(PropertyRow));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(object), typeof(PropertyRow), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty ValueTypeProperty =
            DependencyProperty.Register(nameof(ValueType), typeof(PropertyValueType), typeof(PropertyRow),
                new PropertyMetadata(PropertyValueType.String));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public PropertyValueType ValueType
        {
            get => (PropertyValueType)GetValue(ValueTypeProperty);
            set => SetValue(ValueTypeProperty, value);
        }

        public PropertyRow()
        {
            InitializeComponent();

            IntegerBox.PreviewTextInput += (s, e) => e.Handled = !e.Text.All(char.IsDigit);
            IntegerBox.PreviewKeyDown += (s, e) => e.Handled = e.Key == Key.Space;
            DataObject.AddPastingHandler(IntegerBox, OnIntegerPaste);

            ULongBox.PreviewTextInput += (s, e) => e.Handled = !e.Text.All(char.IsDigit);
            ULongBox.PreviewKeyDown += (s, e) => e.Handled = e.Key == Key.Space;
            DataObject.AddPastingHandler(ULongBox, OnULongPaste);

            DoubleBox.PreviewTextInput += OnDoublePreviewTextInput;
            DoubleBox.PreviewKeyDown += (s, e) => e.Handled = e.Key == Key.Space;
            DataObject.AddPastingHandler(DoubleBox, OnDoublePaste);
        }

        // Integer paste
        private void OnIntegerPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!text.All(char.IsDigit))
                    e.CancelCommand();
            }
            else e.CancelCommand();
        }

        private void OnULongPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!ulong.TryParse(text, out _))
                    e.CancelCommand();
            }
            else e.CancelCommand();
        }

        // Double input — allow digits, one dot, one leading minus
        private void OnDoublePreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var box = (TextBox)sender;
            string current = box.Text;

            foreach (char c in e.Text)
            {
                if (char.IsDigit(c)) continue;
                if (c == '.' && !current.Contains('.')) continue;
                if (c == '-' && current.Length == 0 && box.CaretIndex == 0) continue;
                e.Handled = true;
                return;
            }
        }

        // Double paste
        private void OnDoublePaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!double.TryParse(text, out _))
                    e.CancelCommand();
            }
            else e.CancelCommand();
        }
    }
}
