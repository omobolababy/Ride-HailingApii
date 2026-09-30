using Ride_HailingApi.DTOs.Ride;
using Ride_HailingApi.Entities;

namespace Ride_HailingApi.Services.Implementation;

public static class RideMapper
{
    public static RideResponse MapToResponse(Ride r) => new()
    {
        Id = r.Id,
        RideReference = r.RideReference,
        PassengerId = r.PassengerId,
        PassengerName = r.Passenger?.FullName ?? string.Empty,
        DriverProfileId = r.DriverProfileId,
        DriverName = r.DriverProfile?.User?.FullName,
        PickupLocation = r.PickupLocation,
        Destination = r.Destination,
        Status = r.Status.ToString(),
        EstimatedFare = r.EstimatedFare,
        RequestedAtUtc = r.RequestedAtUtc,
        AcceptedAtUtc = r.AcceptedAtUtc,
        CompletedAtUtc = r.CompletedAtUtc,
        CancelledAtUtc = r.CancelledAtUtc,
        CancellationReason = r.CancellationReason
    };
}