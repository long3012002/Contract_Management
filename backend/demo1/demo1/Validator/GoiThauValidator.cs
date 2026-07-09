using System;

namespace demo1.Validator;

public static class GoiThauValidator
{
    public static void EnsureValid(decimal giaTriGoiThau, decimal nguongCanhBaoPercent)
    {
        if (giaTriGoiThau < 0)
        {
            throw new ArgumentException("Giá trị gói thầu không được âm.");
        }

        if (nguongCanhBaoPercent <= 0 || nguongCanhBaoPercent > 100)
        {
            throw new ArgumentException("Ngưỡng cảnh báo phải nằm trong khoảng 1 đến 100.");
        }
    }
}
