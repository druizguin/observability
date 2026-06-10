namespace WebOpenObserveDemo.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

public class MonitorController : Controller
{
    private readonly HttpClient _http;

    public MonitorController(IHttpClientFactory factory)
    {
        _http = factory.CreateClient();
    }

    public async Task<IActionResult> Index()
    {
        var req = new HttpRequestMessage(HttpMethod.Get,
            "http://localhost:5080/api/default/logs?size=20");

        req.Headers.Authorization =
            new AuthenticationHeaderValue("Basic",
                "cm9vdEBleGFtcGxlLmNvbTpDb21wbGV4UGFzczEyMw==");

        var res = await _http.SendAsync(req);
        var json = await res.Content.ReadAsStringAsync();

        ViewBag.Data = json;
        return View();
    }
}
