using Cloudstarter.Dtos;
using fastJSON;
using NLog.Extensions.Logging;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;

namespace Cloudstarter.Services;

public class ZeebeService: IZeebeService
{
    private readonly IZeebeClient _client;
    private readonly ILogger<ZeebeService> _logger;
    private static readonly string DemoProcessPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "demo-process.bpmn");
    private static readonly string ZeebeUrl = "localhost:26500";
    private static readonly string ProcessInstanceVariables = "{\"a\":\"123\"}";
    private static readonly string JobType = "payment-service";
    private static readonly string WorkerName = Environment.MachineName;
    private static readonly long WorkCount = 100L;

    public ZeebeService(ILogger<ZeebeService> logger)
    {
        _logger = logger;
       
        // create zeebe client
        _client = ZeebeClient.Builder()
            .UseLoggerFactory(new NLogLoggerFactory())
            .UseGatewayAddress(ZeebeUrl)
            .UsePlainText()
            .Build();
    }
    
    public async Task<IDeployResponse> Deploy(string modelFilename)
    {
        var filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, "Resources", modelFilename);
        var deployResponse = await _client.NewDeployCommand().AddResourceFile(filename).Send();
        var processDefinitionKey = deployResponse.Processes[0].ProcessDefinitionKey;
        _logger.LogInformation("Deployed BPMN Model: " + processDefinitionKey);
        return deployResponse;
    }
    
    public async Task<String> StartWorkflowInstance(string bpmProcessId)
    {
        _logger.LogInformation("Creating workflow instance...");
        var instance = await _client.NewCreateProcessInstanceCommand()
            .BpmnProcessId(bpmProcessId)
            .LatestVersion()
            .Variables("{\"name\": \"Josh Wulf\"}")
            .WithResult()
            .Send();
        var jsonParams = new JSONParameters {ShowReadOnlyProperties = true};
        return JSON.ToJSON(instance, jsonParams);
    }
    
    public void StartWorkers()
    {
        CreateGetTimeWorker();
    }
    
    public void CreateGetTimeWorker()
    {
        _createWorker("get-time", async (client, job) =>
        {
            _logger.LogInformation("Received job: " + job);
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync("https://json-api.joshwulf.com/time"))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                            
                    await client.NewCompleteJobCommand(job.Key)
                        .Variables("{\"time\":" + apiResponse + "}")
                        .Send();
                    _logger.LogInformation("Get Time worker completed");
                }
            }
        });    
    }
    
    public void CreateMakeGreetingWorker()
    {
        _createWorker("make-greeting", async (client, job) =>
        {
            _logger.LogInformation("Make Greeting Received job: " + job);
            var headers = JSON.ToObject<MakeGreetingCustomHeadersDto>(job.CustomHeaders);
            var variables = JSON.ToObject<MakeGreetingVariablesDto>(job.Variables);
            string greeting = headers.greeting;
            string name = variables.name;

            await client.NewCompleteJobCommand(job.Key)
                .Variables("{\"say\": \"" + greeting + " " + name + "\"}")
                .Send();
            _logger.LogInformation("Make Greeting Worker completed job");
        });    
    }

    public Task<ITopology> Status()
    {
        return _client.TopologyRequest().Send();
    }
    
    private void _createWorker(String jobType, JobHandler handleJob)
    {
        _client.NewWorker()
            .JobType(jobType)
            .Handler(handleJob)
            .MaxJobsActive(5)
            .Name(jobType)
            .PollInterval(TimeSpan.FromSeconds(50))
            .PollingTimeout(TimeSpan.FromSeconds(50))
            .Timeout(TimeSpan.FromSeconds(10))
            .Open();
    }
}