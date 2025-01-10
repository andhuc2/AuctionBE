﻿using API.Models;
using API.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NET_base.Models.Common;
using System;
using static System.Net.WebRequestMethods;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly DbContext _context;

        public PaymentController(IConfiguration configuration, DbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        [HttpPost]
        [Route("pay")]
        public Response<string> CreatePayment(OrderInfo order)
        {
            string vnp_Returnurl = "http://localhost:5001/api/Payment/notify";
            string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            string vnp_TmnCode = "CGXZLS0Z";
            string vnp_HashSecret = "XNBCJFAKAZQSGTARRLGCHVZWCIOIGSHN";

            if (order == null)
                return new Response<string>(false, "Order information is required.", null);

            string OrderId = DateTime.Now.Ticks.ToString();

            VnPayLibrary vnpay = new VnPayLibrary();
            vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", (order.Amount * 100).ToString()); // Convert to cents
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", VnPayLibrary.GetIpAddress(HttpContext));
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang:" + OrderId);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", OrderId);

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

            return new Response<string>(true, "Success", paymentUrl);
        }

        [HttpGet]
        [Route("notify")]
        public Response<bool> PaymentNotify()
        {
            try
            {
                if (Request.Query.Count == 0)
                {
                    return new Response<bool>(false, "Input data required", false);
                }

                string vnp_HashSecret = "XNBCJFAKAZQSGTARRLGCHVZWCIOIGSHN";
                var vnpayData = Request.Query;
                VnPayLibrary vnpay = new VnPayLibrary();

                foreach (var item in vnpayData)
                {
                    if (!string.IsNullOrEmpty(item.Value) && item.Key.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(item.Key, item.Value);
                    }
                }

                if (!long.TryParse(vnpay.GetResponseData("vnp_Amount"), out long vnp_Amount))
                {
                    return new Response<bool>(false, "Invalid amount", false);
                }
                vnp_Amount /= 100;

                string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                string vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
                string vnp_SecureHash = Request.Query["vnp_SecureHash"];

                if (string.IsNullOrEmpty(vnp_SecureHash))
                {
                    return new Response<bool>(false, "Invalid signature", false);
                }

                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);
                if (!checkSignature)
                {
                    return new Response<bool>(false, "Invalid signature", false);
                }


                if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
                {
                    return new Response<bool>(true, "Success", true);
                }
                else
                {
                    throw new Exception("Fail");
                }
            } catch (Exception e)
            {
                return new Response<bool>(false, e.Message, false);
            }
        }
    }
}
