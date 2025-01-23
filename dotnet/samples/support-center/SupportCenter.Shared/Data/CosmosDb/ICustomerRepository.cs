// Copyright (c) Microsoft Corporation. All rights reserved.
// ICustomerRepository.cs

using SupportCenter.Shared.Data.CosmosDb.Entities;

namespace SupportCenter.Shared.Data.CosmosDb;

public interface ICustomerRepository
{
    Task<Customer?> GetCustomerByIdAsync(string customerId);
    Task<IEnumerable<Customer>> GetCustomersAsync();
    Task InsertCustomerAsync(Customer customer);
    Task UpdateCustomerAsync(Customer customer);
}