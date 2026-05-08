using WinUICalc.Core;

namespace WinUICalc.Tests;

public class CalculatorEngineTests
{
    private static CalculatorEngine NewEngine() => new();

    private static void Type(CalculatorEngine e, string s)
    {
        foreach (var c in s)
        {
            if (c == '.')
            {
                e.InputDecimal();
            }
            else if (c >= '0' && c <= '9')
            {
                e.InputDigit(c);
            }
            else
            {
                throw new ArgumentException($"Unexpected char '{c}' in test input '{s}'.");
            }
        }
    }

    [Fact]
    public void Initial_Display_Is_Zero()
    {
        var e = NewEngine();
        Assert.Equal("0", e.Display);
        Assert.False(e.IsError);
    }

    [Fact]
    public void InputDigit_Single_Replaces_Leading_Zero()
    {
        var e = NewEngine();
        e.InputDigit('5');
        Assert.Equal("5", e.Display);
    }

    [Fact]
    public void InputDigit_Multiple_Appends()
    {
        var e = NewEngine();
        Type(e, "123");
        Assert.Equal("123", e.Display);
    }

    [Fact]
    public void Addition_Produces_Correct_Result()
    {
        var e = NewEngine();
        Type(e, "2");
        e.SetOperator(Operator.Add);
        Type(e, "3");
        e.Equals();
        Assert.Equal("5", e.Display);
        Assert.False(e.IsError);
    }

    [Fact]
    public void Subtraction_Produces_Correct_Result()
    {
        var e = NewEngine();
        Type(e, "9");
        e.SetOperator(Operator.Subtract);
        Type(e, "4");
        e.Equals();
        Assert.Equal("5", e.Display);
    }

    [Fact]
    public void Multiplication_Produces_Correct_Result()
    {
        var e = NewEngine();
        Type(e, "6");
        e.SetOperator(Operator.Multiply);
        Type(e, "7");
        e.Equals();
        Assert.Equal("42", e.Display);
    }

    [Fact]
    public void Division_Produces_Correct_Result()
    {
        var e = NewEngine();
        Type(e, "20");
        e.SetOperator(Operator.Divide);
        Type(e, "4");
        e.Equals();
        Assert.Equal("5", e.Display);
    }

    [Fact]
    public void Decimal_Inputs_Are_Exact()
    {
        var e = NewEngine();
        Type(e, "1.5");
        e.SetOperator(Operator.Add);
        Type(e, "2.25");
        e.Equals();
        Assert.Equal("3.75", e.Display);
    }

    [Fact]
    public void Negative_Result_Displays_Correctly()
    {
        var e = NewEngine();
        Type(e, "2");
        e.SetOperator(Operator.Subtract);
        Type(e, "5");
        e.Equals();
        Assert.Equal("-3", e.Display);
    }

    [Fact]
    public void Divide_By_Zero_Sets_Error_State_And_Does_Not_Throw()
    {
        var e = NewEngine();
        Type(e, "10");
        e.SetOperator(Operator.Divide);
        Type(e, "0");

        // Must not throw.
        var ex = Record.Exception(() => e.Equals());
        Assert.Null(ex);

        Assert.True(e.IsError);
        Assert.Equal("Error", e.Display);
    }

    [Fact]
    public void Input_Is_Ignored_While_In_Error_State()
    {
        var e = NewEngine();
        Type(e, "1");
        e.SetOperator(Operator.Divide);
        Type(e, "0");
        e.Equals();
        Assert.True(e.IsError);

        e.InputDigit('5');
        e.InputDecimal();
        e.SetOperator(Operator.Add);
        e.Equals();
        e.Backspace();
        Assert.Equal("Error", e.Display);
        Assert.True(e.IsError);
    }

    [Fact]
    public void Clear_Resets_State_After_Error()
    {
        var e = NewEngine();
        Type(e, "10");
        e.SetOperator(Operator.Divide);
        Type(e, "0");
        e.Equals();
        Assert.True(e.IsError);

        e.Clear();
        Assert.False(e.IsError);
        Assert.Equal("0", e.Display);

        // Should be usable again.
        Type(e, "7");
        e.SetOperator(Operator.Add);
        Type(e, "3");
        e.Equals();
        Assert.Equal("10", e.Display);
    }

    [Fact]
    public void Clear_Resets_State_From_Mid_Calculation()
    {
        var e = NewEngine();
        Type(e, "42");
        e.SetOperator(Operator.Multiply);
        Type(e, "7");
        e.Clear();
        Assert.Equal("0", e.Display);
        Assert.False(e.IsError);

        // Pressing = right after Clear should be a no-op.
        e.Equals();
        Assert.Equal("0", e.Display);
    }

    [Fact]
    public void Chaining_After_Equals_Uses_Result_As_Left_Operand()
    {
        var e = NewEngine();
        Type(e, "2");
        e.SetOperator(Operator.Add);
        Type(e, "3");
        e.Equals();
        Assert.Equal("5", e.Display);

        e.SetOperator(Operator.Multiply);
        Type(e, "4");
        e.Equals();
        Assert.Equal("20", e.Display);
    }

    [Fact]
    public void Chaining_Without_Equals_Computes_Pending_Op()
    {
        // 2 + 3 + 4 = 9 (the middle "+" forces the 2+3 calculation)
        var e = NewEngine();
        Type(e, "2");
        e.SetOperator(Operator.Add);
        Type(e, "3");
        e.SetOperator(Operator.Add);
        Assert.Equal("5", e.Display); // intermediate result shown
        Type(e, "4");
        e.Equals();
        Assert.Equal("9", e.Display);
    }

    [Fact]
    public void Backspace_Removes_Last_Character()
    {
        var e = NewEngine();
        Type(e, "123");
        e.Backspace();
        Assert.Equal("12", e.Display);
        e.Backspace();
        Assert.Equal("1", e.Display);
        e.Backspace();
        Assert.Equal("0", e.Display);
    }

    [Fact]
    public void Backspace_On_Zero_Stays_Zero()
    {
        var e = NewEngine();
        e.Backspace();
        Assert.Equal("0", e.Display);
    }

    [Fact]
    public void Backspace_Handles_Decimal_Point()
    {
        var e = NewEngine();
        Type(e, "1.5");
        e.Backspace();
        Assert.Equal("1.", e.Display);
        e.Backspace();
        Assert.Equal("1", e.Display);
    }

    [Fact]
    public void Decimal_Point_Is_Inserted_Once_Only()
    {
        var e = NewEngine();
        Type(e, "1");
        e.InputDecimal();
        e.InputDecimal();
        e.InputDecimal();
        Type(e, "5");
        Assert.Equal("1.5", e.Display);
    }

    [Fact]
    public void Decimal_With_No_Digits_Yet_Shows_Zero_Dot()
    {
        var e = NewEngine();
        e.InputDecimal();
        Assert.Equal("0.", e.Display);
        e.InputDigit('5');
        Assert.Equal("0.5", e.Display);
    }

    [Fact]
    public void Equals_With_Trailing_Decimal_Point_Treats_As_Integer()
    {
        var e = NewEngine();
        Type(e, "5");
        e.InputDecimal();
        e.SetOperator(Operator.Add);
        Type(e, "1");
        e.Equals();
        Assert.Equal("6", e.Display);
    }

    [Fact]
    public void Operator_Without_Equals_Then_Operator_Updates_Operator()
    {
        var e = NewEngine();
        Type(e, "5");
        e.SetOperator(Operator.Add);
        e.SetOperator(Operator.Multiply); // change mind
        Type(e, "3");
        e.Equals();
        Assert.Equal("15", e.Display);
    }

    [Fact]
    public void Chained_Decimal_Math_Is_Exact()
    {
        // The key motivator for using decimal over double.
        var e = NewEngine();
        Type(e, "0.1");
        e.SetOperator(Operator.Add);
        Type(e, "0.2");
        e.Equals();
        Assert.Equal("0.3", e.Display);
    }

    [Fact]
    public void Equals_Without_Pending_Operator_Is_NoOp()
    {
        var e = NewEngine();
        Type(e, "42");
        e.Equals();
        Assert.Equal("42", e.Display);
        Assert.False(e.IsError);
    }

    [Fact]
    public void Pressing_Digit_After_Equals_Starts_Fresh_Number()
    {
        var e = NewEngine();
        Type(e, "2");
        e.SetOperator(Operator.Add);
        Type(e, "3");
        e.Equals();
        Assert.Equal("5", e.Display);

        e.InputDigit('7');
        Assert.Equal("7", e.Display);
    }

    [Fact]
    public void Pressing_Digit_After_Operator_Starts_Fresh_Number()
    {
        var e = NewEngine();
        Type(e, "12");
        e.SetOperator(Operator.Add);
        e.InputDigit('3');
        Assert.Equal("3", e.Display);
    }

    [Theory]
    [InlineData('a')]
    [InlineData(':')]
    [InlineData('/')]
    public void InputDigit_Throws_For_Non_Digit(char c)
    {
        var e = NewEngine();
        Assert.Throws<ArgumentOutOfRangeException>(() => e.InputDigit(c));
    }
}
