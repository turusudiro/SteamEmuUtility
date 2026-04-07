using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SteamEmuUtility.Controls
{
    /// <summary>
    /// Interaction logic for IconText.xaml
    /// </summary>
    public partial class IconText : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty LabelStyleProperty =
            DependencyProperty.Register(nameof(LabelStyle), typeof(Style), typeof(IconText),
                new PropertyMetadata(null, OnVisualStateChanged));

        public static readonly DependencyProperty IconStyleProperty =
            DependencyProperty.Register(nameof(IconStyle), typeof(Style), typeof(IconText),
                new PropertyMetadata(null, OnVisualStateChanged));

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(IconText),
                new PropertyMetadata(null, OnVisualStateChanged));

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(IconText),
                new PropertyMetadata(false, OnVisualStateChanged));

        public static readonly DependencyProperty TrueStyleProperty =
            DependencyProperty.Register(nameof(TrueStyle), typeof(Style), typeof(IconText),
                new PropertyMetadata(null, OnVisualStateChanged));

        public static readonly DependencyProperty FalseStyleProperty =
            DependencyProperty.Register(nameof(FalseStyle), typeof(Style), typeof(IconText),
                new PropertyMetadata(null, OnVisualStateChanged));

        public static readonly DependencyProperty TrueForegroundProperty =
            DependencyProperty.Register(nameof(TrueForeground), typeof(Brush), typeof(IconText),
                new PropertyMetadata(null, OnVisualStateChanged));

        public static readonly DependencyProperty FalseForegroundProperty =
            DependencyProperty.Register(nameof(FalseForeground), typeof(Brush), typeof(IconText),
                new PropertyMetadata(null, OnVisualStateChanged));

        public static readonly DependencyProperty TrueLabelProperty =
            DependencyProperty.Register(nameof(TrueLabel), typeof(string), typeof(IconText),
                new PropertyMetadata(null, OnVisualStateChanged));

        public static readonly DependencyProperty FalseLabelProperty =
            DependencyProperty.Register(nameof(FalseLabel), typeof(string), typeof(IconText),
                new PropertyMetadata(null, OnVisualStateChanged));

        #endregion

        #region Properties

        public Style LabelStyle
        {
            get => (Style)GetValue(LabelStyleProperty);
            set => SetValue(LabelStyleProperty, value);
        }

        public Style IconStyle
        {
            get => (Style)GetValue(IconStyleProperty);
            set => SetValue(IconStyleProperty, value);
        }

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public Style TrueStyle
        {
            get => (Style)GetValue(TrueStyleProperty);
            set => SetValue(TrueStyleProperty, value);
        }

        public Style FalseStyle
        {
            get => (Style)GetValue(FalseStyleProperty);
            set => SetValue(FalseStyleProperty, value);
        }

        public Brush TrueForeground
        {
            get => (Brush)GetValue(TrueForegroundProperty);
            set => SetValue(TrueForegroundProperty, value);
        }

        public Brush FalseForeground
        {
            get => (Brush)GetValue(FalseForegroundProperty);
            set => SetValue(FalseForegroundProperty, value);
        }

        public string TrueLabel
        {
            get => (string)GetValue(TrueLabelProperty);
            set => SetValue(TrueLabelProperty, value);
        }

        public string FalseLabel
        {
            get => (string)GetValue(FalseLabelProperty);
            set => SetValue(FalseLabelProperty, value);
        }

        #endregion

        #region Constructor

        public IconText()
        {
            InitializeComponent();
            Loaded += (s, e) => ApplyVisualState();
        }

        #endregion

        #region Private Methods

        private static void OnVisualStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is IconText control && control.IsLoaded)
                control.ApplyVisualState();
        }

        private void ApplyVisualState()
        {
            if (IconTB == null || TextTB == null) return;

            Style resolvedStyle = IsActive ? TrueStyle ?? IconStyle : FalseStyle ?? IconStyle;
            string resolvedLabel = IsActive ? TrueLabel ?? Label : FalseLabel ?? Label;
            Brush resolvedForeground = IsActive ? TrueForeground : FalseForeground;

            IconTB.Style = resolvedStyle;
            TextTB.Text = resolvedLabel;
            TextTB.Style = LabelStyle;

            if (resolvedForeground != null)
                IconTB.Foreground = resolvedForeground;
            else
                IconTB.ClearValue(TextBlock.ForegroundProperty);
        }

        #endregion
    }
}
