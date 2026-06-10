namespace Rating.BusinessLayer.Controllers;

using Microsoft.AspNetCore.Mvc;

public class ApiHeaders
{
    [FromHeader(Name = "X-tenant")]
    public string? Tenant { get; set; }
}
