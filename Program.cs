
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Operations;
using Microsoft.VisualStudio.Services.WebApi;


VssConnection connection = new VssConnection(new Uri(""), new VssCredentials());

var projectNames = new List<string>();
foreach (var projectName in File.ReadLines("projectNames.csv"))
{
    projectNames.Add(projectName.ToLower());
}

foreach(var projectName in projectNames)
{
    var projectGuid = GetProjectId(connection, projectName);

    var projectDeleted = DeleteProject(connection, projectGuid);

    if(projectDeleted)
        Console.WriteLine($"Project {projectName} deleted successfully.");
    else
        Console.WriteLine($"Project {projectName} could not be deleted.");

    Console.ReadLine();
}

static Guid GetProjectId(VssConnection connection, string projectName)
{
    ProjectHttpClient projectClient = connection.GetClient<ProjectHttpClient>();

    // Get the details for the specified project
    TeamProject project = projectClient.GetProject(projectName).Result;

    
    Console.WriteLine("Details for project {0}:", projectName);
    Console.WriteLine();
    Console.WriteLine("  ID          : {0}", project.Id);

    return project.Id;
}

static bool DeleteProject(VssConnection connection, Guid projectId)
{    
    ProjectHttpClient projectClient = connection.GetClient<ProjectHttpClient>();

    Console.WriteLine("Queuing project delete...");

    // Queue the delete operation
    Guid operationId = projectClient.QueueDeleteProject(projectId).Result.Id;

    // Check the operation status every 2 seconds (for up to 30 seconds)
    Operation operationResult = WaitForLongRunningOperation(connection, operationId, 2, 30).Result;

    Console.WriteLine();
    Console.WriteLine("Delete project operation completed {0}", operationResult.Status);

    return operationResult.Status == OperationStatus.Succeeded;
}

static async Task<Operation> WaitForLongRunningOperation(VssConnection connection, Guid operationId, int interavalInMins = 5, int maxTimeInDays = 60, CancellationToken cancellationToken = default(CancellationToken))
{
    OperationsHttpClient operationsClient = connection.GetClient<OperationsHttpClient>();
    DateTime expiration = DateTime.Now.AddDays(maxTimeInDays);
    int checkCount = 0;

    while (true)
    {
        Console.WriteLine(" Checking status ({0})... ", (checkCount++));

        Operation operation = await operationsClient.GetOperation(operationId, cancellationToken);

        if (!operation.Completed)
        {
            Console.WriteLine("   Pausing {0} mins", interavalInMins);

            await Task.Delay(TimeSpan.FromMinutes(interavalInMins));

            if (DateTime.Now > expiration)
            {
                throw new Exception(String.Format("Operation did not complete in {0} days.", maxTimeInDays));
            }
        }
        else
        {
            return operation;
        }
    }
}