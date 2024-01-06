using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using System.Threading.Channels;
using MySql.Data.MySqlClient;


class Program
{
    private static ITelegramBotClient _botClient;

    private static ReceiverOptions _receiverOptions;

    public static string messageText = "";

    public static int counter = 0;
    private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        MySqlConnectionStringBuilder stringBuilder = new MySqlConnectionStringBuilder();

        stringBuilder.Server = "127.0.0.1";
        stringBuilder.Port = 3306;
        stringBuilder.UserID = "root";
        stringBuilder.Password = "MaxiMUM02040608+";
        stringBuilder.Database = "tgbot";

        string connectionString = stringBuilder.ToString();

        MySqlConnection connection = new MySqlConnection(connectionString);

        await connection.OpenAsync();
        {
            try
            {
                switch (update.Type)
                {
                    case UpdateType.Message:
                        {
                            var message = update.Message;

                            messageText = message.Text;

                            var user = message.From;

                            Console.WriteLine($"{user.FirstName} ({user.Id}) написал сообщение: {message.Text}");

                            var chat = message.Chat;

                            string sql = "INSERT INTO message (ID, messageText) VALUES (@value1, @value2)";

                            using (MySqlCommand command1 = new MySqlCommand(sql, connection))
                            {
                                command1.Parameters.AddWithValue("@value1", 0);
                                command1.Parameters.AddWithValue("@value2", messageText);

                                int rowsAffected = command1.ExecuteNonQuery();

                                Console.WriteLine("Количество добавленных строк: " + rowsAffected);
                                counter++;
                            }

                            string query = $"SELECT * FROM `message` WHERE `ID`={counter}";
                            MySqlCommand command2 = new MySqlCommand(query, connection);
                            MySqlDataReader reader = command2.ExecuteReader();

                            while (reader.Read())
                            {
                                int id = reader.GetInt32("ID");
                                string messageText = reader.GetString("messageText");

                                await botClient.SendTextMessageAsync("@mmmmmsdwr", messageText);
                                Console.WriteLine($"ID: {id}, Message: {messageText}");
                            }

                            reader.Close();

                            return;
                        }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

    private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
    {
        var ErrorMessage = error switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => error.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }

    static async Task Main()
    {
        _botClient = new TelegramBotClient("6925345713:AAHZob8lnqDmyXnYbqW7qmCOdT2EMlTA7BA");
        _receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[]
            {
                UpdateType.Message,
            },
            ThrowPendingUpdates = true,
        };

        using var cts = new CancellationTokenSource();

        _botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token);

        var me = await _botClient.GetMeAsync();
        Console.WriteLine($"{me.FirstName} запущен!");

        await Task.Delay(-1);
    }
}