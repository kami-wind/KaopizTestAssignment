using DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BussinessLogic;

public class AuthResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? JwtToken { get; set; }
    public string? RefreshToken { get; set; } // plain token returned to be stored in cookie (server stores hash)
    public Account? Account { get; set; }
}
