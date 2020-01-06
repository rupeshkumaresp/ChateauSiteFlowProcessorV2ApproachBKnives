using System.Threading.Tasks;
using System.Net.Http;

namespace SiteFlowHelper.Interface
{
    /// <summary>
    /// SITEFLOW INTERFACE - CREATE SITE FLOW ORDER
    /// </summary>
    public interface ISiteFlowEngine
    {
        void CreateHmacHeadersSiteFlow(string method, string path, HttpClient client);
        Task SubmitOrder(HttpContent content, string orderId);
        void MarkSiteFlowSubmissionSucess(string orderId);
        Task PushOrderToSiteFlow(long orderid);
    }
}
