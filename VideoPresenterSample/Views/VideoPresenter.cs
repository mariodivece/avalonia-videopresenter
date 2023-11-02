using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using System;

namespace VideoPresenterSample.Views;

public class VideoPresenter : VideoPresenterBase
{
    private WriteableBitmap? BufferBitmap;

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        BufferBitmap?.Dispose();
        BufferBitmap = null;
    }

    /// <summary>
    /// Renders the control.
    /// </summary>
    /// <param name="context">The drawing context.</param>
    public override void Render(DrawingContext context)
    {
        try
        {
            if (Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            UpdateContextRects();
            using var lockedBitmap = AcquireBitmapBuffer();
            WriteBitmapBuffer(lockedBitmap.Address, lockedBitmap.Size.Width, lockedBitmap.Size.Height, lockedBitmap.RowBytes);
            context.DrawImage(BufferBitmap!, ContextSourceRect, ContextTargetRect);
        }
        finally
        {
            // always force an update
            Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
        }
    }

    public unsafe ILockedFramebuffer AcquireBitmapBuffer()
    {
        if (BufferBitmap is not null &&
            BufferBitmap.PixelSize == PicturePixelSize)
            return BufferBitmap.Lock();

        BufferBitmap?.Dispose();
        BufferBitmap = new WriteableBitmap(
            PicturePixelSize, PictureDpi, PicturePixelFormat, AlphaFormat.Unpremul);

        var lockedBuffer = BufferBitmap.Lock();
        var s = new Span<byte>(lockedBuffer.Address.ToPointer(),
            lockedBuffer.RowBytes * lockedBuffer.Size.Height);

        s.Clear();
        return lockedBuffer;
    }
}
