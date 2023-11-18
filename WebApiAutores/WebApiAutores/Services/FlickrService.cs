namespace WebApiAutores.Services
{
    public class FlickrService
    {
        private readonly HttpClient _httpClient;
        private const string ApiKey = "4339d0f4c745cf92b20763f02dbbba8b"; // Reemplaza con tu API key
        private const string ApiSecret = "87fcd9ce0d29b829"; // Reemplaza con tu API secret

        public FlickrService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> SubirImagenPortada(string base64Image)
        {
            var content = new MultipartFormDataContent();
            content.Add(new StringContent(ApiKey), "api_key");
            content.Add(new StringContent("upload"), "method");
            content.Add(new StringContent("json"), "format");
            content.Add(new StringContent("1"), "nojsoncallback");
            content.Add(new StringContent(base64Image), "photo");

            // Agregar la firma usando el secreto
            var firma = GenerarFirma(ApiSecret, content);

            // Agregar la firma a la solicitud
            content.Add(new StringContent(firma), "firma");

            var response = await _httpClient.PostAsync("https://up.flickr.com/services/upload/", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            // Procesar la respuesta de Flickr para obtener la URL de la imagen subida
            return responseContent;
        }

        // Método para generar la firma usando el secreto
        private string GenerarFirma(string apiSecret, MultipartFormDataContent content)
        {
            // Aquí deberías implementar el algoritmo de firma adecuado proporcionado por Flickr.
            // La firma se genera combinando los parámetros y el secreto usando un algoritmo específico.
            // Este código es solo un ejemplo de cómo podría ser el proceso, debes ajustarlo según las especificaciones de Flickr.

            // Ejemplo simple (NO seguro para producción)
            var parametros = content.Where(p => p.Headers.ContentDisposition != null)
                                    .OrderBy(p => p.Headers.ContentDisposition.Name)
                                    .Select(p => $"{p.Headers.ContentDisposition.Name}={p.ReadAsStringAsync().Result}");

            var cadenaParametros = string.Join("&", parametros);
            var firma = ComputeHash(apiSecret + cadenaParametros); // Aquí deberías usar un algoritmo de firma seguro

            return firma;
        }

        // Método para calcular un hash (solo como ejemplo, NO seguro para producción)
        private string ComputeHash(string input)
        {
            using (var sha = new System.Security.Cryptography.SHA256Managed())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(input);
                var hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }

}
