using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Skia;
using SkiaSharp;
using System;
using System.Diagnostics;

namespace VideoPresenterSample.Views
{
    public abstract class VideoPresenterBase : Control
    {
        /// <summary>
        /// Defines the <see cref="Stretch"/> property.
        /// </summary>
        public static readonly StyledProperty<Stretch> StretchProperty =
            AvaloniaProperty.Register<VideoPresenterBase, Stretch>(nameof(Stretch), Stretch.Uniform);

        /// <summary>
        /// Defines the <see cref="StretchDirection"/> property.
        /// </summary>
        public static readonly StyledProperty<StretchDirection> StretchDirectionProperty =
            AvaloniaProperty.Register<VideoPresenterBase, StretchDirection>(
                nameof(StretchDirection),
                StretchDirection.Both);

        public static readonly StyledProperty<BitmapInterpolationMode> RenderInterpolationModeProperty =
            AvaloniaProperty.Register<VideoPresenterBase, BitmapInterpolationMode>(
                nameof(RenderInterpolationMode),
                BitmapInterpolationMode.None);

        public static readonly StyledProperty<EdgeMode> RenderEdgedModeProperty =
            AvaloniaProperty.Register<VideoPresenterBase, EdgeMode>(
                nameof(RenderEdgeMode),
                EdgeMode.Aliased);

        public static readonly StyledProperty<int> PicturePixelWidthProperty =
            AvaloniaProperty.Register<VideoPresenterBase, int>(nameof(PicturePixelWidth), 2560);

        public static readonly StyledProperty<int> PicturePixelHeightProperty =
            AvaloniaProperty.Register<VideoPresenterBase, int>(nameof(PicturePixelHeight), 1440);

        public static readonly StyledProperty<double> SpeedFactorProperty =
            AvaloniaProperty.Register<VideoPresenterBase, double>(nameof(SpeedFactor), 1.0);

        static VideoPresenterBase()
        {
            AffectsRender<VideoPresenterBase>(
                StretchProperty,
                StretchDirectionProperty,
                RenderInterpolationModeProperty,
                RenderEdgedModeProperty,
                PicturePixelWidthProperty,
                PicturePixelHeightProperty);

            AffectsMeasure<VideoPresenterBase>(
                StretchProperty,
                StretchDirectionProperty,
                PicturePixelWidthProperty,
                PicturePixelHeightProperty);

            AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<VideoPresenterBase>(
                AutomationControlType.Image);
        }

        /// <summary>
        /// Gets or sets a value controlling how the video will be stretched.
        /// </summary>
        public Stretch Stretch
        {
            get => GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        /// <summary>
        /// Gets or sets a value controlling in what direction the video will be stretched.
        /// </summary>
        public StretchDirection StretchDirection
        {
            get => GetValue(StretchDirectionProperty);
            set => SetValue(StretchDirectionProperty, value);
        }

        public BitmapInterpolationMode RenderInterpolationMode
        {
            get => GetValue(RenderInterpolationModeProperty);
            set => SetValue(RenderInterpolationModeProperty, value);
        }

        public EdgeMode RenderEdgeMode
        {
            get => GetValue(RenderEdgedModeProperty);
            set => SetValue(RenderEdgedModeProperty, value);
        }

        public int PicturePixelWidth
        {
            get => GetValue(PicturePixelWidthProperty);
            set => SetValue(PicturePixelWidthProperty, value);
        }

        public int PicturePixelHeight
        {
            get => GetValue(PicturePixelHeightProperty);
            set => SetValue(PicturePixelHeightProperty, value);
        }

        public double SpeedFactor
        {
            get => GetValue<double>(SpeedFactorProperty);
            set => SetValue(SpeedFactorProperty, value);
        }

        protected static Vector PictureDpi { get; } = new(96, 96);

        protected PixelSize PicturePixelSize { get; private set; }

        protected PixelFormat PicturePixelFormat { get; } = PixelFormats.Bgra8888;

        protected AlphaFormat PictureAlphaFormat { get; } = AlphaFormat.Unpremul;

        protected Rect ContextBoundsRect { get; private set; }

        protected Rect ContextSourceRect { get; private set; }

        protected Rect ContextTargetRect { get; private set; }

        /// <inheritdoc />
        protected override bool BypassFlowDirectionPolicies => true;

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size.</param>
        /// <returns>The desired size of the control.</returns>
        protected override Size MeasureOverride(Size availableSize) => PicturePixelWidth > 0 && PicturePixelHeight > 0
            ? Stretch.CalculateSize(availableSize, PicturePixelSize.ToSizeWithDpi(PictureDpi), StretchDirection)
            : default;

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize) => PicturePixelWidth > 0 && PicturePixelHeight > 0
            ? Stretch.CalculateSize(finalSize, PicturePixelSize.ToSizeWithDpi(PictureDpi))
            : default;

        /// <inheritdoc/>
        protected override void OnInitialized()
        {
            ClipToBounds = true;
            RenderOptions.SetBitmapInterpolationMode(this, RenderInterpolationMode);
            RenderOptions.SetEdgeMode(this, RenderEdgeMode);
            PicturePixelSize = new(PicturePixelWidth, PicturePixelHeight);
            animationSpeedFactor = SpeedFactor;
            base.OnInitialized();
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            try
            {
                if (change.Property is null)
                    return;

                if (change.Property == RenderInterpolationModeProperty && change.NewValue is BitmapInterpolationMode interpolationMode)
                    RenderOptions.SetBitmapInterpolationMode(this, interpolationMode);

                if (change.Property == RenderEdgedModeProperty && change.NewValue is EdgeMode edgeMode)
                    RenderOptions.SetEdgeMode(this, edgeMode);

                if (change.Property == PicturePixelWidthProperty || change.Property == PicturePixelHeightProperty)
                    PicturePixelSize = new(PicturePixelWidth, PicturePixelHeight);

                if (change.Property == SpeedFactorProperty)
                    animationSpeedFactor = SpeedFactor;
            }
            finally
            {
                base.OnPropertyChanged(change);
            }
        }

        protected void UpdateContextRects()
        {
            ContextBoundsRect = new(0, 0, Bounds.Width, Bounds.Height);
            var boundsSize = Bounds.Size;
            var viewPort = new Rect(boundsSize);
            var dpiSize = PicturePixelSize.ToSizeWithDpi(PictureDpi);
            var scale = Stretch.CalculateScaling(boundsSize, dpiSize, StretchDirection);
            var scaledSize = dpiSize * scale;

            ContextTargetRect = viewPort
                .CenterRect(new(scaledSize))
                .Intersect(viewPort);

            ContextSourceRect = new Rect(dpiSize)
                .CenterRect(new(ContextTargetRect.Size / scale));
        }

        private Color Color = GetRandomColor();
        private long lastTicks;
        private double animationSpeedFactor;

        private static Color GetRandomColor()
        {
            return new Color(255,
                (byte)Random.Shared.Next(0, 256),
                (byte)Random.Shared.Next(0, 256),
                (byte)Random.Shared.Next(0, 256));
        }

        protected unsafe void WriteBitmapBuffer(nint address, int width, int height, int bytesPerRow)
        {
            if (lastTicks <= 0)
                lastTicks = Stopwatch.GetTimestamp();

            var elapsed = Stopwatch.GetElapsedTime(lastTicks).TotalMilliseconds;
            var bitmapPixelCount = height * width;
            var paintPixelCount = Convert.ToInt32(elapsed * width * animationSpeedFactor);

            var span = new Span<SKColor>(address.ToPointer(), Math.Min(paintPixelCount, bitmapPixelCount));
            span.Fill(Color.ToSKColor());

            if (paintPixelCount >= bitmapPixelCount)
            {
                Color = GetRandomColor();
                lastTicks = Stopwatch.GetTimestamp();
                Debug.WriteLine($"Cycle Elapsed: {elapsed:n2} ms.");
            }
        }

    }
}
