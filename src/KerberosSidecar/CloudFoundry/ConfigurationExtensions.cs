namespace KerberosSidecar.CloudFoundry;

public static class ConfigurationExtensions
{
    public static IEnumerable<ServiceBinding> GetServiceBindings(this IConfiguration config)
    {
        return config.GetSection("vcap:services").GetChildren().SelectMany(serviceTypeSection =>
        {
            var serviceType = serviceTypeSection.Key;

            return serviceTypeSection.Get<List<ServiceBinding>>(c =>
                {
                    c.BindNonPublicProperties = true;
                })
                .Select(x =>
                {
                    x.Type = serviceType;
                    return x;
                });
        });
    }
}