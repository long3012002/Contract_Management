using System;
using demo1.Validator;
using Xunit;

namespace demo1.Tests.UnitTests.Validators;

public class CodePrefixValidatorTests
{
    [Theory]
    [InlineData("DAN-2026-001")]
    [InlineData("CUSTOM_CODE_123")]
    public void ValidateDuAnCode_ValidNonEmptyCodes_DoesNotThrow(string inputCode)
    {
        var exception = Record.Exception(() => CodePrefixValidator.ValidateDuAnCode(inputCode));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ValidateDuAnCode_EmptyOrNullCode_ThrowsArgumentException(string? inputCode)
    {
        Assert.Throws<ArgumentException>(() => CodePrefixValidator.ValidateDuAnCode(inputCode));
    }

    [Theory]
    [InlineData("GT-2026-001")]
    [InlineData("CUSTOM_GT")]
    public void ValidateGoiThauCode_ValidNonEmptyCodes_DoesNotThrow(string inputCode)
    {
        var exception = Record.Exception(() => CodePrefixValidator.ValidateGoiThauCode(inputCode));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ValidateGoiThauCode_EmptyOrNullCode_ThrowsArgumentException(string? inputCode)
    {
        Assert.Throws<ArgumentException>(() => CodePrefixValidator.ValidateGoiThauCode(inputCode));
    }

    [Theory]
    [InlineData("HD-2026-001")]
    [InlineData("CUSTOM_HD")]
    public void ValidateHopDongCode_ValidNonEmptyCodes_DoesNotThrow(string inputCode)
    {
        var exception = Record.Exception(() => CodePrefixValidator.ValidateHopDongCode(inputCode));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ValidateHopDongCode_EmptyOrNullCode_ThrowsArgumentException(string? inputCode)
    {
        Assert.Throws<ArgumentException>(() => CodePrefixValidator.ValidateHopDongCode(inputCode));
    }
}
