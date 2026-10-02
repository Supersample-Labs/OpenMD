using System.Runtime.InteropServices;

namespace OpenMD;

// Rich Edit's Text Object Model suspends recording without clearing prior edits.
// Definitions: microsoft/win32metadata, RecompiledIdlHeaders/um/TOM.h.
internal sealed class UndoSuspension : IDisposable
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, out IntPtr pointer);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int UndoMethod(IntPtr document, int count, IntPtr actualCount);

    private readonly IntPtr document;
    private readonly UndoMethod undo;
    private UndoSuspension(IntPtr document, UndoMethod undo) { this.document = document; this.undo = undo; }

    internal static UndoSuspension? Begin(RichTextBox editor)
    {
        if (!editor.IsHandleCreated) return null;
        _ = SendMessage(editor.Handle, 0x043C /* EM_GETOLEINTERFACE */, IntPtr.Zero, out var ole);
        if (ole == IntPtr.Zero) return null;
        IntPtr document = IntPtr.Zero;
        try
        {
            var id = new Guid("8CC497C0-A1DF-11CE-8098-00AA0047BE5D");
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(ole, in id, out document));
            // IDispatch's seven slots precede fifteen ITextDocument methods.
            var vtable = Marshal.ReadIntPtr(document);
            var undo = Marshal.GetDelegateForFunctionPointer<UndoMethod>(Marshal.ReadIntPtr(vtable, 22 * IntPtr.Size));
            Marshal.ThrowExceptionForHR(undo(document, -9999995 /* tomSuspend */, IntPtr.Zero));
            return new UndoSuspension(document, undo);
        }
        catch
        {
            if (document != IntPtr.Zero) Marshal.Release(document);
            throw;
        }
        finally { Marshal.Release(ole); }
    }

    public void Dispose()
    {
        try { Marshal.ThrowExceptionForHR(undo(document, -9999994 /* tomResume */, IntPtr.Zero)); }
        finally { Marshal.Release(document); }
    }
}


