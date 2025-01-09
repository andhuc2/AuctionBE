using API.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using static System.Net.WebRequestMethods;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public PaymentController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        [Route("pay")]
        public IActionResult CreatePayment([FromBody] OrderInfo order)
        {
            string vnp_Returnurl = "http://localhost:5001/api/Payment/notify";
            string vnp_Url = "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction";
            string vnp_TmnCode = "CGXZLS0Z";
            string vnp_HashSecret = "XNBCJFAKAZQSGTARRLGCHVZWCIOIGSHN";

            if (order == null)
                return BadRequest("Order information is required.");

            order.OrderId = DateTime.Now.Ticks;
            order.Status = "0"; // Order status: 0 - pending
            order.CreatedDate = DateTime.Now;

            VnPayLibrary vnpay = new VnPayLibrary();
            vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", (order.Amount * 100).ToString()); // Convert to cents
            vnpay.AddRequestData("vnp_CreateDate", order.CreatedDate.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", VnPayLibrary.GetIpAddress(HttpContext));  // Correctly passing HttpContext
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang:" + order.OrderId);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", order.OrderId.ToString());

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

            return Redirect(paymentUrl);
        }

        [HttpGet]
        [Route("notify")]
        public IActionResult PaymentNotify()
        {
            string returnContent = string.Empty;
            if (Request.Query.Count > 0)
            {
                string vnp_HashSecret = _configuration["vnp_HashSecret"];
                var vnpayData = Request.Query;
                VnPayLibrary vnpay = new VnPayLibrary();

                foreach (var item in vnpayData)
                {
                    if (!string.IsNullOrEmpty(item.Value) && item.Key.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(item.Key, item.Value);
                    }
                }

                long orderId = Convert.ToInt64(vnpay.GetResponseData("vnp_TxnRef"));
                long vnp_Amount = Convert.ToInt64(vnpay.GetResponseData("vnp_Amount")) / 100;
                long vnpayTranId = Convert.ToInt64(vnpay.GetResponseData("vnp_TransactionNo"));
                string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                string vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
                string vnp_SecureHash = Request.Query["vnp_SecureHash"];

                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);
                if (checkSignature)
                {
                    // Simulating the order retrieval from the database
                    OrderInfo order = GetOrderFromDatabase(orderId); // Simulate database lookup
                    if (order != null)
                    {
                        if (order.Amount == vnp_Amount)
                        {
                            if (order.Status == "0")
                            {
                                if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
                                {
                                    order.Status = "1"; // Payment successful
                                    returnContent = "{\"RspCode\":\"00\",\"Message\":\"Confirm Success\"}";
                                }
                                else
                                {
                                    order.Status = "2"; // Payment failed
                                    returnContent = "{\"RspCode\":\"02\",\"Message\":\"Payment failed\"}";
                                }
                            }
                            else
                            {
                                returnContent = "{\"RspCode\":\"02\",\"Message\":\"Order already confirmed\"}";
                            }
                        }
                        else
                        {
                            returnContent = "{\"RspCode\":\"04\",\"Message\":\"Invalid amount\"}";
                        }
                    }
                    else
                    {
                        returnContent = "{\"RspCode\":\"01\",\"Message\":\"Order not found\"}";
                    }
                }
                else
                {
                    returnContent = "{\"RspCode\":\"97\",\"Message\":\"Invalid signature\"}";
                }
            }
            else
            {
                returnContent = "{\"RspCode\":\"99\",\"Message\":\"Input data required\"}";
            }

            return Ok(returnContent);
        }

        private OrderInfo GetOrderFromDatabase(long orderId)
        {
            // Simulating an order lookup from the database
            return new OrderInfo
            {
                OrderId = orderId,
                Amount = 100000, // Example amount in VND (1000 VND)
                Status = "0",    // Simulating an order with status "0" (Pending)
                CreatedDate = DateTime.Now
            };
        }
    }
}
