using System.Text.Json;
using System.Text;

namespace PetStore;

public class ApiResponse
{
    public int Code { get; set; }
    public string? Type { get; set; }
    public string? Message { get; set; }
}

public class Category
{
    public long Id { get; set; }
    public string? Name { get; set; }
}

public class Pet
{
    public long Id { get; set; }
    public Category? Category { get; set; }
    public string Name { get; set; }
    public List<string> PhotoUrls { get; set; }
    public List<Tag>? Tags { get; set; }
    public string? Status { get; set; }
}

public class Tag
{
    public long Id { get; set; }
    public string? Name { get; set; }
}

public class Order
{
    public long Id { get; set; }
    public long PetId { get; set; }
    public int Quantity { get; set; }
    public DateTime? ShipDate { get; set; }
    public string? Status { get; set; }
    public bool Complete { get; set; }
}

public class User
{
    public long Id { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Phone { get; set; }
    public int UserStatus { get; set; }
}

public class PetStoreClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public PetStoreClient(HttpClient httpClient, string baseUrl = "")
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl;
    }

    public async Task<ApiResponse?> UploadFile(int petId, object body)
    {
        var path = $"/pet/{petId}/uploadImage";
        return await SendRequestAsync<ApiResponse>(path, HttpMethod.Post, body);
    }

    public async Task UpdatePet(Pet body)
    {
        var path = $"/pet";
        await SendRequestAsync(path, HttpMethod.Put, body);
    }

    public async Task AddPet(Pet body)
    {
        var path = $"/pet";
        await SendRequestAsync(path, HttpMethod.Post, body);
    }

    public async Task<List<Pet>?> FindPetsByStatus(object[]? status = null)
    {
        var queryParams = new List<string>();
        if (status != null)
            queryParams.Add($"status={status}");
        var path = "/pet/findByStatus" + (queryParams.Any() ? "?" + string.Join("&", queryParams) : "");
        return await SendRequestAsync<List<Pet>>(path, HttpMethod.Get);
    }

    public async Task<List<Pet>?> FindPetsByTags(object[]? tags = null)
    {
        var queryParams = new List<string>();
        if (tags != null)
            queryParams.Add($"tags={tags}");
        var path = "/pet/findByTags" + (queryParams.Any() ? "?" + string.Join("&", queryParams) : "");
        return await SendRequestAsync<List<Pet>>(path, HttpMethod.Get);
    }

    public async Task<Pet?> GetPetById(int petId)
    {
        var path = $"/pet/{petId}";
        return await SendRequestAsync<Pet>(path, HttpMethod.Get);
    }

    public async Task UpdatePetWithForm(int petId, object body)
    {
        var path = $"/pet/{petId}";
        await SendRequestAsync(path, HttpMethod.Post, body);
    }

    public async Task DeletePet(int petId)
    {
        var path = $"/pet/{petId}";
        await SendRequestAsync(path, HttpMethod.Delete);
    }

    public async Task<object?> GetInventory()
    {
        var path = $"/store/inventory";
        return await SendRequestAsync<object>(path, HttpMethod.Get);
    }

    public async Task<Order?> PlaceOrder(Order body)
    {
        var path = $"/store/order";
        return await SendRequestAsync<Order>(path, HttpMethod.Post, body);
    }

    public async Task<Order?> GetOrderById(int orderId)
    {
        var path = $"/store/order/{orderId}";
        return await SendRequestAsync<Order>(path, HttpMethod.Get);
    }

    public async Task DeleteOrder(int orderId)
    {
        var path = $"/store/order/{orderId}";
        await SendRequestAsync(path, HttpMethod.Delete);
    }

    public async Task CreateUsersWithListInput(List<User> body)
    {
        var path = $"/user/createWithList";
        await SendRequestAsync(path, HttpMethod.Post, body);
    }

    public async Task<User?> GetUserByName(string username)
    {
        var path = $"/user/{username}";
        return await SendRequestAsync<User>(path, HttpMethod.Get);
    }

    public async Task UpdateUser(string username, User body)
    {
        var path = $"/user/{username}";
        await SendRequestAsync(path, HttpMethod.Put, body);
    }

    public async Task DeleteUser(string username)
    {
        var path = $"/user/{username}";
        await SendRequestAsync(path, HttpMethod.Delete);
    }

    public async Task<string?> LoginUser(string? username = null, string? password = null)
    {
        var queryParams = new List<string>();
        if (username != null)
            queryParams.Add($"username={username}");
        if (password != null)
            queryParams.Add($"password={password}");
        var path = "/user/login" + (queryParams.Any() ? "?" + string.Join("&", queryParams) : "");
        return await SendRequestAsync<string>(path, HttpMethod.Get);
    }

    public async Task LogoutUser()
    {
        var path = $"/user/logout";
        await SendRequestAsync(path, HttpMethod.Get);
    }

    public async Task CreateUsersWithArrayInput(List<User> body)
    {
        var path = $"/user/createWithArray";
        await SendRequestAsync(path, HttpMethod.Post, body);
    }

    public async Task CreateUser(User body)
    {
        var path = $"/user";
        await SendRequestAsync(path, HttpMethod.Post, body);
    }

    private async Task<T?> SendRequestAsync<T>(string path, HttpMethod method, object? body = null)
    {
        var request = new HttpRequestMessage(method, _baseUrl + path);

        if (body != null)
        {
            var json = JsonSerializer.Serialize(body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        
        if (string.IsNullOrEmpty(responseContent))
            return default;

        return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    private async Task SendRequestAsync(string path, HttpMethod method, object? body = null)
    {
        var request = new HttpRequestMessage(method, _baseUrl + path);

        if (body != null)
        {
            var json = JsonSerializer.Serialize(body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
