using SmartPendant.MAUIHybrid.Models;

namespace SmartPendant.MAUIHybrid.Abstractions
{
    /// <summary>
    /// Provides methods for saving, retrieving, and deleting daily data models.
    /// </summary>
    public interface IDayJournalRepository
    {
        #region Save

        /// <summary>
        /// Saves the specified <see cref="DayRecord"/> to persistent storage.
        /// </summary>
        /// <param name="dayRecord">The day model to save.</param>
        /// <returns>A task that represents the asynchronous save operation.</returns>
        Task SaveAsync(DayRecord dayRecord);

        #endregion

        #region Retrieve

        /// <summary>
        /// Retrieves a <see cref="DayRecord"/> for the specified date.
        /// </summary>
        /// <param name="date">The date to retrieve data for.</param>
        /// <returns>
        /// A task that represents the asynchronous get operation. 
        /// The task result contains the <see cref="DayRecord"/> if found; otherwise, <c>null</c>.
        /// </returns>
        Task<DayRecord?> GetAsync(DateTime date);

        #endregion

        #region Delete

        /// <summary>
        /// Deletes the <see cref="DayRecord"/> associated with the specified date.
        /// </summary>
        /// <param name="date">The date of the day model to delete.</param>
        /// <returns>A task that represents the asynchronous delete operation.</returns>
        Task DeleteAsync(DateTime date);

        #endregion
    }
}
