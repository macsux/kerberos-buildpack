using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace KerberosCommon;

public class Util
{
    public static bool TryGetCredentials(out NetworkCredential credential)
    {
        if (!TryGetCredentials(out credential, out var errors))
        {
            
            foreach (var error in errors)
            {
                Console.Error.WriteLine(error);
                return false;
            }
        }

        return true;
    }
    public static bool TryGetCredentials(out NetworkCredential credential, out List<string> errors)
    {
        credential = default!;
        var upn = Environment.GetEnvironmentVariable("KRB_SERVICE_ACCOUNT");
        errors = new List<string>();
        string[] principalParts = new string[1];
        
        if (upn == null)
        {
            errors.Add("Required KRB_SERVICE_ACCOUNT environmental variable is not set. Must be set to service account under which service will run provided in account@domain.com format");
        }
        else
        {
            principalParts = upn.Split("@");
            if (principalParts.Length != 2)
            {
                errors.Add("KRB_SERVICE_ACCOUNT must be in account@domain.com format");
            }
        }
            
        var password = Environment.GetEnvironmentVariable("KRB_PASSWORD");
        if (password == null)
        {
            errors.Add("Required KRB_PASSWORD environmental variable is not set");
        }

        if (errors.Any())
        {
            return false;
        }


        credential = new NetworkCredential(principalParts[0], password, principalParts[1]);
        return true;
    }
}