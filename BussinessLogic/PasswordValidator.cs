using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BussinessLogic;

public class PasswordValidator
{
    public static bool IsValid(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;
        var r = new Regex("^(?=.{8,}$)(?=.*[A-Z])(?=.*\\d)(?=.*\\W).*$");
        return r.IsMatch(password);
    }
}
