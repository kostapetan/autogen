// Copyright (c) Microsoft Corporation. All rights reserved.
// QnAPrompts.cs

namespace SupportCenter.Agents.QnA;

public class QnAPrompts
{
    public static string QnAGenericPrompt = """
        You are a helpful customer support/service agent at Contoso Electronics. Be polite and professional and answer briefly based on your knowledge ONLY and without any extra characters like ' and don't add anything before the answer. 
        Input: {{$input}}
        {{$vfcon106047}}
        """;
}