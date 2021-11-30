using System.Collections.Generic;

namespace KerberosBuildpack;

public class SidecarProcessConfig
{
    public List<SidecarProcess> Processes { get; set; }

    public class SidecarProcess
    {
        public string Type { get; set; }
        public string Command { get; set; }
        public string Limits { get; set; }
    }
}