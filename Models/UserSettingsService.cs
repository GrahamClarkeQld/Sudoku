using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Sudoku.Models
{
    public class UserSettingsService
    {
        private readonly IJSRuntime _jsRuntime;
        private const string SettingsKey = "UserStringSettings";

        public UserSettingsService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task SaveStringListAsync(List<string> list)
        {
            var json = JsonSerializer.Serialize(list);
            await _jsRuntime.InvokeVoidAsync("BlazorSetLocalStorage", SettingsKey, json);
        }

        public async Task<List<string>> LoadStringListAsync()
        {
            var json = await _jsRuntime.InvokeAsync<string>("BlazorGetLocalStorage", SettingsKey);
            if (string.IsNullOrEmpty(json))
            {
                return new List<string>();
            }
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
    }

}
