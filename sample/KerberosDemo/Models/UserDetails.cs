using System.Collections.Generic;

namespace KerberosDemo.Models;

public class UserDetails
{
    public string Name { get; set; }
    public List<ClaimSummary> Claims { get; set; }
}