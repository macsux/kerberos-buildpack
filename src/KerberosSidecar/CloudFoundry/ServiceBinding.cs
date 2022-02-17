using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

#nullable disable
namespace KerberosSidecar.CloudFoundry;

[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class ServiceBinding
{
    public string Name { get; set; }
    public string Type {get;set;}
    public string Plan { get; set; }
    public IConfigurationSection Credentials { get; set; }
    public T GetCredentials<T>() => Credentials.Get<T>();
    public List<string> Tags {get;set;}
    public string SyslogDrainUrl {get;set;}
    private string Syslog_Drain_Url { set => SyslogDrainUrl = value; }
    public string Provider {get;set;}
    public string Label {get;set;}
    public string InstanceName { get; set; }
    private string Instance_Name { set => InstanceName = value; }
    public string BindingName {get;set;}
    private string Binding_Name { set => BindingName = value; }
}