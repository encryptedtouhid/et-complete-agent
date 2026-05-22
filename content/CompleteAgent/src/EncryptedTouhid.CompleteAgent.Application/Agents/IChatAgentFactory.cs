using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace EncryptedTouhid.CompleteAgent.Application.Agents;

public interface IChatAgentFactory
{
    AIAgent Create(string name, string instructions, IEnumerable<AIFunction>? tools = null);
}
