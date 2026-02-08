using System.ServiceModel;
using System.Xml;
using Microsoft.Extensions.Options;

namespace Bramki_Evacuation.Wcf;

public sealed class ApiClientFactory
{
    private readonly ApiOptions _opt;

    public ApiClientFactory(IOptions<ApiOptions> opt) => _opt = opt.Value;

    private string U(string tail) => _opt.BaseUrl.TrimEnd('/') + "/" + tail.TrimStart('/');

    private BasicHttpBinding CreateBinding()
        => new(BasicHttpSecurityMode.None)
        {
            MaxBufferSize = int.MaxValue,
            MaxReceivedMessageSize = int.MaxValue,
            ReaderQuotas = XmlDictionaryReaderQuotas.Max,
            AllowCookies = true,
            OpenTimeout = TimeSpan.FromSeconds(30),
            CloseTimeout = TimeSpan.FromSeconds(30),
            SendTimeout = TimeSpan.FromSeconds(_opt.TimeoutSeconds),
            ReceiveTimeout = TimeSpan.FromSeconds(_opt.TimeoutSeconds),
        };

    public SessionManagement.SessionManagementServiceClient CreateSession()
    {
        var c = new SessionManagement.SessionManagementServiceClient(
            CreateBinding(),
            new EndpointAddress(U("SessionManagement")));

        c.InnerChannel.OperationTimeout = TimeSpan.FromSeconds(_opt.TimeoutSeconds);
        return c;
    }

    public Integration.IntegrationServiceClient CreateIntegration()
    {
        var c = new Integration.IntegrationServiceClient(
            CreateBinding(),
            new EndpointAddress(U("Integration")));

        c.InnerChannel.OperationTimeout = TimeSpan.FromSeconds(_opt.TimeoutSeconds);
        return c;
    }
}