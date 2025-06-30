using Blazored.LocalStorage;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Models;
using System.Diagnostics;

namespace SmartPendant.MAUIHybrid.Services
{
    /// <summary>
    /// An implementation of IDayJournalRepository that uses LocalStorage
    /// for client-side data persistence.
    /// </summary>
    public class LocalDayJournalRepository : IDayJournalRepository
    {
        #region Fields

        private readonly ILocalStorageService _localStorage;
        private const string dayRecordListKey = "dayRecord_list";

        #endregion

        #region Constructor

        public LocalDayJournalRepository(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        #endregion

        #region Public Methods

        public async Task SaveAsync(DayRecord dayRecord)
        {
            if (dayRecord == null) return;

            try
            {
                var keys = await GetdayRecordKeyListAsync();
                var key = GetDateKey(dayRecord.Date);

                if (!keys.Contains(key))
                {
                    keys.Add(key);
                    await _localStorage.SetItemAsync(dayRecordListKey, keys);
                }

                await _localStorage.SetItemAsync(key, dayRecord);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving dayRecord for {dayRecord.Date:d}: {ex.Message}");
            }
        }

        public async Task<DayRecord?> GetAsync(DateTime date)
        {
            var key = GetDateKey(date);
            try
            {
                return await _localStorage.GetItemAsync<DayRecord>(key);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving dayRecord for {date:d}: {ex.Message}");
                return null;
            }
        }

        public async Task DeleteAsync(DateTime date)
        {
            var key = GetDateKey(date);

            try
            {
                var keys = await GetdayRecordKeyListAsync();
                if (keys.Remove(key))
                {
                    await _localStorage.SetItemAsync(dayRecordListKey, keys);
                }
                await _localStorage.RemoveItemAsync(key);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting dayRecord for {date:d}: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        private static string GetDateKey(DateTime date)
        {
            return $"dayRecord_{date:yyyyMMdd}";
        }

        private async Task<List<string>> GetdayRecordKeyListAsync()
        {
            try
            {
                return await _localStorage.GetItemAsync<List<string>>(dayRecordListKey) ?? [];
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving dayRecord key list: {ex.Message}");
                return [];
            }
        }

        #endregion
    }
}
