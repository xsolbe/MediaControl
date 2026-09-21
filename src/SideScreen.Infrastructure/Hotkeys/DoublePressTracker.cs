namespace SideScreen.Infrastructure.Hotkeys;

/// <summary>
/// Lógica pura do double-press (testável, sem timers).
/// Uso: a cada pressionamento de Next, chame Press(now).
/// - First: primeiro toque — o chamador deve aguardar a janela antes de disparar Next.
/// - Second: segundo toque dentro da janela — dispare Previous, cancele o Next pendente.
/// - Expired: passou da janela sem segundo toque — dispare o Next pendente.
/// Default desligado (introduz delay — ver docs/hotkeys.md).
/// </summary>
public sealed class DoublePressTracker
{
    private DateTime? _first;
    private readonly int _windowMs;

    public DoublePressTracker(int windowMs) => _windowMs = Math.Clamp(windowMs, 100, 1000);

    public enum Result { First, Second, SingleExpired, Ignored }

    public Result Press(DateTime now)
    {
        if (_first is null)
        {
            _first = now;
            return Result.First;
        }

        var elapsed = (now - _first.Value).TotalMilliseconds;
        _first = null;
        return elapsed <= _windowMs ? Result.Second : Result.First; // se expirou, trata como novo First
    }

    public Result Expire(DateTime now)
    {
        if (_first is null) return Result.Ignored;
        if ((now - _first.Value).TotalMilliseconds >= _windowMs)
        {
            _first = null;
            return Result.SingleExpired;
        }
        return Result.Ignored;
    }

    public void Reset() => _first = null;
}
