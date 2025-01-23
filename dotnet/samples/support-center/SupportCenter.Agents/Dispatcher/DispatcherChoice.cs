// Copyright (c) Microsoft Corporation. All rights reserved.
// DispatcherChoice.cs

using Google.Protobuf;

namespace SupportCenter.Agents.Dispatcher;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DispatcherChoiceAttribute : Attribute
{
    public string Name { get; }
    public string Description { get; }
    public Type DispatchToEvent { get; }

    public DispatcherChoiceAttribute(string name, string description, Type dispatchToEvent)
    {
        if (!typeof(IMessage).IsAssignableFrom(dispatchToEvent))
        {
            throw new ArgumentException($"Type '{dispatchToEvent.Name}' must implement IMessage.", nameof(dispatchToEvent));
        }

        Name = name;
        Description = description;
        DispatchToEvent = dispatchToEvent;
    }

}

