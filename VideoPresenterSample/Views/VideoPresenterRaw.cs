using Avalonia;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using SkiaSharp;
using System;
using System.Threading.Tasks;

namespace VideoPresenterSample.Views;

internal class VideoPresenterRaw : VideoPresenterBase, ICustomDrawOperation
{
    private WriteableBitmap? NormalSource;
    private SKBitmap? SkiaSource;

    /// <inheritdoc/>
    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);

        NormalSource?.Dispose();
        SkiaSource?.Dispose();
        NormalSource = null;
        SkiaSource = null;
    }

    public override void Render(DrawingContext context)
    {
        try
        {
            if (Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            UpdateContextRects();
            context.Custom(this);

        }
        finally
        {
            Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Render);
        }
    }

    /// <inheritdoc/>
    unsafe void ICustomDrawOperation.Render(ImmediateDrawingContext context)
    {
        if (context.TryGetFeature<ISkiaSharpApiLeaseFeature>() is ISkiaSharpApiLeaseFeature leaseFeature)
        {
            using var lease = leaseFeature.Lease();

            if (SkiaSource is null || SkiaSource.Width != PicturePixelSize.Width || SkiaSource.Height != PicturePixelSize.Height)
            {
                SkiaSource?.Dispose();
                SkiaSource = new(PicturePixelSize.Width, PicturePixelSize.Height,
                    PicturePixelFormat.ToSkColorType(), PictureAlphaFormat.ToSkAlphaType());
            }

            WriteBitmapBuffer(SkiaSource.GetPixels(), PicturePixelSize.Width, PicturePixelSize.Height, SkiaSource.RowBytes);
            lease.SkCanvas.DrawBitmap(SkiaSource, ContextSourceRect.ToSKRect(), ContextTargetRect.ToSKRect());
        }
        else
        {
            if (NormalSource is null || NormalSource.PixelSize != PicturePixelSize)
            {
                NormalSource?.Dispose();
                NormalSource = new(PicturePixelSize, PictureDpi, PicturePixelFormat, PictureAlphaFormat);
            }

            using (var source = NormalSource.Lock())
                WriteBitmapBuffer(source.Address, source.Size.Width, source.Size.Height, source.RowBytes);

            context.DrawBitmap(NormalSource, ContextSourceRect, ContextTargetRect);
        }
    }

    Rect ICustomDrawOperation.Bounds => ContextBoundsRect;

    bool ICustomDrawOperation.HitTest(Point p) => false;

    bool IEquatable<ICustomDrawOperation>.Equals(ICustomDrawOperation? other) => false;

    void IDisposable.Dispose() { }

}
