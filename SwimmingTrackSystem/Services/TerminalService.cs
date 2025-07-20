using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SwimmingTrackSystem.Services;

public class TerminalService : IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public TerminalService(string username, string password)
    {
        AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);
        var credentials = new NetworkCredential(username, password);
        var handler = new HttpClientHandler { Credentials = credentials };
        _httpClient = new HttpClient(handler);
    }

    public async Task<bool> DeleteUsersAsync(UserInfoDeleteRequest request, string ipAddress)
    {
        var url = $"http://{ipAddress}/ISAPI/AccessControl/UserInfo/Delete?format=json";
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync(url, content);

        var result = await response.Content.ReadFromJsonAsync<UserInfoResponse>();

        if (result is null)
            return false;

        return result.statusCode == 1;
    }

    public async Task<bool> AddUserInfoAsync(Models.Transaction transaction, string ipAddress)
    {
        var requestStr = $@"
            {{
                ""UserInfo"": {{
                    ""employeeNo"": ""{transaction.Id.ToString()}"",
                    ""name"": ""Гость"",
                    ""userType"": ""normal"",
                    ""localUIRight"": false,
                    ""Valid"": {{
                        ""enable"": true,
                        ""beginTime"": ""{transaction.CreateDate.ToString(Constants.DateTerminalFormat)}"",
                        ""endTime"": ""{transaction.ExpireDate!.Value.ToString(Constants.DateTerminalFormat)}"",
                        ""timeType"": ""local""
                    }},
                    ""doorRight"": ""1"",
                    ""RightPlan"": [
                        {{
                            ""doorNo"": 1,
                            ""planTemplateNo"": ""1""
                        }}
                    ],
                    ""addUser"": true
                }}
            }}
            ";
        
        var url = $"http://{ipAddress}/ISAPI/AccessControl/UserInfo/SetUp?format=json";
        var content = new StringContent(requestStr, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync(url, content);

        var result = await response.Content.ReadFromJsonAsync<UserInfoResponse>();

        if (result is null)
            return false;

        return result.statusCode == 1;
    }

    public async Task<bool> AddCardInfoAsync(Models.Transaction transaction, string ipAddress, string guid)
    {
        var requestStr = $@"
        {{
            ""CardInfo"": {{
                ""employeeNo"": ""{transaction.Id.ToString()}"",
                ""cardNo"": ""{guid}"",
                ""cardType"": ""normalCard"",
                ""addCard"": true
            }}
        }}
        ";
        
        var url = $"http://{ipAddress}/ISAPI/AccessControl/CardInfo/SetUp?format=json";
        var content = new StringContent(requestStr, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync(url, content);

        var result = await response.Content.ReadFromJsonAsync<UserInfoResponse>();

        if (result is null)
            return false;

        return result.statusCode == 1;
    }
    
    public async Task<List<AcsEventInfo>> GetFilteredUserHistoriesAsync(string ipAddress, string startDate)
    {
        var result = new List<AcsEventInfo>();
        var hasMoreData = true;
        var position = 0;
        var url = $"http://{ipAddress}/ISAPI/AccessControl/AcsEvent?format=json";

        while (hasMoreData)
        {
            var requestStr = $@"
            {{
                ""AcsEventCond"": {{
                    ""searchID"": ""1"",
                    ""searchResultPosition"": {position},
                    ""maxResults"": 30,
                    ""major"": 5,
                    ""minor"": 0,
                    ""timeReverseOrder"": true,
                    ""startTime"": ""{startDate}T00:00:00""
                }}    
            }}";
            
            var content = new StringContent(requestStr, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            
            var resultJson = await response.Content.ReadAsStringAsync();
            var responseWrapper = JsonSerializer.Deserialize<AscEventReponseWrapper>(resultJson);
            
            if (responseWrapper?.AcsEvent.InfoList != null)
            {
                var filteredInfos = responseWrapper.AcsEvent.InfoList.Where(info => info.currentVerifyMode == "card").ToList();
                result.AddRange(filteredInfos);

                hasMoreData = responseWrapper.AcsEvent.responseStatusStrg == "MORE";
                position += responseWrapper.AcsEvent.numOfMatches;
            }
            else
            {
                hasMoreData = false;
            }
        }

        return result;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _httpClient.Dispose();
        }

        _disposed = true;
    }
}

public class UserInfoResponse
{
    public int statusCode { get; set; }
    public string statusString { get; set; }
}

public class AscEventReponseWrapper
{
    public AcsEventResponse AcsEvent { get; set; }
}

public class AcsEventResponse
{
    public string searchID { get; set; }
    public int totalMatches { get; set; }
    public string responseStatusStrg { get; set; }
    public int numOfMatches { get; set; }
    public List<AcsEventInfo> InfoList { get; set; }
}

public class AcsEventInfo
{
    public string currentVerifyMode { get; set; }
    public string time { get; set; }
    public string name { get; set; }
    public string pictureURL { get; set; }
    public string employeeNoString { get; set; }
    public string userType { get; set; }
}

public class UserInfoSearchRequest
{
    public UserInfoSearchCond UserInfoSearchCond { get; set; }
}

public class UserInfoSearchCond
{
    public string SearchID { get; set; }
    public int SearchResultPosition { get; set; }
    public int MaxResults { get; set; }
}

public class UserInfoDeleteRequest
{
    public UserInfoDelCond UserInfoDelCond { get; set; }
}

public class UserInfoDelCond
{
    public EmployeeNoList[] EmployeeNoList { get; set; }
}

public class EmployeeNoList
{
    public string EmployeeNo { get; set; }
}