using Leitor_de_Legendas;
using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        Console.Write("Digite o caminho da pasta de legendas (.srt): ");
        string filePath = Console.ReadLine();
        string outputFilePath = Path.Combine(filePath, "resultados");

        string nomeDaTemporada = Path.GetFileName(filePath);
        try
        {
            // Obtém todos os arquivos .srt na pasta de destino
            string[] arquivosSRT = Directory.GetFiles(filePath, "*.srt");
            int contador = 1;

            // Use uma expressão regular para encontrar todas as palavras nas legendas
            Regex regex = new Regex(@"\b[\p{L}'\p{M}]+\b");

            // Pegando o nome sem a extensao
            List<string> nomeDoEpp = arquivosSRT.Select(a => Path.GetFileNameWithoutExtension(a)).ToList();

            // Ler as legendas de cada Epp
            // Remove as tags HTML usando uma expressão regular
            // Lista os EPP separadamente
            List<string> conteudoDoArquivo = arquivosSRT.Select(a => File.ReadAllText(a)).ToList();
            conteudoDoArquivo = conteudoDoArquivo.Select(c => Regex.Replace(c, "<.*?>", "")).ToList();

            // Remove numeros e simbolos a partir do Regex criado
            List<MatchCollection> matches = conteudoDoArquivo.Select(m => regex.Matches(m)).ToList();

            Episodio(outputFilePath, nomeDoEpp, matches);
            Temporada(outputFilePath, nomeDaTemporada, matches);

        }
        catch (Exception ex)
        {
            Console.WriteLine("Ocorreu um erro: " + ex.Message);
        }
    }
    private static void Episodio(string outputFolderPath, List<string> nomeDoEpp, List<MatchCollection> matches)
    {

        // Usa LINQ para contar a frequência das palavras
        List<List<PalavraFrequencia>> frequenciaPorEpisodio = matches
             .Select(matchesPorEpisodio =>
             {
                 return matchesPorEpisodio
                     .Cast<Match>()
                     .GroupBy(match => match.Value, StringComparer.OrdinalIgnoreCase)
                     .Select(group => new PalavraFrequencia
                     {
                         Palavra = group.Key,
                         Frequencia = group.Count()
                     })
                     .OrderByDescending(item => item.Frequencia)
                     .ToList();
             })
             .ToList();

        // Verifica se a pasta não existe antes de criá-la
        if (!Directory.Exists(outputFolderPath))
        {
            // Cria a pasta
            Directory.CreateDirectory(outputFolderPath);
        }

        // Escreve o JSON em arquivos separados para cada episódio
        EscreverArquivosJson(nomeDoEpp, frequenciaPorEpisodio, outputFolderPath);
    }

    private static void Temporada(string outputFilePath, string nomeDaTemporada, List<MatchCollection> matches)
    {
        // SelectMany para "achatar" todas as MatchCollection em uma única sequência
        IEnumerable<Match> todasAsCorrespondencias = matches.SelectMany(mc => mc.Cast<Match>());

        // LINQ para contar a frequência das palavras na sequência única
        List<PalavraFrequencia> frequenciaPalavras = todasAsCorrespondencias
            .GroupBy(match => match.Value, StringComparer.OrdinalIgnoreCase)
            .Select(group => new PalavraFrequencia
            {
                Palavra = group.Key,
                Frequencia = group.Count()
            })
            .OrderByDescending(item => item.Frequencia)
            .ToList();

        // Cria o arquivo JSON a partir do objeto frequenciaPalavras
        string json = JsonSerializer.Serialize(frequenciaPalavras, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Escreve o JSON no arquivo de saída
        File.WriteAllText($"{outputFilePath}\\temporada-{nomeDaTemporada}.json", json);
    }

    private static void EscreverArquivosJson(List<string> nomesDoEpisodio, List<List<PalavraFrequencia>> frequenciaPorEpisodio, string outputFolderPath, int index = 0)
    {
        // Verifica se ainda há episódios a processar com base no índice
        if (index < nomesDoEpisodio.Count)
        {
            // Obtém o nome do episódio atual e sua frequência de palavras correspondente
            string nomeDoEpisodio = nomesDoEpisodio[index];
            List<PalavraFrequencia> frequencia = frequenciaPorEpisodio[index];

            // Serializa a lista de frequência de palavras em formato JSON com formatação identada
            string json = JsonSerializer.Serialize(frequencia, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Define o caminho completo para o arquivo JSON de saída, incluindo o nome do episódio
            string filePath = Path.Combine(outputFolderPath, $"episodio-{nomeDoEpisodio}.json");
            File.WriteAllText(filePath, json);

            // Chama a função recursivamente para o próximo episódio
            EscreverArquivosJson(nomesDoEpisodio, frequenciaPorEpisodio, outputFolderPath, index + 1);
        }
    }
}