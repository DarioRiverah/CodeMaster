using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace juezprueba.Services
{
    public class Judge0Service
    {
        private readonly HttpClient _httpClient;

        public Judge0Service(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new System.Uri("https://judge0-ce.p.rapidapi.com/");
            _httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Key", "328c03af5amsha9cdcc9edbe7081p1fd1d1jsn7c2f3e68119d");
            _httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Host", "judge0-ce.p.rapidapi.com");
        }

        public async Task<string> EnviarCodigoAsync(string sourceCode, int languageId, string input)
        {
            if (string.IsNullOrWhiteSpace(sourceCode))
                return "Error: El código fuente no puede estar vacío.";

            if (languageId <= 0)
                return "Error: El ID del lenguaje no es válido.";

            var payload = new
            {
                language_id = languageId,
                source_code = sourceCode,
                stdin = input
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("submissions?base64_encoded=false&wait=true", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return $"Error en la solicitud: {response.StatusCode}\nDetalles: {errorContent}";
            }

            var result = await response.Content.ReadAsStringAsync();
            return result;
        }

        public string ExtraerSalida(string json)
        {
            var data = JObject.Parse(json);
            var stdout = data["stdout"]?.ToString();
            var stderr = data["stderr"]?.ToString();
            var compileOutput = data["compile_output"]?.ToString();

            if (!string.IsNullOrWhiteSpace(stderr))
                return $"[STDERR]:\n{stderr}";

            if (!string.IsNullOrWhiteSpace(compileOutput))
                return $"[COMPILE ERROR]:\n{compileOutput}";

            return string.IsNullOrWhiteSpace(stdout) ? "No hay salida." : stdout.Trim();
        }

    }
}
