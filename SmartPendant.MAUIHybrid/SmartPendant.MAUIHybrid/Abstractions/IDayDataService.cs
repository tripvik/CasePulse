using SmartPendant.MAUIHybrid.Models;

namespace SmartPendant.MAUIHybrid.Abstractions
{
    /// <summary>
    /// Provides methods for saving, retrieving, and deleting daily data models.
    /// </summary>
    public interface IDayDataService
    {
        #region Save

        /// <summary>
        /// Saves the specified <see cref="DayModel"/> to persistent storage.
        /// </summary>
        /// <param name="dayModel">The day model to save.</param>
        /// <returns>A task that represents the asynchronous save operation.</returns>
        Task SaveDayModelAsync(DayModel dayModel);

        #endregion

        #region Retrieve

        /// <summary>
        /// Retrieves a <see cref="DayModel"/> for the specified date.
        /// </summary>
        /// <param name="date">The date to retrieve data for.</param>
        /// <returns>
        /// A task that represents the asynchronous get operation. 
        /// The task result contains the <see cref="DayModel"/> if found; otherwise, <c>null</c>.
        /// </returns>
        Task<DayModel?> GetDayModelAsync(DateTime date);

        #endregion

        #region Delete

        /// <summary>
        /// Deletes the <see cref="DayModel"/> associated with the specified date.
        /// </summary>
        /// <param name="date">The date of the day model to delete.</param>
        /// <returns>A task that represents the asynchronous delete operation.</returns>
        Task DeleteDayModelAsync(DateTime date);

        #endregion
    }
}
