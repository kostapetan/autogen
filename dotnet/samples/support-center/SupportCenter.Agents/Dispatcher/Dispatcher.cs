// Copyright (c) Microsoft Corporation. All rights reserved.
// Dispatcher.cs

using System.Reflection;
using Google.Protobuf;
using Microsoft.AutoGen.Agents;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using SupportCenter.Agents.Extensions;
using SupportCenter.Shared;

namespace SupportCenter.Agents.Dispatcher;

[TopicSubscription("default")]
[DispatcherChoice("QnA", "The customer is asking a question related to internal Contoso knowledge base.", typeof(QnARequest))]
[DispatcherChoice("Discount", "The customer is asking for a discount about a product or service.", typeof(DiscountRequest))]
[DispatcherChoice("Invoice", "The customer is asking for an invoice.", typeof(InvoiceRequest))]
[DispatcherChoice("CustomerInfo", "The customer is asking for reading or updating his or her personal data.", typeof(CustomerInfoRequest))]
//[DispatcherChoice("Conversation", "The customer is having a generic conversation. When the request is generic or can't be classified differently, use this choice.", typeof(ConversationRequest)]

public class Dispatcher(
    IAgentWorker worker,
    Kernel kernel,
    ISemanticTextMemory memory,
    [FromKeyedServices("EventTypes")] EventTypes typeRegistry,
    ILogger<Dispatcher> logger)
    : SKAiAgent<DispatcherState>(worker, memory, kernel, typeRegistry),
    IHandle<UserNewConversation>,
    IHandle<UserConnected>,
    IHandle<UserChatInput>,
    IHandle<QnAResponse>,
    IHandle<DiscountResponse>,
    IHandle<InvoiceResponse>,
    IHandle<CustomerInfoResponse>
{
    public async Task Handle(UserNewConversation item)
    {
        logger.LogInformation($"[{nameof(Dispatcher)}] Event {nameof(UserNewConversation)}");
        // The user started a new conversation.
        _state.History.Clear();
    }

    public Task Handle(QnAResponse item)
    {
        throw new NotImplementedException();
    }

    public async Task Handle(UserChatInput item)
    {
        // TODO: check if id is needed in messages
        var (_, userId, message) = item.GetAgentData();
        var intent = await GetIntentAsync(item.Message).ConfigureAwait(false);

        var notif = new DispatcherNotification
        {
            UserId = userId,
            Message = $"The user request has been dispatched to the '{intent}' agent."
        };
        await PublishEventAsync(notif.ToCloudEvent(AgentId.ToString())).ConfigureAwait(false);

        await SendDispatcherEvent(userId, intent, message).ConfigureAwait(false);
    }

    public Task Handle(UserConnected item)
    {
        throw new NotImplementedException();
    }

    public Task Handle(InvoiceResponse item)
    {
        throw new NotImplementedException();
    }

    public Task Handle(DiscountResponse item)
    {
        throw new NotImplementedException();
    }

    public Task Handle(CustomerInfoResponse item)
    {
        throw new NotImplementedException();
    }

    private async Task<string> GetIntentAsync(string message)
    {
        var input = AppendChatHistory(message);

        var context = new KernelArguments
        {
            ["input"] = input,
            ["choices"] = GetAndSerializeChoices()
        };
        var result = await CallFunction(DispatcherPrompts.IntentPrompt, context).ConfigureAwait(false);
        return result.Trim(' ', '\"', '.') ?? string.Empty;
    }

    private string GetAndSerializeChoices()
    {
        var choices = this.GetType()
            .GetCustomAttributes<DispatcherChoiceAttribute>()
            .Select(attr => $"- {attr.Name}: {attr.Description}")
            .ToArray();

        return string.Join("\n", choices);
    }

    private async Task SendDispatcherEvent(string userId, string intent, string message)
    {
        var dispatcherChoice = this.GetType()
            .GetCustomAttributes<DispatcherChoiceAttribute>()
            .FirstOrDefault(attr => string.Equals(attr.Name, intent, StringComparison.InvariantCultureIgnoreCase));

        if (dispatcherChoice == null)
        {
            _logger.LogWarning("Intent '{Intent}' not recognized, defaulting to 'Conversation'.", intent);
            return;
            //dispatcherChoice = new DispatcherChoiceAttribute("Conversation", "Default conversation handler.", null);
        }

        var eventMessage = (IMessage?)Activator.CreateInstance(dispatcherChoice.DispatchToEvent);

        if (eventMessage is null)
        {
            _logger.LogError("Failed to create an instance of '{EventType}'.", dispatcherChoice.DispatchToEvent.Name);
            throw new InvalidOperationException($"Could not create an instance of '{dispatcherChoice.DispatchToEvent.Name}'.");
        }

        var eventType = eventMessage.GetType();
        var userIdProperty = eventType.GetProperty("UserId") ?? eventType.GetProperty("user_id");
        userIdProperty?.SetValue(eventMessage, userId);

        var messageProperty = eventType.GetProperty("Message") ?? eventType.GetProperty("message");
        messageProperty?.SetValue(eventMessage, message);

        await PublishEventAsync(eventMessage.ToCloudEvent(AgentId.ToString())).ConfigureAwait(false);
        _logger.LogInformation("Dispatched event '{EventType}' for intent '{Intent}' to user '{UserId}'.", dispatcherChoice.DispatchToEvent.Name, intent, userId);
    }
}

