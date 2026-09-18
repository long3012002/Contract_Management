using System;
using demo1.Validator;
using Xunit;

namespace demo1.Tests.UnitTests.Validators;

public class CodePrefixValidatorTests
{
    [Theory]
    [InlineData("001/2026/DAN", 1)]
    [InlineData("002/2026/DATK", 2)]
    [InlineData("CUSTOM_CODE_123", 1)]
    public void ValidateDuAnCode_ValidNonEmptyCodes_DoesNotThrow(string inputCode, int loaiDuAn)
    {
        var exception = Record.Exception(() => CodePrefixValidator.ValidateDuAnCode(inputCode, loaiDuAn));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ValidateDuAnCode_EmptyOrNullCode_ThrowsArgumentException(string? inputCode)
    {
        Assert.Throws<ArgumentException>(() => CodePrefixValidator.ValidateDuAnCode(inputCode, 1));
    }

    [Theory]
    [InlineData("002/2026/GT")]
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
    [InlineData("017/2026/HĐ")]
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
