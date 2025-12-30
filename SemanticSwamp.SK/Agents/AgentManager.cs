// Copyright (c) Microsoft. All rights reserved.

//using DocumentFormat.OpenXml.Math;
using SemanticSwamp.Shared.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI.Assistants;
using SemanticSwamp;
using System.ClientModel;
using System.Net.WebSockets;
using System.Text;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;


#pragma warning disable OPENAI001
#pragma warning disable SKEXP0110
#pragma warning disable OPENAI001

namespace Agents;


/// <summary>
/// Demonstrate that two different agent types are able to participate in the same conversation.
/// In this case a <see cref="ChatCompletionAgent"/> and <see cref="OpenAIAssistantAgent"/> participate.
/// </summary>
public class AgentManager()
{



    //public async Task ChatWithOpenAIAssistantAgentAndChatCompletionAgent(ConfigurationValues configValues, Kernel kernel, WebSocket webSocket, List<AgentFromWeb> agents, string prompt)
    //{
    //    AssistantClient assistantClient = new AssistantClient(configValues.OpenAISettings.OpenAI_ApiKey);

    //    var chat = await GetAgentGroupChat(assistantClient, agents, configValues.OpenAISettings.OpenAI_Model, kernel, prompt);
    //    await BeginChat(chat, prompt, webSocket);
    //}


    


    protected async Task<AgentGroupChat> GetAgentGroupChat(AssistantClient assistantClient,
        List<AgentFromWeb> agents,
        string model,
        Kernel kernel,
        string query)
    {

        var finalApproverFromWeb = agents.FirstOrDefault(x => x.FinalReviewer);
        var htmlDescription = " and return results in rich HTML with the root node as a DIV";

        var finalApprover = await GetAssistant(finalApproverFromWeb.Name, finalApproverFromWeb.Description + htmlDescription + " and there are no more changes needed, no more issues or questions, and you are very satisfied with the result, then and only then do you say ApprovedAndDone", model, assistantClient);

        List<ChatCompletionAgent> chatAgents = new List<ChatCompletionAgent>();
        List<Agent> agentList = new List<Agent>();
        foreach (var agent in agents.Where(x => !x.FinalReviewer))
        {
            var actualAgent = GetAgent(agent.Name, agent.Description + htmlDescription, kernel);
            agentList.Add(actualAgent);

        }

        OpenAIAssistantAgent finalApproverAssistant = new OpenAIAssistantAgent(finalApprover, assistantClient);

        agentList.Add(finalApproverAssistant);

        AgentGroupChat chat =
            new(agentList.ToArray())
            {
                ExecutionSettings =
                    new()
                    {
                        // Here a TerminationStrategy subclass is used that will terminate when
                        // an assistant message contains the term "approve".
                        TerminationStrategy =
                            new ApprovalTerminationStrategy("ApprovedAndDone")
                            {
                                // Only the art-director may approve.
                                Agents = [finalApproverAssistant],
                                // Limit total number of turns
                                MaximumIterations = 10,
                            }
                    },
            };

        return chat;
    }



    protected async Task BeginChat(AgentGroupChat agentGroupChat, string topic, WebSocket webSocket)
    {
        // Invoke chat and display messages.
        ChatMessageContent input = new(AuthorRole.User, topic);
        agentGroupChat.AddChatMessage(input);
       
        await foreach (ChatMessageContent response in agentGroupChat.InvokeAsync())
        {
            var resultString = response.AsJson();
            var resultMsgBytes = Encoding.UTF8.GetBytes(resultString);

            await webSocket.SendAsync(
                 resultMsgBytes,
                 WebSocketMessageType.Text,
                 true,
                 CancellationToken.None);
        }

    }


    public ChatCompletionAgent GetAgent(string name, string instructions, Kernel kernel)
    {
        return new ChatCompletionAgent()
        {
            Instructions = instructions,
            Name = name,
            Kernel = kernel,
        };
    }

    public async Task<Assistant> GetAssistant(string name, string instructions, string model, AssistantClient assistantClient)
    {

        var result = await assistantClient.CreateAssistantAsync(
            model,
            new AssistantCreationOptions
            {
                Name = name,
                Instructions = instructions,
            });

        return result;
    }


#pragma warning disable SKEXP0110
    protected sealed class ApprovalTerminationStrategy(string valueToCauseAnExit = "ApprovedAndDone") : TerminationStrategy
    {
        protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
            => Task.FromResult(history[history.Count - 1].Content?.Contains("ApprovedAndDone", StringComparison.OrdinalIgnoreCase) ?? false);
    }


    protected virtual async Task<ClientResult<Assistant>> CreateAssistantAsync(string model, AssistantCreationOptions options = null, CancellationToken cancellationToken = default)
    {
        ClientResult protocolResult = await CreateAssistantAsync(model, options, cancellationToken).ConfigureAwait(false);
        return ClientResult.FromValue((Assistant)protocolResult, protocolResult.GetRawResponse());
    }

}

