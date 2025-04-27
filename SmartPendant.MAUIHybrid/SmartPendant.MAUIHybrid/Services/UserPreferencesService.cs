using Blazored.LocalStorage;
using SmartPendant.MAUIHybrid.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPendant.MAUIHybrid.Services
{
    internal class UserPreferencesService
    {
        private readonly ILocalStorageService _localStorage;
        private const string Key = "userPreferences";

        public UserPreferencesService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task SaveUserPreferences(UserPreferences userPreferences)
        {
            await _localStorage.SetItemAsync(Key, userPreferences);
        }

        public async Task<UserPreferences> LoadUserPreferences()
        {
            return await _localStorage.GetItemAsync<UserPreferences>(Key);
        }
    }
}
