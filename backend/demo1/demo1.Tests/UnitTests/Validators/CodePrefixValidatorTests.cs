using System;
using demo1.Validator;
using Xunit;

namespace demo1.Tests.UnitTests.Validators;

public class CodePrefixValidatorTests
{
    [Theory]
    [InlineData("PRJ_SRC-001", 1)]
    [InlineData("prj_src-abc", 1)]
    [InlineData("PRJ_SUB-002", 2)]
    [InlineData("prj_sub-xyz", 2)]
    public void ValidateDuAnCode_ValidCodes_DoesNotThrow(string inputCode, int loaiDuAn)
    {
        var exception = Record.Exception(() => CodePrefixValidator.ValidateDuAnCode(inputCode, loaiDuAn));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("001", 1)]
    [InlineData("DA-001", 1)]
    [InlineData("PRJ_SUB-123", 1)]
    [InlineData("002", 2)]
    [InlineData("DA-002", 2)]
    [InlineData("PRJ_SRC-123", 2)]
    public void ValidateDuAnCode_InvalidCodes_ThrowsArgumentException(string inputCode, int loaiDuAn)
    {
        Assert.Throws<ArgumentException>(() => CodePrefixValidator.ValidateDuAnCode(inputCode, loaiDuAn));
    }

    [Theory]
    [InlineData("PKG-001")]
    [InlineData("pkg-test")]
    public void ValidateGoiThauCode_ValidCodes_DoesNotThrow(string inputCode)
    {
        var exception = Record.Exception(() => CodePrefixValidator.ValidateGoiThauCode(inputCode));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("001")]
    [InlineData("GT-001")]
    public void ValidateGoiThauCode_InvalidCodes_ThrowsArgumentException(string inputCode)
    {
        Assert.Throws<ArgumentException>(() => CodePrefixValidator.ValidateGoiThauCode(inputCode));
    }

    [Theory]
    [InlineData("CTR-001")]
    [InlineData("ctr-contract")]
    public void ValidateHopDongCode_ValidCodes_DoesNotThrow(string inputCode)
    {
        var exception = Record.Exception(() => CodePrefixValidator.ValidateHopDongCode(inputCode));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("001")]
    [InlineData("HD-001")]
    public void ValidateHopDongCode_InvalidCodes_ThrowsArgumentException(string inputCode)
    {
        Assert.Throws<ArgumentException>(() => CodePrefixValidator.ValidateHopDongCode(inputCode));
    }

    [Theory]
    [InlineData("PAY-Đợt 1")]
    [InlineData("pay-01")]
    public void ValidateDotThanhToanTen_ValidNames_DoesNotThrow(string inputTen)
    {
        var exception = Record.Exception(() => CodePrefixValidator.ValidateDotThanhToanTen(inputTen));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("Đợt 1")]
    [InlineData("01")]
    public void ValidateDotThanhToanTen_InvalidNames_ThrowsArgumentException(string inputTen)
    {
        Assert.Throws<ArgumentException>(() => CodePrefixValidator.ValidateDotThanhToanTen(inputTen));
    }
}
