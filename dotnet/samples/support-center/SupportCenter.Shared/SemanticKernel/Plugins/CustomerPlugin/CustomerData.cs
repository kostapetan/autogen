// Copyright (c) Microsoft Corporation. All rights reserved.
// CustomerData.cs

using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SupportCenter.Shared.Data.CosmosDb;
using SupportCenter.Shared.Data.CosmosDb.Entities;
using SupportCenter.Shared.Extensions;

namespace SupportCenter.Shared.SemanticKernel.Plugins.CustomerPlugin;

public class CustomerData(
    ILogger<CustomerData> logger,
    ICustomerRepository customerRepository)
{
    private readonly ILogger<CustomerData> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ICustomerRepository _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));

    [KernelFunction, Description("Get customer data")]
    public async Task<Customer> GetCustomerDataAsync(
        [Description("The customer id")] string customerId)
    {
        _logger.LogInformation("Executing {FunctionName} function. Params: {parameters}", nameof(GetCustomerDataAsync), string.Join(", ", [customerId]));
        var customer = await _customerRepository.GetCustomerByIdAsync(customerId);
        if (customer == null)
        {
            _logger.LogWarning("Customer with id {customerId} not found", customerId);
        }
        return customer ?? new Customer();
    }

    [KernelFunction, Description("Get all customers")]
    public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
    {
        _logger.LogInformation("Executing {FunctionName} function", nameof(GetAllCustomersAsync));
        return await _customerRepository.GetCustomersAsync();
    }

    [KernelFunction, Description("Insert customer data")]
    public async Task InsertCustomerDataAsync(
        [Description("The customer data")] Customer customer)
    {
        _logger.LogInformation("Executing {FunctionName} function. Params: {parameters}", nameof(InsertCustomerDataAsync), customer.ToStringCustom());
        await _customerRepository.InsertCustomerAsync(customer);
    }

    [KernelFunction, Description("Update customer data")]
    public async Task UpdateCustomerDataAsync(
        [Description("The customer data")] Customer customer)
    {
        _logger.LogInformation("Executing {FunctionName} function. Params: {parameters}", nameof(UpdateCustomerDataAsync), customer.ToStringCustom());
        await _customerRepository.UpdateCustomerAsync(customer);
    }
}
