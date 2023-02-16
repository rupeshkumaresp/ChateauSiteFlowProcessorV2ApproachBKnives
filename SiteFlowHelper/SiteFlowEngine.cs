using PicsMeOrderHelper;
using SiteFlowHelper.Interface;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SiteFlowHelper
{
    /// <summary>
    /// SITE FLOW ENGINE - PROCESS ORDERS TO SITE FLOW
    /// </summary>
    public class SiteFlowEngine : ISiteFlowEngine
    {
        private readonly string _baseUrlSiteFlow;
        readonly OrderHelper _orderHelper = new OrderHelper();

        public SiteFlowEngine(string baseUrlSiteFlow, string siteflowKey, string siteflowSecretKey)
        {
            _baseUrlSiteFlow = baseUrlSiteFlow;
        }

        public void CreateHmacHeadersSiteFlow(string method, string path, HttpClient client)
        {
            string timeStamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            string stringToSign = method + " " + path + " " + timeStamp;
            HMACSHA1 hmac = new HMACSHA1(Encoding.UTF8.GetBytes("3481f6a9b7209d5a34d229fafe695992a072c3fd72c62702"));
            byte[] signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
            string signature = BitConverter.ToString(signatureBytes).Replace("-", "").ToLower();
            string authHeader = "2599971514309" + ":" + signature;

            client.DefaultRequestHeaders.Add("x-oneflow-authorization", authHeader);
            client.DefaultRequestHeaders.Add("x-oneflow-date", timeStamp);

        }

        public async Task SubmitOrder(HttpContent content, string orderId)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            using (var client = new HttpClient())
            {
                string path = "/api/order";
                CreateHmacHeadersSiteFlow("POST", path, client);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                try
                {

                    HttpResponseMessage response = await client.PostAsync(string.Format("{0}{1}", _baseUrlSiteFlow, path), content);


                    Stream receiveStream = await response.Content.ReadAsStreamAsync();
                    StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
                    var error = readStream.ReadToEnd();

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();

                        MarkSiteFlowSubmissionSucess(orderId);
                    }                    
                }
                catch (Exception ex)
                {
                    var msg = ex.Message;
                }
            }
        }

        public void MarkSiteFlowSubmissionSucess(string orderId)
        {

            _orderHelper.MarkOrderPushedTositeFlow(orderId);
        }

        public async Task PushOrderToSiteFlow(long orderid)
        {

            var sourceOrderId=_orderHelper.GetOrderSourceOrderId(orderid);
            var json = _orderHelper.GetModifiedSiteflowOrderJson(orderid);

            if (!string.IsNullOrEmpty(json))
            {
                var serializedResultJson = json;

                var httpContent = new StringContent(serializedResultJson, Encoding.UTF8, "application/json");

                await SubmitOrder(httpContent, sourceOrderId);
            }
        }
    }
}
