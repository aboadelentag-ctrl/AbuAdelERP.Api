using KayanERP.Api.Controllers;
using KayanERP.Api.DTOs;
using KayanERP.UI.DTOs;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace KayanERP.UI.Services
{
    public class SalesClientService : ISalesClientService
    {
        private readonly HttpClient _http;

        public SalesClientService(HttpClient http)
        {
            _http = http;
        }

        // ==============================================================
        // 1. عمليات فواتير المبيعات
        // ==============================================================

        public async Task<List<SalesInvoiceViewDto>> GetAllInvoicesAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<SalesInvoiceViewDto>>("api/sales") ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SalesClientService] Error fetching invoices: {ex.Message}");
                return new();
            }
        }

        public async Task<SalesInvoiceViewDto?> GetInvoiceByIdAsync(long id)
        {
            try
            {
                return await _http.GetFromJsonAsync<SalesInvoiceViewDto>($"api/sales/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SalesClientService] Error fetching invoice #{id}: {ex.Message}");
                return null;
            }
        }

        public async Task<SalesLookupsDto> GetLookupsAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<SalesLookupsDto>("api/sales/lookups") ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SalesClientService] Error fetching sales lookups: {ex.Message}");
                return new();
            }
        }

        public async Task<SalesInvoiceResultDto> CreateInvoiceAsync(SalesInvoiceCreateDto dto)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/sales", dto);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<SalesInvoiceResultDto>()
                           ?? new SalesInvoiceResultDto { Success = true, Message = "تم إصدار الفاتورة بنجاح." };
                }

                var errorMsg = await response.Content.ReadAsStringAsync();
                return new SalesInvoiceResultDto
                {
                    Success = false,
                    Message = !string.IsNullOrWhiteSpace(errorMsg) ? errorMsg : "فشل اعتماد الفاتورة من الخادم."
                };
            }
            catch (Exception ex)
            {
                return new SalesInvoiceResultDto
                {
                    Success = false,
                    Message = $"خطأ في الاتصال بالخادم: {ex.Message}"
                };
            }
        }

        // ==============================================================
        // 2. عمليات دليل العملاء
        // ==============================================================

        public async Task<List<CustomerItemDto>> GetAllCustomersAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<CustomerItemDto>>("api/customers") ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SalesClientService] Error fetching customers: {ex.Message}");
                return new();
            }
        }

        public async Task<bool> SaveCustomerAsync(CustomerItemDto customer)
        {
            try
            {
                HttpResponseMessage response;
                if (customer.Id > 0)
                {
                    response = await _http.PutAsJsonAsync($"api/customers/{customer.Id}", customer);
                }
                else
                {
                    response = await _http.PostAsJsonAsync("api/customers", customer);
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SalesClientService] Error saving customer: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ToggleCustomerStatusAsync(long id)
        {
            try
            {
                var response = await _http.PatchAsync($"api/customers/{id}/toggle-status", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SalesClientService] Error toggling customer status: {ex.Message}");
                return false;
            }
        }
    }
}