using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace QuanLyCuaHangMyPham.Services
{
    public class MoMoPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public MoMoPaymentService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<dynamic> CreatePaymentAsync(string orderId, decimal amount, string returnUrl, string notifyUrl)
        {
            var endpoint = _configuration["MoMo:Endpoint"];
            var partnerCode = _configuration["MoMo:PartnerCode"];
            var accessKey = _configuration["MoMo:AccessKey"];
            var secretKey = _configuration["MoMo:SecretKey"];
            var requestId = Guid.NewGuid().ToString(); // Mã yêu cầu

            var requestBody = new
            {
                partnerCode = partnerCode,
                accessKey = accessKey,
                requestId = requestId,
                orderId = orderId,
                orderInfo = $"Thanh toán đơn hàng #{orderId}",
                amount = amount.ToString(),
                returnUrl = returnUrl,
                notifyUrl = notifyUrl,
                requestType = "captureWallet"
            };

            // Tạo chữ ký
            var rawSignature = $"accessKey={accessKey}&amount={amount}&orderId={orderId}&orderInfo={requestBody.orderInfo}&partnerCode={partnerCode}&requestId={requestId}&returnUrl={returnUrl}&notifyUrl={notifyUrl}&requestType=captureWallet";
            var signature = CreateSignature(secretKey, rawSignature);

            // Gửi yêu cầu POST đến MoMo
            var payload = new
            {
                partnerCode = partnerCode,
                accessKey = accessKey,
                requestId = requestId,
                orderId = orderId,
                orderInfo = requestBody.orderInfo,
                amount = requestBody.amount,
                returnUrl = requestBody.returnUrl,
                notifyUrl = requestBody.notifyUrl,
                requestType = requestBody.requestType,
                signature = signature
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(endpoint, content);

            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<dynamic>(responseContent);
        }

        private string CreateSignature(string secretKey, string data)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
    }
}
