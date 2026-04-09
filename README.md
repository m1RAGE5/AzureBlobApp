
## Лабораторна робота №1. Azure Blob Storage

В ході виконання роботи було розроблено систему для управління двійковими об'єктами (файлами) та контейнерами з використанням Azure Blob Storage, ASP.NET Core та імплементацією Swagger UI для зручного тестування API.

**Структура:**
- Використано .NET 8 SDK, Azurite для локальної імітації Azure
- Пакет Azure.Storage.Blobs для взаємодії з API сховища
- Створено Web API додаток (AzureBlobApp) для маніпулювання файлами

**Функціональні можливості:**
- Створення та видалення контейнерів
- Завантаження файлів Blob у контейнер
- Перегляд списку файлів у вказаному контейнері
- Видалення окремих об'єктів зі сховища

### Реалізація

[Services/**AzureBlobService.cs**](Services/AzureBlobService.cs)\
Основний сервіс, що інкапсулює логіку роботи з SDK. Використовує BlobServiceClient для підключення до Azurite.
```csharp
public async Task UploadAsync(IFormFile file, string containerName)
{
    // Отримання клієнта контейнера та створення його за потреби
    var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
    await containerClient.CreateIfNotExistsAsync();

    // Завантаження файлу через потік (Stream)
    var blobClient = containerClient.GetBlobClient(file.FileName);
    using var stream = file.OpenReadStream();
    await blobClient.UploadAsync(stream, true);
}

public async Task<List<string>> ListAsync(string containerName)
{
    var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
    var blobs = new List<string>();

    // Асинхронна ітерація по списку блобів
    await foreach (var blobItem in containerClient.GetBlobsAsync())
    {
        blobs.Add(blobItem.Name);
    }
    return blobs;
}
```

[Controllers/**BlobController.cs**](Controllers/BlobController.cs)\
Контролер, що надає REST-інтерфейс через Swagger. Кожен метод відповідає за конкретну операцію зі сховищем.
```csharp
// POST api/blob/upload - Завантаження файлу
[HttpPost("upload")]
public async Task<IActionResult> Upload(IFormFile file, string containerName)
{
    await _blobService.UploadAsync(file, containerName);
    return Ok("Файл успішно завантажено.");
}

// GET api/blob/list/{containerName} - Отримання списку файлів
[HttpGet("list/{containerName}")]
public async Task<IActionResult> List(string containerName)
{
    var result = await _blobService.ListAsync(containerName);
    return Ok(result);
}

// DELETE api/blob/delete - Видалення файлу
[HttpDelete("delete")]
public async Task<IActionResult> Delete(string containerName, string blobName)
{
    await _blobService.DeleteAsync(containerName, blobName);
    return Ok("Файл видалено.");
}
```

[Program.cs](Program.cs)\
У файлі налаштовується Dependency Injection. AzureBlobService реєструється для використання в контролерах, а рядок підключення до Azurite зчитується з конфігурації. Також підключається Swagger для візуалізації API.

### Демонстрація роботи
Azurite: Start:\
<img width="394" height="29" alt="Azurite Blob Service" src="https://github.com/user-attachments/assets/1b519bb1-d05c-4a19-a09e-97208687436b" />

Запуск через dotnet run:\
<img width="709" height="234" alt="dotnet run" src="https://github.com/user-attachments/assets/8cba1f6a-7692-4860-a7ad-46274c40e3f2" />

Створення контейнера (POST container):\
<img width="557" height="564" alt="POST container" src="https://github.com/user-attachments/assets/dff0b1cf-1649-4f7d-b3cb-136e2540974b" />

Azurite Storage сховиже cars:\
<img width="289" height="195" alt="Azurite Storage cars" src="https://github.com/user-attachments/assets/9ccbb75f-dfaf-440a-ac96-03e50f171309" />

Видалення сховища (DELETE /delete):\
<img width="548" height="560" alt="DELETE container" src="https://github.com/user-attachments/assets/90b2e76f-7213-45ab-9d44-d7536d8d7a84" />

Azurite Storage після видалення сховища cars:\
<img width="285" height="165" alt="Azurite Storage empty" src="https://github.com/user-attachments/assets/1ddf1fea-38d1-4f4a-a1c2-fe96f3327872" />

Завантаження файлу (POST /upload):\
<img width="550" height="789" alt="POST upload" src="https://github.com/user-attachments/assets/c29a2d53-6684-4081-b832-cc754438e468" />

Перегляд списку завантажених файлів (GET /list):\
<img width="549" height="566" alt="POST list" src="https://github.com/user-attachments/assets/bb48587c-f979-4474-8aaa-0117a5caf163" />

Azurite Storage перегляд списку завантажених файлів:\
<img width="285" height="192" alt="Azurite Storage image" src="https://github.com/user-attachments/assets/d1ed7359-4d7d-4396-b9a2-e5d635c9affe" />

Видалення об'єкта зі сховища (DELETE /delete blob):\
<img width="550" height="639" alt="DELETE Blob" src="https://github.com/user-attachments/assets/d7219371-9a85-412e-83f4-61107d02b11a" />

Azurite Storage після видалення об'єкта зі сховища cars:\
<img width="286" height="164" alt="Azurite Storage no image" src="https://github.com/user-attachments/assets/a3f27ebf-89f1-4b59-b896-2752d8a1e471" />
