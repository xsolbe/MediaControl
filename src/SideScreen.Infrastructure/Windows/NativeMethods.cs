using System.Runtime.InteropServices;

namespace SideScreen.Infrastructure.Windows;

/// <summary>
/// P/Invoke mínimo para Fase 3. Só o que o PotPlayer precisa — nada de hooks aqui.
/// </summary>
internal static partial class NativeMethods
{
    [LibraryImport("user32.dll", EntryPoint = "FindWindowW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint FindWindow(string? lpClassName, string? lpWindowName);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWindow(nint hWnd);

    [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
    public static partial nint SendMessage(nint hWnd, uint msg, nint wParam, nint lParam);
}
