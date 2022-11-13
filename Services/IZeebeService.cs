using Zeebe.Client.Api.Responses;

namespace Cloudstarter.Services;

public interface IZeebeService
{
    public Task<IDeployResponse> Deploy(string modelFilename);
    public Task<String> StartWorkflowInstance(string bpmProcessId);
    public void StartWorkers();
    public Task<ITopology> Status();
}