using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MrMoney.Api.Models;
using MrMoney.Api.Repositories;
using MimeKit;
using MailKit.Net.Smtp;

namespace MrMoney.Api.Infrastructure
{
    public class EmailService
    {
        private readonly IAdminEmailRepository _adminEmailRepo;

        public EmailService(IAdminEmailRepository adminEmailRepo)
        {
            _adminEmailRepo = adminEmailRepo;
        }

        public async Task SendNewOrderEmailAsync(Order order)
        {
            var host = "smtp.gmail.com";
            var port = 587;
            var username = "mohanmano2020@gmail.com";
            var password = "vkwk phnl duhc wqpk"; // Hardcoded Google App password

            // Fetch recipient list dynamically from Google Sheets
            var adminEmails = await _adminEmailRepo.GetAllAsync();
            if (adminEmails.Count == 0)
            {
                Console.WriteLine("No admin email recipients configured. Skipping email notification.");
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("StarGraphix Order Notifications", username));
            foreach (var email in adminEmails)
            {
                message.To.Add(new MailboxAddress("", email));
            }
            message.Subject = $"New Order Placed: #{order.Id} - ₹{order.Total:N0}";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = GenerateHtmlBody(order)
            };
            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(username, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }

        private string GenerateHtmlBody(Order order)
        {
            var itemsHtml = "";
            var culture = new System.Globalization.CultureInfo("en-IN");
            
            if (order.Items != null)
            {
                foreach (var item in order.Items)
                {
                    var itemImageHtml = "";
                    if (!string.IsNullOrEmpty(item.Image))
                    {
                        var imageUrl = item.Image;
                        // Support relative backend images or external URLs
                        if (imageUrl.StartsWith("/"))
                        {
                            imageUrl = "https://stargraphix-react-app.vercel.app" + imageUrl;
                        }
                        itemImageHtml = $"<img src='{imageUrl}' alt='{item.Name}' style='width: 50px; height: 50px; object-fit: cover; border-radius: 8px; border: 1px solid #eeeeee;' />";
                    }

                    itemsHtml += $@"
                    <tr>
                        <td style='padding: 12px; border-bottom: 1px solid #eeeeee; vertical-align: middle;'>
                            <table border='0' cellpadding='0' cellspacing='0'>
                                <tr>
                                    {(string.IsNullOrEmpty(itemImageHtml) ? "" : $"<td style='padding-right: 12px; vertical-align: middle;'>{itemImageHtml}</td>")}
                                    <td style='vertical-align: middle;'>
                                        <p style='margin: 0; font-weight: bold; color: #2d3748; font-size: 14px;'>{item.Name}</p>
                                        <p style='margin: 3px 0 0 0; font-size: 12px; color: #718096;'>Qty: {item.Qty} &times; ₹{item.Price:N0}</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                        <td style='padding: 12px; border-bottom: 1px solid #eeeeee; text-align: right; font-weight: bold; color: #2d3748; vertical-align: middle; font-size: 14px;'>
                            ₹{(item.Price * item.Qty):N0}
                        </td>
                    </tr>";
                }
            }

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>New Order Received</title>
</head>
<body style='margin: 0; padding: 0; background-color: #f7fafc; font-family: ""Outfit"", -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif;'>
    <table border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color: #f7fafc; padding: 25px 0;'>
        <tr>
            <td align='center'>
                <table border='0' cellpadding='0' cellspacing='0' width='600' style='background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.05), 0 4px 6px -2px rgba(0, 0, 0, 0.025); border: 1px solid #edf2f7;'>
                    <!-- Top Accent Header -->
                    <tr>
                        <td align='center' style='background: linear-gradient(135deg, #e53e3e 0%, #c53030 100%); padding: 35px 24px; text-align: center;'>
                            <h1 style='margin: 0; color: #ffffff; font-size: 26px; font-weight: 900; letter-spacing: 1px; text-transform: uppercase;'>Star Graphix</h1>
                            <p style='margin: 6px 0 0 0; color: #fed7d7; font-size: 14px; font-weight: 500;'>New Design Order Notification</p>
                        </td>
                    </tr>
                    
                    <!-- Content Body -->
                    <tr>
                        <td style='padding: 30px 24px;'>
                            <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                                <tr>
                                    <td>
                                        <h2 style='margin: 0 0 15px 0; color: #1a202c; font-size: 18px; font-weight: 700; border-bottom: 2px solid #edf2f7; padding-bottom: 8px;'>Order Metadata</h2>
                                    </td>
                                </tr>
                                <tr>
                                    <td style='padding-bottom: 25px;'>
                                        <table border='0' cellpadding='0' cellspacing='0' width='100%' style='font-size: 14px; color: #4a5568;'>
                                            <tr>
                                                <td style='padding: 6px 0; font-weight: 600; color: #718096; width: 140px;'>Order ID</td>
                                                <td style='padding: 6px 0; font-weight: 700; color: #1a202c; font-family: monospace; font-size: 15px;'>#{order.Id}</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 6px 0; font-weight: 600; color: #718096;'>Placed On</td>
                                                <td style='padding: 6px 0; font-weight: 600; color: #2d3748;'>{order.PlacedAt.ToString("f")}</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 6px 0; font-weight: 600; color: #718096;'>Client Name</td>
                                                <td style='padding: 6px 0; font-weight: 600; color: #2d3748;'>{order.Name}</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 6px 0; font-weight: 600; color: #718096;'>Client Email</td>
                                                <td style='padding: 6px 0; font-weight: 600; color: #2d3748;'>{order.Email}</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 6px 0; font-weight: 600; color: #718096;'>Client Phone</td>
                                                <td style='padding: 6px 0; font-weight: 600; color: #2d3748;'>{order.Phone}</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 6px 0; font-weight: 600; color: #718096; vertical-align: top;'>Address</td>
                                                <td style='padding: 6px 0; font-weight: 600; color: #2d3748;'>
                                                    {order.Address}, {order.City}, {order.State} - {order.Pincode}
                                                </td>
                                            </tr>
                                            {(string.IsNullOrEmpty(order.Notes) ? "" : $@"
                                            <tr>
                                                <td style='padding: 6px 0; font-weight: 600; color: #718096; vertical-align: top;'>Notes</td>
                                                <td style='padding: 8px 12px; font-weight: 500; color: #9c4221; background-color: #fffaf0; border-radius: 8px; border: 1px solid #feebc8;'>
                                                    {order.Notes}
                                                </td>
                                            </tr>")}
                                        </table>
                                    </td>
                                </tr>
                                
                                <tr>
                                    <td>
                                        <h2 style='margin: 0 0 15px 0; color: #1a202c; font-size: 18px; font-weight: 700; border-bottom: 2px solid #edf2f7; padding-bottom: 8px;'>Ordered Products</h2>
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <table border='0' cellpadding='0' cellspacing='0' width='100%' style='border-collapse: collapse;'>
                                            {itemsHtml}
                                        </table>
                                    </td>
                                </tr>
                                
                                <tr>
                                    <td style='padding-top: 20px;'>
                                        <table border='0' cellpadding='0' cellspacing='0' width='100%' style='border-top: 2px solid #edf2f7; padding-top: 15px; font-size: 14px; color: #4a5568;'>
                                            <tr>
                                                <td style='padding: 6px 0; color: #718096;'>Subtotal</td>
                                                <td style='padding: 6px 0; text-align: right; font-weight: 600; color: #2d3748;'>₹{order.Subtotal:N0}</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 6px 0; color: #718096;'>GST (18%)</td>
                                                <td style='padding: 6px 0; text-align: right; font-weight: 600; color: #2d3748;'>₹{order.Gst:N0}</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 8px 0 0 0; font-weight: bold; color: #1a202c; font-size: 16px;'>Grand Total</td>
                                                <td style='padding: 8px 0 0 0; text-align: right; font-weight: 800; color: #e53e3e; font-size: 20px;'>₹{order.Total:N0}</td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                                
                                <tr>
                                    <td align='center' style='padding-top: 35px;'>
                                        <table border='0' cellpadding='0' cellspacing='0'>
                                            <tr>
                                                <td align='center' style='border-radius: 10px; background-color: #c53030;'>
                                                    <a href='https://stargraphix-react-app.vercel.app/admin/orders' target='_blank' style='border: 1px solid #c53030; border-radius: 10px; color: #ffffff; display: inline-block; font-size: 14px; font-weight: bold; padding: 14px 28px; text-decoration: none; text-transform: uppercase; letter-spacing: 0.5px;'>Open Admin Dashboard</a>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    
                    <!-- Footer Info -->
                    <tr>
                        <td align='center' style='background-color: #f7fafc; padding: 24px; border-top: 1px solid #edf2f7; font-size: 12px; color: #a0aec0; text-align: center;'>
                            <p style='margin: 0;'>Automated system notification from <strong>STAR GRAPHIX</strong>.</p>
                            <p style='margin: 4px 0 0 0;'>Do not reply directly to this email address.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }
    }
}
