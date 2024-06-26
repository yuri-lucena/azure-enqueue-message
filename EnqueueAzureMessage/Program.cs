using Azure.Storage.Queues;

namespace EnqueueAzureMessage;

public class Program
{
    private const string FilePath = "azure_storage_info.dat";

    private static readonly List<(string storageAccount, string nickname)> storageAccounts = new();

    static async Task Main(string[] args)
    {
        LoadSavedStorageAccounts();

        int choice;
        do
        {
            Console.WriteLine("\nEscolha uma opção:");
            Console.WriteLine("[1] - Enviar mensagem para a fila");
            Console.WriteLine("[2] - Ver mensagens na fila");
            Console.WriteLine("[99] - Sair");
            Console.Write("Opção: ");

            if (!int.TryParse(Console.ReadLine(), out choice))
            {
                Console.WriteLine("Opção inválida. Tente novamente.");
                continue;
            }

            switch (choice)
            {
                case 1:
                    await SendMessageToQueue();
                    break;
                case 2:
                    await ViewMessagesInQueue();
                    break;
                case 99:
                    Console.WriteLine("Saindo do programa.");
                    break;
                default:
                    Console.WriteLine("Opção inválida. Tente novamente.");
                    break;
            }
        } while (choice != 3);
    }

    static async Task SendMessageToQueue()
    {
        var storageAccount = GetStorageAccountFromUser();
        Console.Write("Digite o nome da fila: ");
        var queueName = Console.ReadLine();

        var queueClient = new QueueClient(storageAccount, queueName);

        Console.Write("Digite a mensagem a ser enviada para a fila: ");
        var message = Console.ReadLine();

        await queueClient.SendMessageAsync(Base64Encode(message));
        Console.WriteLine(
            $"\nMensagem '{message}' enviada com sucesso para a fila '{queueName}'.");
    }

    static async Task ViewMessagesInQueue()
    {
        var storageAccount = GetStorageAccountFromUser();
        Console.Write("Digite o nome da fila: ");
        var queueName = Console.ReadLine();

        var queueClient = new QueueClient(storageAccount, queueName);
        var messages = (await queueClient.ReceiveMessagesAsync(10)).Value;

        if (messages.Length == 0)
        {
            Console.WriteLine("Não há mensagens na fila\n");
            return;
        }

        foreach (var message in messages)
            Console.WriteLine($"Id: {message.MessageId}, Conteúdo: {message.MessageText}");

    }

    static string GetStorageAccountFromUser()
    {
        if (storageAccounts.Count == 0)
        {
            Console.WriteLine("Nenhuma conta de armazenamento salva. Por favor, adicione uma.");
            AddStorageAccount();
        }

        if (storageAccounts.Count == 1)
        {
            Console.WriteLine($"Usando a única conta de armazenamento disponível: {storageAccounts[0].nickname}");
            return storageAccounts[0].storageAccount;
        }
        else
        {
            Console.WriteLine("Escolha uma das contas de armazenamento disponíveis:");
            for (var i = 0; i < storageAccounts.Count; i++)
            {
                Console.WriteLine($"{i + 1} - {storageAccounts[i].nickname}");
            }

            int choice;
            do
            {
                Console.Write("Escolha: ");
            } while (!int.TryParse(Console.ReadLine(), out choice) || choice < 1 || choice > storageAccounts.Count);

            return storageAccounts[choice - 1].storageAccount;
        }
    }

    static void AddStorageAccount()
    {
        Console.Write("Digite o nome da conta de armazenamento: ");
        var storageAccount = Console.ReadLine();
        Console.Write("Digite o apelido (nome) para essa conta: ");
        var nickname = Console.ReadLine();

        storageAccounts.Add((storageAccount, nickname));
        SaveStorageAccounts();
    }

    static async Task SaveStorageAccounts()
    {
        using (var ms = new MemoryStream())
        {
            using (var sw = new StreamWriter(ms))
            {
                foreach (var account in storageAccounts)
                {
                    sw.WriteLine($"{account.storageAccount},{account.nickname}");
                }
            }

            await File.WriteAllBytesAsync(FilePath, ms.ToArray());
        }
    }

    static void LoadSavedStorageAccounts()
    {
        if (File.Exists(FilePath))
        {
            using (var ms = new MemoryStream(File.ReadAllBytes(FilePath)))
            {
                using (var sr = new StreamReader(ms))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] parts = line.Split(',');
                        storageAccounts.Add((parts[0], parts[1]));
                    }
                }
            }
        }
    }

    private static string Base64Encode(string plainText)
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(plainTextBytes);
    }
}