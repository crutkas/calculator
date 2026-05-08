using System.Globalization;

namespace WinUICalc.Core;

/// <summary>
/// Pure infix calculator state machine. Independent of any UI framework so it
/// can be unit-tested with a vanilla <c>dotnet test</c> on a non-Windows TFM.
/// </summary>
/// <remarks>
/// State machine summary:
/// <list type="bullet">
///   <item>
///     <description>
///     Display always reflects either the user's in-progress input, the
///     result of the last evaluation, or the literal "Error" sentinel after a
///     failed operation.
///     </description>
///   </item>
///   <item>
///     <description>
///     A "fresh" flag indicates the next digit press should overwrite the
///     display rather than append to it. Set after an operator press or after
///     <c>=</c> so chaining works naturally.
///     </description>
///   </item>
///   <item>
///     <description>
///     <c>decimal</c> is used (not <c>double</c>) because base-2 floating
///     point would surprise users with results like 0.1 + 0.2 = 0.30000…04.
///     </description>
///   </item>
/// </list>
/// </remarks>
public sealed class CalculatorEngine
{
    private const string ErrorText = "Error";
    private const string Zero = "0";

    private string _display = Zero;
    private decimal? _stored;
    private Operator? _pendingOp;
    private bool _isInputFresh;
    private bool _isError;

    /// <summary>
    /// The current text shown to the user. Always non-null. Either a number
    /// in invariant culture, "0", or "Error".
    /// </summary>
    public string Display => _display;

    /// <summary>
    /// True after a failed operation (e.g. divide-by-zero or overflow).
    /// While true, all input methods except <see cref="Clear"/> are no-ops.
    /// </summary>
    public bool IsError => _isError;

    /// <summary>Append a digit to the current input.</summary>
    /// <param name="digit">A char in '0'..'9'.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if not a digit.</exception>
    public void InputDigit(char digit)
    {
        if (digit < '0' || digit > '9')
        {
            throw new ArgumentOutOfRangeException(nameof(digit), digit, "Digit must be 0-9.");
        }

        if (_isError)
        {
            return;
        }

        if (_isInputFresh)
        {
            _display = digit.ToString();
            _isInputFresh = false;
            return;
        }

        if (_display == Zero)
        {
            _display = digit.ToString();
            return;
        }

        _display += digit;
    }

    /// <summary>Insert a decimal point (no-op if one is already present).</summary>
    public void InputDecimal()
    {
        if (_isError)
        {
            return;
        }

        if (_isInputFresh)
        {
            _display = "0.";
            _isInputFresh = false;
            return;
        }

        if (_display.Contains('.'))
        {
            return;
        }

        _display += ".";
    }

    /// <summary>
    /// Set or change the pending binary operator. If a previous operator was
    /// already pending and the user has typed a new right operand, the
    /// previous operation is computed first and its result becomes the new
    /// left operand (this gives 2 + 3 + 4 = 9 the natural way).
    /// </summary>
    public void SetOperator(Operator op)
    {
        if (_isError)
        {
            return;
        }

        var current = ParseDisplay();

        if (_stored.HasValue && _pendingOp.HasValue && !_isInputFresh)
        {
            var (ok, result) = TryCompute(_stored.Value, current, _pendingOp.Value);
            if (!ok)
            {
                EnterError();
                return;
            }
            _stored = result;
            _display = FormatResult(result);
        }
        else
        {
            _stored = current;
        }

        _pendingOp = op;
        _isInputFresh = true;
    }

    /// <summary>
    /// Evaluate the pending operation (if any). Resets the pending operator
    /// but keeps the result as the left operand so that the next operator
    /// press chains naturally.
    /// </summary>
    public void Equals()
    {
        if (_isError)
        {
            return;
        }

        if (!_stored.HasValue || !_pendingOp.HasValue)
        {
            // Nothing to evaluate; leave display alone.
            return;
        }

        var current = ParseDisplay();
        var (ok, result) = TryCompute(_stored.Value, current, _pendingOp.Value);
        if (!ok)
        {
            EnterError();
            return;
        }

        _stored = result;
        _pendingOp = null;
        _display = FormatResult(result);
        _isInputFresh = true;
    }

    /// <summary>Reset everything to the initial state.</summary>
    public void Clear()
    {
        _display = Zero;
        _stored = null;
        _pendingOp = null;
        _isInputFresh = false;
        _isError = false;
    }

    /// <summary>
    /// Remove the last character of the current input. No-op while showing
    /// a result (immediately after <c>=</c> or an operator press) or while
    /// in the error state.
    /// </summary>
    public void Backspace()
    {
        if (_isError)
        {
            return;
        }

        if (_isInputFresh)
        {
            // Don't let the user mutate a "result" character-by-character;
            // they should press C if they want to start over.
            return;
        }

        if (_display.Length <= 1)
        {
            _display = Zero;
            return;
        }

        var trimmed = _display[..^1];
        if (trimmed == "-" || trimmed.Length == 0)
        {
            _display = Zero;
        }
        else
        {
            _display = trimmed;
        }
    }

    private decimal ParseDisplay()
    {
        // The display is always something we produced (never user free text)
        // so a parse failure here is genuinely a bug — let it throw so the
        // caller (Equals/SetOperator) routes it through the error path.
        if (_display.EndsWith('.'))
        {
            // Trailing decimal point is valid input UX-wise; treat as integer.
            return decimal.Parse(_display.TrimEnd('.'), CultureInfo.InvariantCulture);
        }
        return decimal.Parse(_display, CultureInfo.InvariantCulture);
    }

    private static (bool ok, decimal result) TryCompute(decimal left, decimal right, Operator op)
    {
        try
        {
            return op switch
            {
                Operator.Add => (true, left + right),
                Operator.Subtract => (true, left - right),
                Operator.Multiply => (true, left * right),
                Operator.Divide => right == 0m ? (false, 0m) : (true, left / right),
                _ => (false, 0m),
            };
        }
        catch (OverflowException)
        {
            return (false, 0m);
        }
        catch (DivideByZeroException)
        {
            return (false, 0m);
        }
    }

    private static string FormatResult(decimal value)
    {
        // Strip trailing zeros from the scale without going through double
        // (which would lose precision). e.g. 5.000m -> "5", 1.250m -> "1.25".
        var s = value.ToString(CultureInfo.InvariantCulture);
        if (s.Contains('.'))
        {
            s = s.TrimEnd('0').TrimEnd('.');
            if (s.Length == 0 || s == "-")
            {
                s = Zero;
            }
        }
        return s;
    }

    private void EnterError()
    {
        _isError = true;
        _display = ErrorText;
        _stored = null;
        _pendingOp = null;
        _isInputFresh = true;
    }
}
