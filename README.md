
## Лабораторна робота №3. Background Services з Azure Queue та Blob Storage
***Удосконалення лабораторної роботи №1***

В ході виконання роботи було розроблено систему, яка демонструє взаємодію між Web API, Azure Blob Storage та Azure Queue Storage. Реалізовано автоматичне стиснення зображень за допомогою фонового сервісу BackgroundService, що імітує поведінку веб-завдань WebJobs.

**Структура:**
- Використано .NET 8 SDK, Azurite для локальної імітації Azure
- Пакети Azure.Storage.Blobs та Azure.Storage.Queues для взаємодії зі сховищами
- Пакет SixLabors.ImageSharp для обробки та зміни розміру зображень
- Фоновий сервіс (ImageProcessingBackgroundService) паралельної обробки

**Життєвий цикл:**
- Завантажується зображення у контейнер big-images.
- Назва файлу відправляється у чергу повідомлень new-images
- Фоновий сервіс отримує повідомлення, стискає оригінал та зберігає у small-images
- Після успішної обробки повідомлення видаляється з черги

### Реалізація

[Services/**AzureBlobService.cs**](Services/AzureBlobService.cs)\
У сервісі реалізовано завантаження файлу в контейнер big-images та подальшу відправку імені файлу (Base64) до черги new-images.
```csharp
public async Task UploadAsync(IFormFile file)
{
    // Завантаження оригінального файлу у big-images
    var containerClient = _blobServiceClient.GetBlobContainerClient("big-images");
    await containerClient.CreateIfNotExistsAsync();
    var blobClient = containerClient.GetBlobClient(file.FileName);
    using var stream = file.OpenReadStream();
    await blobClient.UploadAsync(stream, true);

    // Відправка повідомлення з назвою файлу у чергу new-images
    var queueClient = _queueServiceClient.GetQueueClient("new-images");
    await queueClient.CreateIfNotExistsAsync();
    var base64Message = Convert.ToBase64String(Encoding.UTF8.GetBytes(file.FileName));
    await queueClient.SendMessageAsync(base64Message);
}

```

[BackgroundServices/**ImageProcessingBackgroundService.cs**](BackgroundServices/ImageProcessingBackgroundService.cs)\
Головний компонент, який є реалізацією інтерфейсу IHostedService через BackgroundService. Тобто код працюватиме паралельно до обробки запитів.\
Він зчитує повідомлення, обробляє зображення за допомогою бібліотеки ImageSharp та зберігає результат.
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        var queueClient = _queueServiceClient.GetQueueClient("new-images");
        var response = await queueClient.ReceiveMessagesAsync(maxMessages: 1, cancellationToken: stoppingToken);
        var message = response.Value.FirstOrDefault();

        if (message != null)
        {
            // Декодування імені файлу
            var fileName = Encoding.UTF8.GetString(Convert.FromBase64String(message.Body.ToString()));
            
            var bigContainer = _blobServiceClient.GetBlobContainerClient("big-images");
            var smallContainer = _blobServiceClient.GetBlobContainerClient("small-images");
            await smallContainer.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

            // Завантаження оригінального зображення
            var bigBlob = bigContainer.GetBlobClient(fileName);
            using var memoryStream = new MemoryStream();
            await bigBlob.DownloadToAsync(memoryStream, cancellationToken: stoppingToken);
            memoryStream.Position = 0;

            // Зміна розміру за допомогою SixLabors.ImageSharp
            using var image = await Image.LoadAsync(memoryStream, stoppingToken);
            image.Mutate(x => x.Resize(200, 0)); // Ширина 200px, висота - авто

            using var outStream = new MemoryStream();
            await image.SaveAsJpegAsync(outStream, cancellationToken: stoppingToken);
            outStream.Position = 0;

            // Збереження у small-images
            var smallBlob = smallContainer.GetBlobClient(fileName);
            await smallBlob.UploadAsync(outStream, overwrite: true, cancellationToken: stoppingToken);

            // Видалення обробленого повідомлення з черги
            await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
        }
        else
        {
            await Task.Delay(2000, stoppingToken);
        }
    }
}
```

[**Program.cs**](Program.cs)\
Щоб сервіс працював паралельно з Web API, його зареєстровано як Hosted Service.
```csharp
builder.Services.AddHostedService<ImageProcessingBackgroundService>();
```
### Демонстрація роботи
Завантаження зображення через POST-запит:\
<img width="713" height="800" alt="POST /upload-images" src="https://github.com/user-attachments/assets/0ba293ed-932b-43c8-8716-09c781c42b6d" />

Вигляд контейнерів big-images та small-images в Azurite Storage:\
<img width="286" height="359" alt="Azure Storage view" src="https://github.com/user-attachments/assets/f06d706d-c69e-4319-a14f-8d41d38205f6" />

Стиснене зображення (результат роботи ImageSharp):\
<img width="333" height="268" alt="Result image" src="https://github.com/user-attachments/assets/fe0124e5-6abe-497c-a88e-5b872be1b36c" />
