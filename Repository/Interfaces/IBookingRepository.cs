using CommonLibrary.Domain.Entities;
using CommonLibrary.Integrations.Model;
using ServicePortal.Domain.PSQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Repository.Interfaces
{
    public interface IBookingRepository
    {
        Task<DoOperationResponse<Booking>> SaveBooking(Booking data);
        Task<BookingViewModel> GetBooking(Guid id);
        Task<DoOperationResponse<Booking>> UpdateBooking(Object model);
        Task<string> GetBookingReason(Guid id);
    }
}
