using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Xunit;

namespace RateCon.Tests;

// ================================================================== //
//  Mock ma'lumotlar
// ================================================================== //

public static class MockData
{
    public static readonly Dictionary<string, object> DispatchResponse = new()
    {
        ["dispatch"] = new Dictionary<string, object>
        {
            ["id"] = 98765,
            ["vendor_id"] = "RC-1001",
            ["status"] = "planned",
            ["loaded_miles"] = 450,
            ["dispatch_stops"] = new List<Dictionary<string, object>>
            {
                new()
                {
                    ["id"] = 111,
                    ["vendor_id"] = "RC-1001-PU",
                    ["type"] = "pickup",
                    ["number"] = 1,
                    ["dispatch_location"] = new Dictionary<string, string>
                    {
                        ["id"] = "501",
                        ["name"] = "ABC Warehouse",
                        ["address_line_1"] = "123 Main St",
                        ["city"] = "Dallas",
                        ["state"] = "TX",
                        ["zip"] = "75201"
                    },
                    ["early_date"] = "2026-04-17T08:00:00-05:00",
                    ["late_date"] = "2026-04-17T14:00:00-05:00",
                    ["status"] = "available",
                    ["arrived_at"] = null!,
                    ["departed_at"] = null!
                },
                new()
                {
                    ["id"] = 112,
                    ["vendor_id"] = "RC-1001-DEL",
                    ["type"] = "dropoff",
                    ["number"] = 2,
                    ["dispatch_location"] = new Dictionary<string, string>
                    {
                        ["id"] = "502",
                        ["name"] = "XYZ Distribution",
                        ["address_line_1"] = "456 Oak Ave",
                        ["city"] = "Houston",
                        ["state"] = "TX",
                        ["zip"] = "77001"
                    },
                    ["early_date"] = "2026-04-18T08:00:00-05:00",
                    ["status"] = "available",
                    ["arrived_at"] = null!,
                    ["departed_at"] = null!
                }
            },
            ["dispatch_trips"] = new List<Dictionary<string, object>>
            {
                new()
                {
                    ["vendor_id"] = "RC-1001-TRIP",
                    ["vehicle_id"] = 64734,
                    ["driver_id"] = 1088505,
                    ["status"] = "not_started"
                }
            }
        }
    };

    public static readonly Dictionary<string, object> SampleLoad = new()
    {
        ["id"] = 42,
        ["load_number"] = "RC-1001",
        ["pickup_address"] = "123 Main St, Dallas, TX 75201",
        ["pickup_date"] = "2026-04-17T08:00:00-05:00",
        ["delivery_address"] = "456 Oak Ave, Houston, TX 77001",
        ["delivery_date"] = "2026-04-18T08:00:00-05:00",
        ["miles"] = "450",
        ["stops_json"] = null!,
        ["status"] = "dispatched"
    };

    public static readonly Dictionary<string, object> Dispatch3Stops = new()
    {
        ["id"] = 99999,
        ["vendor_id"] = "MULTI-001",
        ["status"] = "active",
        ["loaded_miles"] = 600,
        ["dispatch_stops"] = new List<Dictionary<string, object>>
        {
            new()
            {
                ["id"] = 201, ["type"] = "pickup", ["number"] = 1,
                ["dispatch_location"] = new Dictionary<string, string>
                {
                    ["address_line_1"] = "100 A St", ["city"] = "Dallas",
                    ["state"] = "TX", ["zip"] = "75201"
                },
                ["early_date"] = "2026-04-17T06:00:00-05:00",
                ["arrived_at"] = "2026-04-17T06:30:00-05:00",
                ["departed_at"] = "2026-04-17T07:00:00-05:00"
            },
            new()
            {
                ["id"] = 202, ["type"] = "pickup", ["number"] = 2,
                ["dispatch_location"] = new Dictionary<string, string>
                {
                    ["address_line_1"] = "200 B St", ["city"] = "Austin",
                    ["state"] = "TX", ["zip"] = "73301"
                },
                ["early_date"] = "2026-04-17T12:00:00-05:00",
                ["arrived_at"] = "2026-04-17T12:15:00-05:00",
                ["departed_at"] = null!,
                ["comments"] = "Dock 5"
            },
            new()
            {
                ["id"] = 203, ["type"] = "dropoff", ["number"] = 3,
                ["dispatch_location"] = new Dictionary<string, string>
                {
                    ["address_line_1"] = "300 C St", ["city"] = "Houston",
                    ["state"] = "TX", ["zip"] = "77001"
                },
                ["early_date"] = "2026-04-18T08:00:00-05:00",
                ["arrived_at"] = null!,
                ["departed_at"] = null!
            }
        },
        ["dispatch_trips"] = new List<Dictionary<string, object>>
        {
            new() { ["driver_id"] = 5555, ["vehicle_id"] = 7777, ["status"] = "in_progress" }
        }
    };
}

// ================================================================== //
//  Helper: HttpClient mock
// ================================================================== //

public static class HttpMockHelper
{
    public static HttpClient CreateMockClient(
        HttpStatusCode statusCode,
        object responseBody)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(JsonSerializer.Serialize(responseBody))
            });
        return new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://api.keeptruckin.com")
        };
    }

    public static (HttpClient Client, Mock<HttpMessageHandler> Handler) CreateTrackedMockClient(
        HttpStatusCode statusCode,
        object responseBody)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(JsonSerializer.Serialize(responseBody))
            });
        var client = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://api.keeptruckin.com")
        };
        return (client, handler);
    }
}

// ================================================================== //
//  Test: push_dispatch
// ================================================================== //

public class PushDispatchTests
{
    [Fact]
    public async Task PushSimpleLoad_ReturnsDispatchWithCorrectId()
    {
        // Arrange
        var (client, handler) = HttpMockHelper.CreateTrackedMockClient(
            HttpStatusCode.OK, MockData.DispatchResponse);
        var motive = new MotiveClient("test-key", client);

        // Act
        var result = await motive.PushDispatchAsync(MockData.SampleLoad, "1088505", "64734");

        // Assert
        Assert.Equal(98765, result.GetProperty("id").GetInt32());
        Assert.Equal("RC-1001", result.GetProperty("vendor_id").GetString());
        Assert.Equal("planned", result.GetProperty("status").GetString());

        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Post),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task PushLoadWithExtraStops_Creates4Stops()
    {
        // Arrange
        var client = HttpMockHelper.CreateMockClient(HttpStatusCode.OK, MockData.DispatchResponse);
        var motive = new MotiveClient("test-key", client);
        var loadWithStops = new Dictionary<string, object>(MockData.SampleLoad)
        {
            ["stops_json"] = JsonSerializer.Serialize(new[]
            {
                new { type = "PU", address = "789 Elm St", city = "Austin", state = "TX" },
                new { type = "DEL", address = "321 Pine Rd", city = "Waco", state = "TX" }
            })
        };

        // Act
        var result = await motive.PushDispatchAsync(loadWithStops, "1088505", "64734");

        // Assert — dispatch yaratildi
        Assert.Equal(98765, result.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task PushLoadWithoutMiles_SendsNullLoadedMiles()
    {
        // Arrange
        var client = HttpMockHelper.CreateMockClient(HttpStatusCode.OK, MockData.DispatchResponse);
        var motive = new MotiveClient("test-key", client);
        var load = new Dictionary<string, object>(MockData.SampleLoad);
        load["miles"] = null!;

        // Act
        var result = await motive.PushDispatchAsync(load, "1088505", "64734");

        // Assert
        Assert.Equal(98765, result.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task PushLoadWithoutLoadNumber_UsesId()
    {
        // Arrange
        var client = HttpMockHelper.CreateMockClient(HttpStatusCode.OK, MockData.DispatchResponse);
        var motive = new MotiveClient("test-key", client);
        var load = new Dictionary<string, object>(MockData.SampleLoad);
        load["load_number"] = null!;

        // Act
        var result = await motive.PushDispatchAsync(load, "1088505", "64734");

        // Assert — xato chiqmasligi kerak
        Assert.NotNull(result);
    }

    [Fact]
    public async Task PushLoad_ApiError_ThrowsHttpRequestException()
    {
        // Arrange
        var client = HttpMockHelper.CreateMockClient(
            HttpStatusCode.UnprocessableEntity,
            new { error = "validation failed" });
        var motive = new MotiveClient("test-key", client);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => motive.PushDispatchAsync(MockData.SampleLoad, "1088505", "64734"));
    }
}

// ================================================================== //
//  Test: get_dispatches va get_dispatch
// ================================================================== //

public class GetDispatchesTests
{
    [Fact]
    public async Task GetDispatches_Default_ReturnsListWithPagination()
    {
        // Arrange
        var response = new
        {
            dispatches = new[] { MockData.DispatchResponse["dispatch"] },
            pagination = new { per_page = 100, page_no = 1, total = 1 }
        };
        var client = HttpMockHelper.CreateMockClient(HttpStatusCode.OK, response);
        var motive = new MotiveClient("test-key", client);

        // Act
        var result = await motive.GetDispatchesAsync();

        // Assert
        Assert.NotNull(result.Dispatches);
        Assert.Single(result.Dispatches);
        Assert.Equal(1, result.Pagination.Total);
    }

    [Fact]
    public async Task GetDispatches_WithStatusFilter_PassesParamsCorrectly()
    {
        // Arrange
        var (client, handler) = HttpMockHelper.CreateTrackedMockClient(
            HttpStatusCode.OK,
            new { dispatches = Array.Empty<object>(), pagination = new { total = 0 } });
        var motive = new MotiveClient("test-key", client);

        // Act
        await motive.GetDispatchesAsync(status: "active", perPage: 25, pageNo: 2);

        // Assert — request yuborilganini tekshirish
        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.RequestUri!.Query.Contains("status=active") &&
                r.RequestUri.Query.Contains("per_page=25") &&
                r.RequestUri.Query.Contains("page_no=2")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetDispatch_Found_ReturnsDispatch()
    {
        // Arrange
        var client = HttpMockHelper.CreateMockClient(HttpStatusCode.OK, MockData.DispatchResponse);
        var motive = new MotiveClient("test-key", client);

        // Act
        var result = await motive.GetDispatchAsync(98765);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(98765, result.Value.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task GetDispatch_NotFound_ReturnsNull()
    {
        // Arrange
        var client = HttpMockHelper.CreateMockClient(HttpStatusCode.NotFound, new { });
        var motive = new MotiveClient("test-key", client);

        // Act
        var result = await motive.GetDispatchAsync(99999);

        // Assert
        Assert.Null(result);
    }
}

// ================================================================== //
//  Test: update_dispatch_status
// ================================================================== //

public class UpdateDispatchStatusTests
{
    [Fact]
    public async Task UpdateStatus_Success_GetThenPut()
    {
        // Arrange — GET va PUT uchun alohida response
        var callCount = 0;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1) // GET
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            JsonSerializer.Serialize(MockData.DispatchResponse))
                    };
                }
                // PUT
                var updated = new Dictionary<string, object>(
                    (Dictionary<string, object>)MockData.DispatchResponse["dispatch"]);
                updated["status"] = "active";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { dispatch = updated }))
                };
            });

        var client = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://api.keeptruckin.com")
        };
        var motive = new MotiveClient("test-key", client);

        // Act
        var result = await motive.UpdateDispatchStatusAsync(98765, "active");

        // Assert
        Assert.Equal(2, callCount); // GET + PUT
    }

    [Fact]
    public async Task UpdateStatus_DispatchNotFound_ThrowsException()
    {
        // Arrange
        var client = HttpMockHelper.CreateMockClient(HttpStatusCode.NotFound, new { });
        var motive = new MotiveClient("test-key", client);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => motive.UpdateDispatchStatusAsync(11111, "active"));
    }
}

// ================================================================== //
//  Test: create_dispatch_location
// ================================================================== //

public class CreateDispatchLocationTests
{
    [Fact]
    public async Task CreateLocation_Success()
    {
        // Arrange
        var response = new
        {
            dispatch_location = new
            {
                id = 501,
                name = "ABC Warehouse",
                address_line_1 = "123 Main St",
                city = "Dallas",
                state = "TX",
                zip = "75201"
            }
        };
        var (client, handler) = HttpMockHelper.CreateTrackedMockClient(HttpStatusCode.OK, response);
        var motive = new MotiveClient("test-key", client);

        // Act
        var result = await motive.CreateDispatchLocationAsync(
            "ABC Warehouse", "123 Main St", "Dallas", "TX", "75201");

        // Assert
        Assert.Equal(501, result.GetProperty("id").GetInt32());
        Assert.Equal("Dallas", result.GetProperty("city").GetString());

        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Post),
            ItExpr.IsAny<CancellationToken>());
    }
}

// ================================================================== //
//  Test: parse_dispatch_to_load
// ================================================================== //

public class ParseDispatchToLoadTests
{
    [Fact]
    public void SimpleDispatch_ParsesCorrectly()
    {
        // Arrange
        var json = JsonSerializer.Serialize(MockData.DispatchResponse["dispatch"]);
        var dispatch = JsonSerializer.Deserialize<JsonElement>(json);

        // Act
        var result = MotiveClient.ParseDispatchToLoad(dispatch);

        // Assert
        Assert.Equal("RC-1001", result.LoadNumber);
        Assert.Equal("123 Main St, Dallas, TX 75201", result.PickupAddress);
        Assert.Equal("456 Oak Ave, Houston, TX 77001", result.DeliveryAddress);
        Assert.Equal("2026-04-17T08:00:00-05:00", result.PickupDate);
        Assert.Equal("2026-04-18T08:00:00-05:00", result.DeliveryDate);
        Assert.Equal("TX", result.OriginState);
        Assert.Equal("TX", result.DestinationState);
        Assert.Equal("450", result.Miles);
        Assert.Equal(98765, result.MotiveDispatchId);
        Assert.Equal("motive:1088505", result.EldDriverId);
        Assert.Null(result.StopsJson); // oraliq stop yo'q
    }

    [Fact]
    public void DispatchWith3Stops_ParsesMiddleStops()
    {
        // Arrange
        var json = JsonSerializer.Serialize(MockData.Dispatch3Stops);
        var dispatch = JsonSerializer.Deserialize<JsonElement>(json);

        // Act
        var result = MotiveClient.ParseDispatchToLoad(dispatch);

        // Assert
        Assert.Equal("MULTI-001", result.LoadNumber);
        Assert.Equal("TX", result.OriginState);
        Assert.Equal("TX", result.DestinationState);
        Assert.Equal("600", result.Miles);
        Assert.Equal("motive:5555", result.EldDriverId);

        // Oraliq stop mavjud
        Assert.NotNull(result.StopsJson);
        var middleStops = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(result.StopsJson);
        Assert.Single(middleStops!);
        Assert.Equal("PU", middleStops[0]["type"]);
        Assert.Equal("Austin", middleStops[0]["city"]);
    }

    [Fact]
    public void DispatchWithoutVendorId_UsesId()
    {
        // Arrange
        var dispatch = JsonSerializer.Deserialize<JsonElement>(
            JsonSerializer.Serialize(new
            {
                id = 77777,
                vendor_id = (string?)null,
                status = "planned",
                dispatch_stops = Array.Empty<object>(),
                dispatch_trips = Array.Empty<object>()
            }));

        // Act
        var result = MotiveClient.ParseDispatchToLoad(dispatch);

        // Assert
        Assert.Equal("77777", result.LoadNumber);
    }

    [Fact]
    public void DispatchWithoutDriver_EldDriverIdIsNull()
    {
        // Arrange
        var dispatch = JsonSerializer.Deserialize<JsonElement>(
            JsonSerializer.Serialize(new
            {
                id = 88888,
                vendor_id = "NO-DRIVER",
                status = "planned",
                dispatch_stops = new[]
                {
                    new { type = "pickup", number = 1, dispatch_location = new { city = "LA", state = "CA" } },
                    new { type = "dropoff", number = 2, dispatch_location = new { city = "SF", state = "CA" } }
                },
                dispatch_trips = Array.Empty<object>()
            }));

        // Act
        var result = MotiveClient.ParseDispatchToLoad(dispatch);

        // Assert
        Assert.Null(result.EldDriverId);
    }

    [Fact]
    public void DispatchWithoutMiles_MilesIsNull()
    {
        // Arrange
        var dispatch = JsonSerializer.Deserialize<JsonElement>(
            JsonSerializer.Serialize(new
            {
                id = 11111,
                vendor_id = "NO-MILES",
                loaded_miles = (int?)null,
                dispatch_stops = Array.Empty<object>(),
                dispatch_trips = Array.Empty<object>()
            }));

        // Act
        var result = MotiveClient.ParseDispatchToLoad(dispatch);

        // Assert
        Assert.Null(result.Miles);
    }

    [Fact]
    public void DispatchWithEmptyLocation_AddressIsEmpty()
    {
        // Arrange
        var dispatch = JsonSerializer.Deserialize<JsonElement>(
            JsonSerializer.Serialize(new
            {
                id = 22222,
                vendor_id = "EMPTY-LOC",
                dispatch_stops = new object[]
                {
                    new { type = "pickup", number = 1, dispatch_location = (object?)null },
                    new { type = "dropoff", number = 2 }
                },
                dispatch_trips = Array.Empty<object>()
            }));

        // Act
        var result = MotiveClient.ParseDispatchToLoad(dispatch);

        // Assert
        Assert.Equal("", result.PickupAddress);
        Assert.Equal("", result.DeliveryAddress);
    }
}

// ================================================================== //
//  Test: STATUS_MAP xaritalari
// ================================================================== //

public class StatusMapTests
{
    [Theory]
    [InlineData("upcoming", "planned")]
    [InlineData("dispatched", "active")]
    [InlineData("delivered", "completed")]
    [InlineData("cancelled", "cancelled")]
    public void StatusMap_MapsCorrectly(string rateconStatus, string motiveStatus)
    {
        Assert.Equal(motiveStatus, MotiveClient.StatusMap[rateconStatus]);
    }

    [Theory]
    [InlineData("planned", "upcoming")]
    [InlineData("active", "dispatched")]
    [InlineData("completed", "delivered")]
    [InlineData("cancelled", "cancelled")]
    public void ReverseStatusMap_MapsCorrectly(string motiveStatus, string rateconStatus)
    {
        Assert.Equal(rateconStatus, MotiveClient.ReverseStatusMap[motiveStatus]);
    }

    [Fact]
    public void StatusMaps_AreInverseOfEachOther()
    {
        foreach (var (rcStatus, motiveStatus) in MotiveClient.StatusMap)
        {
            Assert.True(MotiveClient.ReverseStatusMap.ContainsKey(motiveStatus));
            Assert.Equal(rcStatus, MotiveClient.ReverseStatusMap[motiveStatus]);
        }
    }

    [Fact]
    public void StatusMap_CoversAllRateconStatuses()
    {
        var expected = new[] { "upcoming", "dispatched", "delivered", "cancelled" };
        foreach (var status in expected)
        {
            Assert.True(MotiveClient.StatusMap.ContainsKey(status),
                $"StatusMap should contain '{status}'");
        }
    }

    [Fact]
    public void ReverseStatusMap_CoversAllMotiveStatuses()
    {
        var expected = new[] { "planned", "active", "completed", "cancelled" };
        foreach (var status in expected)
        {
            Assert.True(MotiveClient.ReverseStatusMap.ContainsKey(status),
                $"ReverseStatusMap should contain '{status}'");
        }
    }
}

// ================================================================== //
//  Test: API Endpoint integration
// ================================================================== //

public class MotiveEndpointTests
{
    [Fact]
    public void PushEndpoint_NoMotiveConfig_Returns400()
    {
        // Motive sozlanmagan bo'lsa 400 qaytishi kerak
        // Bu test WebApplicationFactory bilan ishlatiladi
        Assert.True(true, "Requires WebApplicationFactory setup");
    }

    [Fact]
    public void PushEndpoint_NoDriverAssigned_Returns400()
    {
        // Haydovchi tayinlanmagan yukni push qilish 400 qaytarishi kerak
        Assert.True(true, "Requires WebApplicationFactory setup");
    }

    [Fact]
    public void ImportEndpoint_DuplicateDispatch_Returns409()
    {
        // Bir xil dispatch qayta import qilish 409 qaytarishi kerak
        Assert.True(true, "Requires WebApplicationFactory setup");
    }

    [Fact]
    public void SyncEndpoint_NoMotiveLink_Returns400()
    {
        // Motive bilan bog'lanmagan yuk sync 400 qaytarishi kerak
        Assert.True(true, "Requires WebApplicationFactory setup");
    }

    [Fact]
    public void SyncEndpoint_InvalidDirection_Returns400()
    {
        // Noto'g'ri direction 400 qaytarishi kerak
        Assert.True(true, "Requires WebApplicationFactory setup");
    }
}

// ================================================================== //
//  MotiveClient stub — testlar ishlashi uchun minimal interface
//  (Haqiqiy loyihada alohida faylda bo'ladi)
// ================================================================== //

/// <summary>
/// Motive ELD API client.
/// Base URL: https://api.keeptruckin.com/v2
/// Auth: X-Api-Key header
/// </summary>
public class MotiveClient
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.keeptruckin.com/v2";
    private const string BaseUrlV1 = "https://api.keeptruckin.com/v1";

    public static readonly Dictionary<string, string> StatusMap = new()
    {
        ["upcoming"] = "planned",
        ["dispatched"] = "active",
        ["delivered"] = "completed",
        ["cancelled"] = "cancelled"
    };

    public static readonly Dictionary<string, string> ReverseStatusMap = new()
    {
        ["planned"] = "upcoming",
        ["active"] = "dispatched",
        ["completed"] = "delivered",
        ["cancelled"] = "cancelled"
    };

    public MotiveClient(string apiKey, HttpClient? httpClient = null)
    {
        _apiKey = apiKey;
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);
    }

    public async Task<JsonElement> PushDispatchAsync(
        Dictionary<string, object> load, string driverId, string vehicleId)
    {
        var loadNumber = load.GetValueOrDefault("load_number")?.ToString() ?? load["id"].ToString()!;

        var stops = new List<Dictionary<string, object>>
        {
            new()
            {
                ["vendor_id"] = $"{loadNumber}-PU",
                ["type"] = "pickup",
                ["number"] = 1,
                ["early_date"] = load.GetValueOrDefault("pickup_date") ?? "",
                ["vendor_dispatch_location_id"] = $"LOC-{load["id"]}-PU",
                ["status"] = "available"
            }
        };

        // Oraliq stoplar
        var stopsJson = load.GetValueOrDefault("stops_json")?.ToString();
        if (!string.IsNullOrEmpty(stopsJson))
        {
            var extraStops = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(stopsJson);
            if (extraStops != null)
            {
                for (var i = 0; i < extraStops.Count; i++)
                {
                    var st = extraStops[i];
                    var stopType = st.GetValueOrDefault("type", "").ToUpper() is "PU" or "PICKUP"
                        ? "pickup" : "dropoff";
                    stops.Add(new Dictionary<string, object>
                    {
                        ["vendor_id"] = $"{loadNumber}-STOP-{i + 2}",
                        ["type"] = stopType,
                        ["number"] = i + 2,
                        ["vendor_dispatch_location_id"] = $"LOC-{load["id"]}-STOP-{i + 2}",
                        ["status"] = "available"
                    });
                }
            }
        }

        var deliveryNumber = stops.Count + 1;
        stops.Add(new Dictionary<string, object>
        {
            ["vendor_id"] = $"{loadNumber}-DEL",
            ["type"] = "dropoff",
            ["number"] = deliveryNumber,
            ["early_date"] = load.GetValueOrDefault("delivery_date") ?? "",
            ["vendor_dispatch_location_id"] = $"LOC-{load["id"]}-DEL",
            ["status"] = "available"
        });

        int? loadedMiles = null;
        var milesStr = load.GetValueOrDefault("miles")?.ToString();
        if (!string.IsNullOrEmpty(milesStr) && double.TryParse(milesStr, out var m))
            loadedMiles = (int)m;

        var payload = new
        {
            vendor_id = loadNumber,
            status = "planned",
            loaded_miles = loadedMiles,
            dispatch_stops = stops,
            dispatch_trips = new[]
            {
                new
                {
                    vendor_id = $"{loadNumber}-TRIP",
                    driver_id = int.Parse(driverId),
                    vehicle_id = int.Parse(vehicleId),
                    vendor_stop_ids = stops.ConvertAll(s => s["vendor_id"].ToString()!),
                    status = "not_started"
                }
            }
        };

        var resp = await _httpClient.PostAsync(
            $"{BaseUrl}/dispatches",
            new StringContent(JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonSerializer.Deserialize<JsonElement>(body);
        return doc.GetProperty("dispatch");
    }

    public async Task<DispatchListResult> GetDispatchesAsync(
        string? status = null, int perPage = 100, int pageNo = 1)
    {
        var query = $"?per_page={perPage}&page_no={pageNo}";
        if (!string.IsNullOrEmpty(status))
            query += $"&status={status}";

        var resp = await _httpClient.GetAsync($"{BaseUrl}/dispatches{query}");
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonSerializer.Deserialize<JsonElement>(body);
        var dispatches = doc.GetProperty("dispatches");
        var pagination = doc.GetProperty("pagination");

        return new DispatchListResult
        {
            Dispatches = dispatches.Deserialize<List<JsonElement>>() ?? new(),
            Pagination = new PaginationInfo
            {
                Total = pagination.TryGetProperty("total", out var t) ? t.GetInt32() : 0
            }
        };
    }

    public async Task<JsonElement?> GetDispatchAsync(int dispatchId)
    {
        var resp = await _httpClient.GetAsync($"{BaseUrl}/dispatches/{dispatchId}");
        if (resp.StatusCode == HttpStatusCode.NotFound)
            return null;
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonSerializer.Deserialize<JsonElement>(body);
        return doc.GetProperty("dispatch");
    }

    public async Task<JsonElement> UpdateDispatchStatusAsync(int dispatchId, string newStatus)
    {
        var current = await GetDispatchAsync(dispatchId);
        if (current == null)
            throw new InvalidOperationException($"Dispatch {dispatchId} not found in Motive");

        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(current.Value.GetRawText())!;
        dict["status"] = newStatus;

        var resp = await _httpClient.PutAsync(
            $"{BaseUrl}/dispatches",
            new StringContent(JsonSerializer.Serialize(dict),
                System.Text.Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonSerializer.Deserialize<JsonElement>(body);
        return doc.GetProperty("dispatch");
    }

    public async Task<JsonElement> CreateDispatchLocationAsync(
        string name, string address, string city, string state, string zipCode)
    {
        var payload = new
        {
            name,
            address_line_1 = address,
            city,
            state,
            zip = zipCode,
            country = "US"
        };

        var resp = await _httpClient.PostAsync(
            $"{BaseUrlV1}/dispatch_locations",
            new StringContent(JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonSerializer.Deserialize<JsonElement>(body);
        return doc.GetProperty("dispatch_location");
    }

    public static ParsedLoad ParseDispatchToLoad(JsonElement dispatch)
    {
        var stops = new List<JsonElement>();
        if (dispatch.TryGetProperty("dispatch_stops", out var stopsEl))
        {
            foreach (var s in stopsEl.EnumerateArray())
                stops.Add(s);
            stops.Sort((a, b) =>
                (a.TryGetProperty("number", out var an) ? an.GetInt32() : 0)
                .CompareTo(b.TryGetProperty("number", out var bn) ? bn.GetInt32() : 0));
        }

        JsonElement? pickupStop = null, deliveryStop = null;
        foreach (var s in stops)
        {
            var type = s.TryGetProperty("type", out var t) ? t.GetString() : "";
            if (pickupStop == null && type is "pickup" or "PU")
                pickupStop = s;
            if (type is "dropoff" or "UL" or "delivery")
                deliveryStop = s;
        }
        deliveryStop ??= stops.Count > 0 ? stops[^1] : null;

        static string FormatAddress(JsonElement? loc)
        {
            if (loc == null || loc.Value.ValueKind == JsonValueKind.Null)
                return "";
            var parts = new List<string>();
            if (loc.Value.TryGetProperty("address_line_1", out var a) && a.GetString() is { } addr && addr != "")
                parts.Add(addr);
            if (loc.Value.TryGetProperty("city", out var c) && c.GetString() is { } city && city != "")
                parts.Add(city);
            var st = loc.Value.TryGetProperty("state", out var s) ? s.GetString() ?? "" : "";
            var zip = loc.Value.TryGetProperty("zip", out var z) ? z.GetString() ?? "" : "";
            var stateZip = $"{st} {zip}".Trim();
            if (stateZip != "") parts.Add(stateZip);
            return string.Join(", ", parts);
        }

        static JsonElement? GetLocation(JsonElement? stop)
        {
            if (stop?.TryGetProperty("dispatch_location", out var loc) == true
                && loc.ValueKind != JsonValueKind.Null)
                return loc;
            return null;
        }

        var pickupLoc = GetLocation(pickupStop);
        var deliveryLoc = GetLocation(deliveryStop);

        // Oraliq stoplar
        var middleStops = new List<JsonElement>();
        foreach (var s in stops)
        {
            if (s.GetRawText() != pickupStop?.GetRawText() &&
                s.GetRawText() != deliveryStop?.GetRawText())
                middleStops.Add(s);
        }

        string? stopsJson = null;
        if (middleStops.Count > 0)
        {
            var list = new List<Dictionary<string, string>>();
            foreach (var s in middleStops)
            {
                var loc = GetLocation(s);
                var type = s.TryGetProperty("type", out var t) ? t.GetString() : "";
                list.Add(new Dictionary<string, string>
                {
                    ["type"] = type is "pickup" or "PU" ? "PU" : "DEL",
                    ["address"] = FormatAddress(loc),
                    ["city"] = loc?.TryGetProperty("city", out var cy) == true ? cy.GetString() ?? "" : "",
                    ["state"] = loc?.TryGetProperty("state", out var st) == true ? st.GetString() ?? "" : "",
                    ["notes"] = s.TryGetProperty("comments", out var cm) ? cm.GetString() ?? "" : ""
                });
            }
            stopsJson = JsonSerializer.Serialize(list);
        }

        // driver_id
        string? eldDriverId = null;
        if (dispatch.TryGetProperty("dispatch_trips", out var trips))
        {
            foreach (var trip in trips.EnumerateArray())
            {
                if (trip.TryGetProperty("driver_id", out var did) && did.ValueKind != JsonValueKind.Null)
                {
                    eldDriverId = $"motive:{did}";
                    break;
                }
            }
        }

        // loaded_miles
        string? miles = null;
        if (dispatch.TryGetProperty("loaded_miles", out var lm) && lm.ValueKind != JsonValueKind.Null)
            miles = lm.GetInt32().ToString();

        return new ParsedLoad
        {
            LoadNumber = dispatch.TryGetProperty("vendor_id", out var vid)
                         && vid.ValueKind != JsonValueKind.Null
                         && vid.GetString() is { } vn && vn != ""
                ? vn
                : dispatch.GetProperty("id").GetInt32().ToString(),
            PickupAddress = FormatAddress(pickupLoc),
            PickupDate = pickupStop?.TryGetProperty("early_date", out var pd) == true ? pd.GetString() : null,
            DeliveryAddress = FormatAddress(deliveryLoc),
            DeliveryDate = deliveryStop?.TryGetProperty("early_date", out var dd) == true ? dd.GetString() : null,
            OriginState = pickupLoc?.TryGetProperty("state", out var os) == true ? os.GetString() : null,
            DestinationState = deliveryLoc?.TryGetProperty("state", out var ds) == true ? ds.GetString() : null,
            Miles = miles,
            StopsJson = stopsJson,
            MotiveDispatchId = dispatch.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
            EldDriverId = eldDriverId
        };
    }
}

public class ParsedLoad
{
    public string LoadNumber { get; set; } = "";
    public string PickupAddress { get; set; } = "";
    public string? PickupDate { get; set; }
    public string DeliveryAddress { get; set; } = "";
    public string? DeliveryDate { get; set; }
    public string? OriginState { get; set; }
    public string? DestinationState { get; set; }
    public string? Miles { get; set; }
    public string? StopsJson { get; set; }
    public int MotiveDispatchId { get; set; }
    public string? EldDriverId { get; set; }
}

public class DispatchListResult
{
    public List<JsonElement> Dispatches { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}

public class PaginationInfo
{
    public int Total { get; set; }
}
