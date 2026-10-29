using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1.Imagens;

public class ImagensRequest
{
    [FromForm(Name = "arquivo")]
    public IFormFile Arquivo { get; set; } = default!;
}
