﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Net.Mail;
using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json.Nodes;

namespace Demo;

public class Helper
{
    private readonly IWebHostEnvironment en;
    private readonly IHttpContextAccessor ct;
    private readonly IConfiguration cf;

    public Helper(IWebHostEnvironment en,
                  IHttpContextAccessor ct,
                  IConfiguration cf)
    {
        this.en = en;
        this.ct = ct;
        this.cf = cf;
    }



    // ------------------------------------------------------------------------
    // Photo Upload
    // ------------------------------------------------------------------------

    public string ValidatePhoto(IFormFile f)
    {
        var reType = new Regex(@"^image\/(jpeg|png)$", RegexOptions.IgnoreCase);
        var reName = new Regex(@"^.+\.(jpeg|jpg|png)$", RegexOptions.IgnoreCase);

        if (!reType.IsMatch(f.ContentType) || !reName.IsMatch(f.FileName))
        {
            return "Only JPG and PNG photo is allowed.";
        }
        else if (f.Length > 1 * 1024 * 1024)
        {
            return "Photo size cannot more than 1MB.";
        }

        return "";
    }

    public string SavePhoto(IFormFile f, string folder)
    {
        var file = Guid.NewGuid().ToString("n") + ".jpg";
        var path = Path.Combine(en.WebRootPath, folder, file);

        var options = new ResizeOptions
        {
            Size = new(200, 200),
            Mode = ResizeMode.Crop,
        };

        using var stream = f.OpenReadStream();
        using var img = Image.Load(stream);
        img.Mutate(x => x.Resize(options));
        img.Save(path);

        return file;
    }

    public void DeletePhoto(string file, string folder)
    {
        file = Path.GetFileName(file);
        var path = Path.Combine(en.WebRootPath, folder, file);
        File.Delete(path);
    }



    // ------------------------------------------------------------------------
    // Security Helper Functions
    // ------------------------------------------------------------------------

    private readonly PasswordHasher<object> ph = new();

    public string HashPassword(string password)
    {
        return ph.HashPassword(0, password);
    }

    public bool VerifyPassword(string hash, string password)
    {
        return ph.VerifyHashedPassword(0, hash, password)
               == PasswordVerificationResult.Success;
    }

    public void SignIn(string id, string role, bool rememberMe)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.Name, id),
            new(ClaimTypes.Role, role),
        ];

        ClaimsIdentity identity = new(claims, "Cookies");

        ClaimsPrincipal principal = new(identity);

        AuthenticationProperties properties = new()
        {
            IsPersistent = rememberMe,
        };

        ct.HttpContext!.SignInAsync(principal, properties);
    }

    public void SignOut()
    {
        // Clear all session data
        ct.HttpContext!.Session.Clear();

        ct.HttpContext!.SignOutAsync();
    }

    public string RandomPassword()
    {
        string s = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string password = "";

        Random r = new();

        for (int i = 1; i <= 10; i++)
        {
            password += s[r.Next(s.Length)];
        }

        return password;
    }



    // ------------------------------------------------------------------------
    // Email Helper Functions
    // ------------------------------------------------------------------------

    public async void SendEmail(MailMessage mail)
    {
        // TODO
        string user = cf["Smtp:User"] ?? "";
        string pass = cf["Smtp:Pass"] ?? "";
        string name = cf["Smtp:Name"] ?? "";
        string host = cf["Smtp:Host"] ?? "";
        int port = cf.GetValue<int>("Smtp:Port");
        mail.From = new MailAddress(user, name);

        using var smtp = new SmtpClient
        {
            Host = host,
            Port = port,
            EnableSsl = true,
            Credentials = new NetworkCredential(user, pass),
        };
        // TODO
        await smtp.SendMailAsync(mail);
    }


    public bool VerifyReCaptchaV2(string response)
    {
        Console.WriteLine("Received CAPTCHA Token: " + response);

        if (string.IsNullOrWhiteSpace(response))
        {
            Console.WriteLine("Error: No CAPTCHA token provided.");
            return false;
        }

        string secret = cf["ReCaptchaSettings:SecretKey"] ?? "";
        if (string.IsNullOrWhiteSpace(secret))
        {
            Console.WriteLine("Error: No ReCAPTCHA secret key configured.");
            return false;
        }

        //using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
        //{
        //    // Use FormUrlEncodedContent for URL-encoded form data
        //    var content = new FormUrlEncodedContent(new[]
        //    {
        //    new KeyValuePair<string, string>("response", response),
        //    new KeyValuePair<string, string>("secret", secret)
        //});

        //    try
        //    {
        //        // Post the data to Google's reCAPTCHA verification API
        //        var result = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);

        //        // Check if the HTTP response is successful
        //        if (result.IsSuccessStatusCode)
        //        {
        //            var strResponse = await result.Content.ReadAsStringAsync();
        //            Console.WriteLine("Google Response: " + strResponse);

        //            try
        //            {
        //                var jsonResponse = JsonNode.Parse(strResponse) as JsonObject;
        //                if (jsonResponse != null)
        //                {
        //                    var success = jsonResponse["success"]?.GetValue<bool>();
        //                    if (success == true)
        //                    {
        //                        Console.WriteLine("reCAPTCHA verification successful.");
        //                        return true;
        //                    }
        //                    else
        //                    {
        //                        if (jsonResponse.ContainsKey("error-codes"))
        //                        {
        //                            var errorCodes = jsonResponse["error-codes"].AsArray();
        //                            foreach (var errorCode in errorCodes)
        //                            {
        //                                Console.WriteLine("reCAPTCHA Error: " + errorCode.ToString());
        //                            }
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    Console.WriteLine("Error: Unable to parse reCAPTCHA response JSON.");
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine("Error parsing reCAPTCHA response: " + ex.Message);
        //            }
        //        }
        //        else
        //        {
        //            Console.WriteLine("HTTP Error: " + result.StatusCode);
        //            var errorResponse = await result.Content.ReadAsStringAsync();
        //            Console.WriteLine("Error Response: " + errorResponse);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error during reCAPTCHA verification request: " + ex.Message);
        //    }
        //}

        return true;
    }


    // ------------------------------------------------------------------------
    // DateTime Helper Functions
    // ------------------------------------------------------------------------

    // Return January (1) to December (12)
    public SelectList GetMonthList()
    {
        var list = new List<object>();

        for (int n = 1; n <= 12; n++)
        {
            list.Add(new
            {
                Id = n,
                Name = new DateTime(1, n, 1).ToString("MMMM"),
            });
        }

        return new SelectList(list, "Id", "Name");
    }

    // Return min to max years
    public SelectList GetYearList(int min, int max, bool reverse = false)
    {
        var list = new List<int>();

        for (int n = min; n <= max; n++)
        {
            list.Add(n);
        }

        if (reverse) list.Reverse();

        return new SelectList(list);
    }
}
