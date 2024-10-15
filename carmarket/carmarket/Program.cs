using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

class Program
{
    static ITelegramBotClient botClient;
    static List<CarAd> carAds = new List<CarAd>();
    static Dictionary<long, CarAd> userCurrentAd = new Dictionary<long, CarAd>();
    static HashSet<long> greetedUsers = new HashSet<long>();

    static async Task Main(string[] args)
    {
        botClient = new TelegramBotClient("7676597421:AAHYzsGXgXljBYzYRs8AzdF3Reeiv2iCZqQ");

        var me = await botClient.GetMeAsync();
        Console.WriteLine($"Bot started. Username: @{me.Username}");

        using var cts = new CancellationTokenSource();

        botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            },
            cancellationToken: cts.Token
        );

        Console.WriteLine($"Listening for messages...");
        Console.ReadLine();

        cts.Cancel();
    }

    // Метод для обработки входящих сообщений и фотографий
    static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message && update.Message != null)
        {
            var chatId = update.Message.Chat.Id;

            if (!greetedUsers.Contains(chatId))
            {
                await botClient.SendTextMessageAsync(chatId, "Добро пожаловать! Спасибо, что написали. Используйте команды:\n" +
                                                             "/post - Подать автомобильное объявление\n" +
                                                             "/ads - Посмотреть все объявления\n" +
                                                             "/delete - Удалить последнее объявление (только для администратора)");
                greetedUsers.Add(chatId); 
            }


            if (update.Message.Photo != null && userCurrentAd.ContainsKey(chatId))
            {
                var photo = update.Message.Photo.Last(); 
                userCurrentAd[chatId].PhotoFileId = photo.FileId;

                await botClient.SendTextMessageAsync(chatId, "Фото автомобиля добавлено. Ваше объявление было завершено.");

                carAds.Add(userCurrentAd[chatId]);
                userCurrentAd.Remove(chatId);
            }
            else if (update.Message.Text != null)
            {
                var messageText = update.Message.Text;
                Console.WriteLine($"Received message: {messageText} from {chatId}");

                if (messageText == "/post")
                {
                    userCurrentAd[chatId] = new CarAd();
                    await botClient.SendTextMessageAsync(chatId, "Введите марку автомобиля:");
                }
                else if (userCurrentAd.ContainsKey(chatId))
                {
                    var currentAd = userCurrentAd[chatId];
                    if (string.IsNullOrEmpty(currentAd.Brand))
                    {
                        currentAd.Brand = messageText;
                        await botClient.SendTextMessageAsync(chatId, "Введите модель автомобиля:");
                    }
                    else if (string.IsNullOrEmpty(currentAd.Model))
                    {
                        currentAd.Model = messageText;
                        await botClient.SendTextMessageAsync(chatId, "Введите год выпуска автомобиля:");
                    }
                    else if (currentAd.Year == 0)
                    {
                        if (int.TryParse(messageText, out int year))
                        {
                            currentAd.Year = year;
                            await botClient.SendTextMessageAsync(chatId, "Введите цену автомобиля:");
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId, "Введите корректный год выпуска (например, 2015):");
                        }
                    }
                    else if (currentAd.Price == 0)
                    {
                        if (decimal.TryParse(messageText, out decimal price))
                        {
                            currentAd.Price = price;
                            await botClient.SendTextMessageAsync(chatId, "Введите описание автомобиля:");
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId, "Введите корректную цену (например, 15000):");
                        }
                    }
                    else if (string.IsNullOrEmpty(currentAd.Description))
                    {
                        currentAd.Description = messageText;
                        await botClient.SendTextMessageAsync(chatId, "Отправьте фото автомобиля:");
                    }
                }

                // Обработка команды /ads для просмотра всех объявлений
                else if (messageText == "/ads")
                {
                    if (carAds.Count > 0)
                    {
                        foreach (var ad in carAds)
                        {
                            var adMessage = $"Марка: {ad.Brand}\nМодель: {ad.Model}\nГод: {ad.Year}\nЦена: {ad.Price}$\nОписание: {ad.Description}";

                            if (!string.IsNullOrEmpty(ad.PhotoFileId)) 
                            {
                                try
                                {
                                    var inputFile = new InputFileId(ad.PhotoFileId);
                                    await botClient.SendPhotoAsync(chatId, inputFile, caption: adMessage);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Ошибка при отправке фото: {ex.Message}");
                                    await botClient.SendTextMessageAsync(chatId, "Ошибка при отправке фото.");
                                }
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(chatId, adMessage);
                            }
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, "Нет доступных объявлений.");
                    }
                }
                // Удаление последнего объявления (только для администратора)
                else if (messageText == "/delete")
                {
                    var isAdmin = update.Message.From.Id == 1021866063; 

                    if (isAdmin && carAds.Count > 0)
                    {
                        carAds.RemoveAt(carAds.Count - 1);
                        await botClient.SendTextMessageAsync(chatId, "Последнее автомобильное объявление было удалено.");
                    }
                    else if (!isAdmin)
                    {
                        await botClient.SendTextMessageAsync(chatId, "У вас нет прав на удаление объявлений.");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, "Нет объявлений для удаления.");
                    }
                }
            }
        }
    }

    // Обработка ошибок
    static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Error: {exception.Message}");
        return Task.CompletedTask;
    }
}

// Структура данных для автомобильного объявления
class CarAd
{
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public string PhotoFileId { get; set; } = string.Empty; 
}
