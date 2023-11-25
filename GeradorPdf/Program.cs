
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using IronPdf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class EntradaUsuario
{
    public static string ObterTópico()
    {
        Console.WriteLine("Olá, Bem vindo(a) ao GPdfs.");
        Console.WriteLine("Esse programa usa o ChatGPT como ferramenta pra geração de PDFs ou Arquivo de texto.");
        Console.WriteLine("Digite o tópico sobre o qual você deseja obter informações:");
        return Console.ReadLine();
    }

    public static int ObterOpcao()
    {
        Console.WriteLine("O que você gostaria de fazer?");
        Console.WriteLine("1. Gerar PDF");
        Console.WriteLine("2. Salvar em arquivo de texto");
        Console.Write("Escolha a opção (1 ou 2): ");

        int opcao;
        while (!int.TryParse(Console.ReadLine(), out opcao) || (opcao != 1 && opcao != 2))
        {
            Console.WriteLine("Opção inválida. Escolha 1 para gerar PDF ou 2 para salvar em arquivo de texto.");
            Console.Write("Escolha a opção (1 ou 2): ");
        }

        return opcao;
    }
}

public class GeradorPDFApplication
{
    static async Task Main()
    {
        string topicoUsuario = EntradaUsuario.ObterTópico();
        await GerarPDF(topicoUsuario);
    }

    public static async Task GerarPDF(string topicoUsuario)
    {
        try
        {
            Console.WriteLine("Aguarde enquanto processamos sua solicitação...");

            // Obter resposta do ChatGPT
            string respostaChatGPT = await ObterRespostaChatGPT(topicoUsuario);

            Console.WriteLine("Aqui está o que encontramos sobre o seu tópico:");
            string conteudo = ExtrairConteudo(respostaChatGPT);
            Console.WriteLine(conteudo);

            int opcao = EntradaUsuario.ObterOpcao();

            if (opcao == 1)
            {
                // Lógica para gerar PDF
                Console.WriteLine("Gerando PDF...");
                await CriarPDF(topicoUsuario, conteudo);
            }
            else if (opcao == 2)
            {
                // Lógica para salvar em arquivo de texto
                Console.WriteLine("Salvando conteúdo em arquivo de texto...");
                await SalvarEmArquivoDeTexto(topicoUsuario, conteudo);
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Erro ao fazer solicitação HTTP: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ocorreu uma exceção: {ex.Message}");
        }
    }

    private static async Task CriarPDF(string topicoUsuario, string conteudo)
    {
        try
        {
            var renderer = new ChromePdfRenderer();

            // Criar o conteúdo HTML com base no input do usuário
            string conteudoHTML =
            $@"
                <link rel='stylesheet' type='text/css' href='stylePDF/style.css'>
                <h1>{topicoUsuario}</h1>
                <p>{conteudo}</p>
            ";

            var pdf = renderer.RenderHtmlAsPdf(conteudoHTML);

            // Criar uma pasta para os arquivos PDF
            string pastaPdf = "PDFs";
            if (!Directory.Exists(pastaPdf))
            {
                Directory.CreateDirectory(pastaPdf);
            }

            // Salvar o arquivo PDF com um nome baseado no tópico dentro da pasta
            string nomeArquivo = Path.Combine(pastaPdf, $"{topicoUsuario}.pdf");
            pdf.SaveAs(nomeArquivo);

            Console.WriteLine($"PDF gerado com sucesso. Caminho do arquivo: {nomeArquivo}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ocorreu uma exceção ao gerar o PDF: {ex.Message}");
        }
    }

    private static async Task SalvarEmArquivoDeTexto(string topicoUsuario, string conteudo)
    {
        try
        {
            // Criar uma pasta para os arquivos de texto
            string pastaTexto = "Textos";
            if (!Directory.Exists(pastaTexto))
            {
                Directory.CreateDirectory(pastaTexto);
            }

            // Salvar o arquivo de texto com um nome baseado no tópico dentro da pasta
            string nomeArquivo = Path.Combine(pastaTexto, $"{topicoUsuario}.txt");
            File.WriteAllText(nomeArquivo, conteudo);

            Console.WriteLine($"Conteúdo salvo em arquivo de texto com sucesso. Caminho do arquivo: {nomeArquivo}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ocorreu uma exceção ao salvar em arquivo de texto: {ex.Message}");
        }
    }

    private static async Task<string> ObterRespostaChatGPT(string topico)
    {
        try
        {
            using (var client = new HttpClient())
            {

                string apiUrl = "https://api.openai.com/v1/chat/completions";
                // Montar o corpo da solicitação
                string requestBody = JsonConvert.SerializeObject(new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a helpful assistant." },
                        new { role = "user", content = topico }
                    }
                });

                // Configurar o cabeçalho de autorização e tipo de conteúdo
                string apiKey = "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";


                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                // Aumentar o tempo limite 
                client.Timeout = TimeSpan.FromSeconds(300);

                // Fazer a solicitação POST para a API GPT-3
                HttpResponseMessage response = await client.PostAsync(apiUrl, new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json"));

                // Verificar se a solicitação foi bem-sucedida
                if (response.IsSuccessStatusCode)
                {
                    // Ler a resposta como string
                    string resposta = await response.Content.ReadAsStringAsync();
                    return resposta;
                }
                else
                {
                    Console.WriteLine($"Erro na solicitação HTTP: {response.StatusCode}");
                    return $"Erro na solicitação HTTP para {apiUrl}.";
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ocorreu uma exceção: {ex.Message}");
            return $"Ocorreu uma exceção ao obter resposta do ChatGPT para {topico}.";
        }
    }

    private static string ExtrairConteudo(string resposta)
    {
        try
        {
            var jsonResult = JsonConvert.DeserializeObject<RootObject>(resposta);
            string content = jsonResult.choices[0].message.content;
            return content;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ocorreu uma exceção ao extrair conteúdo: {ex}");
            return "Erro ao extrair conteúdo.";
        }
    }

    // Classes para desserialização do JSON
    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class Choice
    {
        public int index { get; set; }
        public Message message { get; set; }
        public string finish_reason { get; set; }
    }

    public class RootObject
    {
        public string id { get; set; }
        public string @object { get; set; }
        public int created { get; set; }
        public string model { get; set; }
        public string system_fingerprint { get; set; }
        public List<Choice> choices { get; set; }
        public Usage usage { get; set; }
    }

    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }
}







