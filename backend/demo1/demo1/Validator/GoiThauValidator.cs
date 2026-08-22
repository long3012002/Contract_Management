using System;
using System.Collections.Generic;
using System.Linq;
using demo1.DTOs;

namespace demo1.Validator;

public static class GoiThauValidator
{
    public static void EnsureValid(decimal giaTriGoiThau)
    {
        if (giaTriGoiThau < 0)
        {
            throw new ArgumentException("Giá trị gói thầu không được âm.");
        }
    }


}
