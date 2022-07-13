using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Trace;

namespace Sample.MoneyTransfer.API.Controllers;

class SampleInsightsService
{
    private static readonly ActivitySource Activity = new(nameof(SampleInsightsService));

    private void Connect(string connectionName)
    {
        throw new ConnectionAbortedException($"aborting connection named {connectionName}");
    }

    private void Connect()
    {
        Connect("basic");
    }

    // method DoSomething has Overloading implementations
    public void DoSomething()
    {
        Connect();
    }

    public void DoSomething(int int1)
    {
        Connect($"basic{int1}");
    }

    public void DoSomething(string str1)
    {
        Connect(str1);
    }

    public void DoSomething(string str1, out bool bool1, IList<string> list1)
    {
        bool1 = true;
        Connect(str1);
    }

    public void DoSomething(ref long[] longsArr1, IEnumerable<string> enumerable1, Func<int, string> func1)
    {
        var str = func1.Invoke(7);
        Connect(str);
    }

    public void DoSomething(IDictionary<string, string> dict1, double[][][] doublesJaggedArr1)
    {
        Connect();
    }

    public void DoSomething(ICollection<object> objectsColl1, long[,,][,,,,][,][,,,] longsMultidimensionalArr1)
    {
        Connect();
    }

    public void DoSomething(Func<int, int, string> func1, float[][,,,][][,,] floatsMixJaggedAndMultidimensionalArr1)
    {
        var str = func1.Invoke(7, 8);
        Connect(str);
    }

    public void DoSomethingElse()
    {
        Connect("else");
    }

    public void ThrowArgumentException()
    {
        using var activity = Activity.StartActivity("Rethrow2");
        {
            throw new ArgumentException("empty argument2");
        }
    }

    public void HandledException()
    {
        using var activity = Activity.StartActivity("HandledException");
        {
            try
            {
                throw new ArgumentException("empty argument");
            }
            catch (Exception ex)
            {
                activity.RecordException(ex);
            }
        }
    }
}

[ApiController]
[Route("[controller]")]
public class SampleInsightsController : ControllerBase
{
    private static readonly ActivitySource Activity = new(nameof(SampleInsightsController));
    private static readonly Random Random = new(Math.Abs((int)DateTime.Now.Ticks));
    private readonly SampleInsightsService _service;

    public SampleInsightsController()
    {
        _service = new SampleInsightsService();
    }

    [HttpGet]
    [Route("Rethrow2")]
    public async Task Rethrow2()
    {
        using var activity = Activity.StartActivity("Rethrow2");
        await Task.Delay(TimeSpan.FromMilliseconds(1));
        try
        {
            _service.ThrowArgumentException();
        }
        catch (Exception ex)
        {
            activity.RecordException(ex);
            throw new ArgumentException("empty argument", ex);
        }
    }

    [HttpGet]
    [Route("Rethrow1")]
    public async Task Rethrow1()
    {
        using var activity = Activity.StartActivity("Rethrow1");
        await Task.Delay(TimeSpan.FromMilliseconds(1));
        try
        {
            _service.ThrowArgumentException();
        }
        catch (Exception ex)
        {
            throw new ArgumentException("empty argument", ex);
        }
    }

    [HttpGet]
    [Route("Handled")]
    public async Task Handled()
    {
        using var activity = Activity.StartActivity("Handled");
        await Task.Delay(TimeSpan.FromMilliseconds(1));
        _service.HandledException();
        throw new ArgumentException("empty argument");
    }

    [HttpGet]
    [Route("ErrorSource")]
    public async Task ErrorSource()
    {
        using var activity = Activity.StartActivity("ErrorSource");
        await Task.Delay(TimeSpan.FromMilliseconds(1));

        var randVal = Random.Next(1, 11);
        switch (randVal)
        {
            case 1:
                _service.DoSomethingElse();
                break;
            case 2:
                _service.DoSomething();
                break;
            case 3:
                _service.DoSomething(3);
                break;
            case 4:
                _service.DoSomething("lets go");
                break;
            case 5:
                _service.DoSomething("yes", out bool bool1, new List<string>());
                break;
            case 6:
                var longsArr = new long[] { };
                _service.DoSomething(ref longsArr, new string[] { }, x => $"val={x}");
                break;
            case 7:
                _service.DoSomething(new Dictionary<string, string>(), new double[][][] { });
                break;
            case 8:
                _service.DoSomething(new object[] { }, new long[,,][,,,,][,][,,,] { });
                break;
            case 9:
                _service.DoSomething((x, y) => $"sum={x + y}", new float[][,,,][][,,] { });
                break;
            default:
                _service.DoSomething();
                break;
        }
    }

    [HttpGet]
    [Route("Error")]
    public async Task Error()
    {
        using var activity = Activity.StartActivity("Error");
        await Task.Delay(TimeSpan.FromMilliseconds(1));
        if (Random.Next(1, 10) % 2 == 0)
        {
            throw new InvalidOperationException("operation is not valid");
        }

        throw new ValidationException("random validation error");
    }

    [HttpGet]
    [Route("SlowEndpoint")]
    public async Task SlowEndpoint([FromQuery] int extraLatency)
    {
        await Task.Delay(extraLatency);
    }

    [HttpGet]
    [Route("SpanBottleneck")]
    public async Task SpanBottleneck()
    {
        using var activity1 = Activity.StartActivity("SpanBottleneck 1");
        await Task.Delay(TimeSpan.FromMilliseconds(200));

        using var activity2 = Activity.StartActivity("SpanBottleneck 2");
        await Task.Delay(TimeSpan.FromMilliseconds(100));
    }

    [HttpGet]
    [Route("OverloadingA1")]
    public async Task MethodOverloadingA([FromQuery] String name)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(11));
    }

    [HttpGet]
    [Route("OverloadingA2")]
    public async Task MethodOverloadingA([FromQuery] String name, [FromQuery] int[] ids)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(12));
    }

    [HttpGet]
    [Route("OverloadingA3")]
    public async Task MethodOverloadingA([FromQuery] String name, [FromQuery] String description,
        [FromQuery] long longId)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(13));
    }

    /*
     *1       1  
     * *3     10 mid
     * *1     20 calls
     */
    [HttpGet]
    [Route("LowUsage")]
    public async Task LowUsage()
    {
        await Task.Delay(TimeSpan.FromMilliseconds(5));
    }

    [HttpGet]
    [Route("NormalUsage")]
    public async Task NormalUsage()
    {
        await Task.Delay(TimeSpan.FromMilliseconds(5));
    }

    [HttpGet]
    [Route("HighUsage")]
    public async Task HighUsage()
    {
        await Task.Delay(TimeSpan.FromMilliseconds(5));
    }
}