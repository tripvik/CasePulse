using Blazored.LocalStorage;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Models;
using System.Diagnostics;

namespace SmartPendant.MAUIHybrid.Services
{
    /// <summary>
    /// An implementation of IDayDataService that uses LocalStorage
    /// for client-side data persistence.
    /// </summary>
    public class LocalStorageDayDataService : IDayDataService
    {
        private readonly ILocalStorageService _localStorage;
        private const string DayModelListKey = "daymodel_list";

        public LocalStorageDayDataService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task SaveDayModelAsync(DayModel dayModel)
        {
            if (dayModel == null) return;

            try
            {
                var keys = await GetDayModelKeyListAsync();
                var key = GetDateKey(dayModel.Date);

                if (!keys.Contains(key))
                {
                    keys.Add(key);
                    await _localStorage.SetItemAsync(DayModelListKey, keys);
                }

                await _localStorage.SetItemAsync(key, dayModel);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving DayModel for {dayModel.Date:d}: {ex.Message}");
            }
        }

        public async Task<DayModel?> GetDayModelAsync(DateTime date)
        {
            var key = GetDateKey(date);
            try
            {
                return await _localStorage.GetItemAsync<DayModel>(key);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving DayModel for {date:d}: {ex.Message}");
                return null;
            }
        }

        public async Task DeleteDayModelAsync(DateTime date)
        {
            var key = GetDateKey(date);

            try
            {
                var keys = await GetDayModelKeyListAsync();
                if (keys.Remove(key))
                {
                    await _localStorage.SetItemAsync(DayModelListKey, keys);
                }
                await _localStorage.RemoveItemAsync(key);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting DayModel for {date:d}: {ex.Message}");
            }
        }

        private static string GetDateKey(DateTime date)
        {
            return $"daymodel_{date:yyyyMMdd}";
        }

        private async Task<List<string>> GetDayModelKeyListAsync()
        {
            try
            {
                return await _localStorage.GetItemAsync<List<string>>(DayModelListKey) ?? new List<string>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving DayModel key list: {ex.Message}");
                return new List<string>();
            }
        }
    }
}
