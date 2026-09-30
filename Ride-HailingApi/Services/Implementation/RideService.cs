using Microsoft.EntityFrameworkCore;
using Ride_HailingApi.DTOs.Ride;
using Ride_HailingApi.Entities;
using Ride_HailingApi.Enums;
using Ride_HailingApi.Repositories.Interface;
using Ride_HailingApi.Services.Interface;
using Ride_HailingApi.Utils;
using RideHailingApi.DTOs.Common;

namespace Ride_HailingApi.Services.Implementation;

public class RideService : IRideService
{
    private readonly IRideRepository _rideRepository;
    private readonly IDriverRepository _driverRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly IAuditService _auditService;
    private readonly ILogger<RideService> _logger;
    private static readonly Random Rng = new();

    private const string ConcurrencyMessage =
        "This ride was just updated by another user or driver. Please refresh and try again.";

    // Statuses from which a passenger is still allowed to cancel.
    private static readonly HashSet<RideStatus> PassengerCancellableStatuses = new()
    {
        RideStatus.Requested, RideStatus.Accepted, RideStatus.DriverArriving, RideStatus.DriverArrived
    };

    // Forward-only lifecycle for driver-controlled status updates (post-acceptance).
    private static readonly Dictionary<RideStatus, RideStatus[]> DriverTransitions = new()
    {
        [RideStatus.Accepted] = new[] { RideStatus.DriverArriving, RideStatus.Cancelled },
        [RideStatus.DriverArriving] = new[] { RideStatus.DriverArrived, RideStatus.Cancelled },
        [RideStatus.DriverArrived] = new[] { RideStatus.InProgress, RideStatus.Cancelled },
        [RideStatus.InProgress] = new[] { RideStatus.Completed },
    };

    public RideService(IRideRepository rideRepository, IDriverRepository driverRepository,
        IUserRepository userRepository, IEmailService emailService, ISmsService smsService,
        IAuditService auditService, ILogger<RideService> logger)
    {
        _rideRepository = rideRepository;
        _driverRepository = driverRepository;
        _userRepository = userRepository;
        _emailService = emailService;
        _smsService = smsService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ApiResponse<RideResponse>> CreateRideAsync(int passengerId, CreateRideRequest request)
    {
        var passenger = await _userRepository.GetByIdAsync(passengerId);
        if (passenger is null || passenger.Role != UserRole.Passenger)
        {
            _logger.LogWarning("Ride request rejected: user {UserId} is not a passenger account.", passengerId);
            return ApiResponse<RideResponse>.FailResponse("Only passenger accounts can request rides.");
        }

        var reference = await GenerateUniqueReferenceAsync();

        var ride = new Ride
        {
            RideReference = reference,
            PassengerId = passengerId,
            PickupLocation = request.PickupLocation,
            Destination = request.Destination, 
            Status = RideStatus.Requested,
            EstimatedFare = 500m + Rng.Next(200, 2000),
            RequestedAtUtc = DateTime.UtcNow
        };
        ride.StatusHistories.Add(new RideStatusHistory
        {
            FromStatus = RideStatus.Requested,
            ToStatus = RideStatus.Requested,
            ChangedByUserId = passengerId,
            Notes = "Ride requested"
        });

        await _rideRepository.AddAsync(ride);
        await _rideRepository.SaveChangesAsync();

        _logger.LogInformation("Passenger {PassengerId} requested ride {RideReference}.", passengerId, reference);
        await _auditService.LogAsync(passengerId, UserRole.Passenger.ToString(), "RideCreated", "Ride",
            ride.Id.ToString(), $"Ride {reference} requested from '{request.PickupLocation}' to '{request.Destination}'");

        var full = await _rideRepository.GetByIdWithDetailsAsync(ride.Id);
        return ApiResponse<RideResponse>.SuccessResponse(RideMapper.MapToResponse(full!), "Ride requested successfully.");
    }

    public async Task<ApiResponse<IEnumerable<RideResponse>>> GetMyRidesAsync(int passengerId)
    {
        var rides = await _rideRepository.GetByPassengerIdAsync(passengerId);
        return ApiResponse<IEnumerable<RideResponse>>.SuccessResponse(rides.Select(RideMapper.MapToResponse));
    }

    public async Task<ApiResponse<RideResponse>> GetRideByIdAsync(int rideId, int requestingUserId, UserRole requestingUserRole)
    {
        var ride = await _rideRepository.GetByIdWithDetailsAsync(rideId);
        if (ride is null)
        {
            return ApiResponse<RideResponse>.FailResponse("Ride not found.");
        }

        if (requestingUserRole == UserRole.Passenger && ride.PassengerId != requestingUserId)
        {
            _logger.LogWarning("Access denied: passenger {UserId} tried to view ride {RideId} owned by another passenger.", requestingUserId, rideId);
            return ApiResponse<RideResponse>.FailResponse("You do not have permission to view this ride.");
        }

        if (requestingUserRole == UserRole.Driver)
        {
            var driverProfile = await _driverRepository.GetByUserIdAsync(requestingUserId);
            if (driverProfile is null || ride.DriverProfileId != driverProfile.Id)
            {
                _logger.LogWarning("Access denied: driver {UserId} tried to view ride {RideId} not assigned to them.", requestingUserId, rideId);
                return ApiResponse<RideResponse>.FailResponse("You do not have permission to view this ride.");
            }
        }

        return ApiResponse<RideResponse>.SuccessResponse(RideMapper.MapToResponse(ride));
    }

    public async Task<ApiResponse<string>> CancelRideAsync(int rideId, int passengerId, CancelRideRequest request)
    {
        var ride = await _rideRepository.GetByIdWithDetailsAsync(rideId);
        if (ride is null)
        {
            return ApiResponse<string>.FailResponse("Ride not found.");
        }

        if (ride.PassengerId != passengerId)
        {
            _logger.LogWarning("Access denied: passenger {UserId} tried to cancel ride {RideId} owned by another passenger.", passengerId, rideId);
            return ApiResponse<string>.FailResponse("You do not have permission to cancel this ride.");
        }

        if (!PassengerCancellableStatuses.Contains(ride.Status))
        {
            _logger.LogWarning("Cancel rejected for ride {RideReference}: status {Status} is not cancellable.", ride.RideReference, ride.Status);
            return ApiResponse<string>.FailResponse($"A ride in '{ride.Status}' status can no longer be cancelled.");
        }

        var previousStatus = ride.Status;
        ride.Status = RideStatus.Cancelled;
        ride.CancellationReason = request.Reason;
        ride.CancelledByUserId = passengerId;
        ride.CancelledAtUtc = DateTime.UtcNow;
        ride.StatusHistories.Add(new RideStatusHistory
        {
            FromStatus = previousStatus,
            ToStatus = RideStatus.Cancelled,
            ChangedByUserId = passengerId,
            Notes = request.Reason
        });

        _rideRepository.Update(ride);
        if (!await TrySaveRideAsync(ride, "cancel"))
        {
            return ApiResponse<string>.FailResponse(ConcurrencyMessage);
        }

        _logger.LogInformation("Ride {RideReference} cancelled by passenger {PassengerId} (was {PreviousStatus}).", ride.RideReference, passengerId, previousStatus);

        await _auditService.LogAsync(passengerId, UserRole.Passenger.ToString(), "RideCancelled", "Ride",
            ride.Id.ToString(), $"Cancelled by passenger. Reason: {request.Reason}");

        await NotifyRideCancelledAsync(ride);

        return ApiResponse<string>.SuccessResponse("Ride cancelled successfully.");
    }

    public async Task<ApiResponse<RideResponse>> AcceptRideAsync(int rideId, int driverUserId)
    {
        var driverProfile = await _driverRepository.GetByUserIdAsync(driverUserId);
        if (driverProfile is null)
        {
            return ApiResponse<RideResponse>.FailResponse("Driver profile not found.");
        }

        if (driverProfile.ApprovalStatus != DriverApprovalStatus.Approved || !driverProfile.IsAvailable)
        {
            _logger.LogWarning("Accept rejected: driver {UserId} is not approved and available.", driverUserId);
            return ApiResponse<RideResponse>.FailResponse("You must be an approved and available driver to accept rides.");
        }

        var ride = await _rideRepository.GetByIdWithDetailsAsync(rideId);
        if (ride is null)
        {
            return ApiResponse<RideResponse>.FailResponse("Ride not found.");
        }

        if (ride.Status != RideStatus.Requested || ride.DriverProfileId != null)
        {
            _logger.LogWarning("Accept rejected for ride {RideReference}: no longer available (status {Status}).", ride.RideReference, ride.Status);
            return ApiResponse<RideResponse>.FailResponse("This ride is no longer available for acceptance.");
        }

        ride.DriverProfileId = driverProfile.Id;
        ride.Status = RideStatus.Accepted;
        ride.AcceptedAtUtc = DateTime.UtcNow;
        ride.StatusHistories.Add(new RideStatusHistory
        {
            FromStatus = RideStatus.Requested,
            ToStatus = RideStatus.Accepted,
            ChangedByUserId = driverUserId,
            Notes = "Ride accepted by driver"
        });

        _rideRepository.Update(ride);
        if (!await TrySaveRideAsync(ride, "accept"))
        {
            // Another driver accepted (or the passenger cancelled) between our read and our save.
            return ApiResponse<RideResponse>.FailResponse("This ride is no longer available for acceptance.");
        }

        _logger.LogInformation("Ride {RideReference} accepted by driver {DriverProfileId}.", ride.RideReference, driverProfile.Id);

        await _auditService.LogAsync(driverUserId, UserRole.Driver.ToString(), "RideAccepted", "Ride",
            ride.Id.ToString());

        var full = await _rideRepository.GetByIdWithDetailsAsync(ride.Id);
        var vehicle = driverProfile.Vehicle;
        var vehicleInfo = vehicle is null ? "N/A" : $"{vehicle.Color} {vehicle.Make} {vehicle.Model}";
        var plate = vehicle?.PlateNumber ?? "N/A";

        await _emailService.SendRideAcceptedByDriverAsync(
            full!.Passenger.Email,
            full.Passenger.FullName,
            driverProfile.User.FullName,
            vehicleInfo,
            plate,
            full.RideReference);
        await _smsService.SendSmsAsync(full.PassengerId, full.Passenger.PhoneNumber,
            SmsUtils.RideAcceptedByDriver(driverProfile.User.FullName, plate, full.RideReference));

        return ApiResponse<RideResponse>.SuccessResponse(RideMapper.MapToResponse(full), "Ride accepted successfully.");
    }

    public async Task<ApiResponse<RideResponse>> UpdateRideStatusAsync(int rideId, int driverUserId, RideStatusUpdateRequest request)
    {
        var driverProfile = await _driverRepository.GetByUserIdAsync(driverUserId);
        if (driverProfile is null)
        {
            return ApiResponse<RideResponse>.FailResponse("Driver profile not found.");
        }

        var ride = await _rideRepository.GetByIdWithDetailsAsync(rideId);
        if (ride is null)
        {
            return ApiResponse<RideResponse>.FailResponse("Ride not found.");
        }

        if (ride.DriverProfileId != driverProfile.Id)
        {
            _logger.LogWarning("Access denied: driver {UserId} tried to update ride {RideId} assigned to another driver.", driverUserId, rideId);
            return ApiResponse<RideResponse>.FailResponse("You are not assigned to this ride.");
        }

        if (!DriverTransitions.TryGetValue(ride.Status, out var allowedNext) || !allowedNext.Contains(request.NewStatus))
        {
            _logger.LogWarning("Invalid transition for ride {RideReference}: {From} -> {To} (driver {UserId}).", ride.RideReference, ride.Status, request.NewStatus, driverUserId);
            return ApiResponse<RideResponse>.FailResponse(
                $"Cannot move a ride from '{ride.Status}' to '{request.NewStatus}'.");
        }

        var previousStatus = ride.Status;
        ride.Status = request.NewStatus;

        switch (request.NewStatus)
        {
            case RideStatus.Completed:
                ride.CompletedAtUtc = DateTime.UtcNow;
                break;
            case RideStatus.Cancelled:
                ride.CancelledAtUtc = DateTime.UtcNow;
                ride.CancelledByUserId = driverUserId;
                ride.CancellationReason = request.Notes ?? "Cancelled by driver";
                break;
        }

        ride.StatusHistories.Add(new RideStatusHistory
        {
            FromStatus = previousStatus,
            ToStatus = request.NewStatus,
            ChangedByUserId = driverUserId,
            Notes = request.Notes
        });

        _rideRepository.Update(ride);
        if (!await TrySaveRideAsync(ride, "update status of"))
        {
            return ApiResponse<RideResponse>.FailResponse(ConcurrencyMessage);
        }

        _logger.LogInformation("Ride {RideReference} moved {From} -> {To} by driver {UserId}.", ride.RideReference, previousStatus, request.NewStatus, driverUserId);

        await _auditService.LogAsync(driverUserId, UserRole.Driver.ToString(), "RideStatusChanged", "Ride",
            ride.Id.ToString(), $"{previousStatus} -> {request.NewStatus}");

        var full = await _rideRepository.GetByIdWithDetailsAsync(ride.Id);

        if (request.NewStatus == RideStatus.DriverArrived)
        {
            await _emailService.SendDriverArrivedAsync(full!.Passenger.Email, full.Passenger.FullName, full.RideReference);
            await _smsService.SendSmsAsync(full.PassengerId, full.Passenger.PhoneNumber,
                SmsUtils.DriverArrived(full.RideReference));
        }
        else if (request.NewStatus == RideStatus.Completed)
        {
            await _emailService.SendRideCompletedAsync(full!.Passenger.Email, full.Passenger.FullName, full.RideReference, full.EstimatedFare);
            await _smsService.SendSmsAsync(full.PassengerId, full.Passenger.PhoneNumber,
                SmsUtils.RideCompleted(full.RideReference));
        }
        else if (request.NewStatus == RideStatus.Cancelled)
        {
            await NotifyRideCancelledAsync(full!);
        }

        return ApiResponse<RideResponse>.SuccessResponse(RideMapper.MapToResponse(full!), "Ride status updated successfully.");
    }

    public async Task<ApiResponse<IEnumerable<RideResponse>>> GetRidesForDriverAsync(int driverUserId)
    {
        var driverProfile = await _driverRepository.GetByUserIdAsync(driverUserId);
        if (driverProfile is null)
        {
            return ApiResponse<IEnumerable<RideResponse>>.FailResponse("Driver profile not found.");
        }

        var rides = await _rideRepository.GetByDriverProfileIdAsync(driverProfile.Id);
        return ApiResponse<IEnumerable<RideResponse>>.SuccessResponse(rides.Select(RideMapper.MapToResponse));
    }

    public async Task<ApiResponse<IEnumerable<RideResponse>>> GetAllRidesAsync()
    {
        var rides = await _rideRepository.GetAllAsync();
        return ApiResponse<IEnumerable<RideResponse>>.SuccessResponse(rides.Select(RideMapper.MapToResponse));
    }

    // ---------- Private helpers ----------

    /// <summary>
    /// Saves a ride change. Returns false if another request changed the same ride first
    /// (optimistic concurrency via the RowVersion column), so callers can respond cleanly.
    /// </summary>
    private async Task<bool> TrySaveRideAsync(Ride ride, string operation)
    {
        try
        {
            await _rideRepository.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict while trying to {Operation} ride {RideReference}: it was changed by another request.", operation, ride.RideReference);
            return false;
        }
    }

    private async Task<string> GenerateUniqueReferenceAsync()
    {
        string reference;
        do
        {
            reference = $"RD-{DateTime.UtcNow:yyyyMMdd}-{Rng.Next(1000, 9999)}";
        } while (await _rideRepository.ReferenceExistsAsync(reference));

        return reference;
    }

    private async Task NotifyRideCancelledAsync(Ride ride)
    {
        var reason = ride.CancellationReason ?? "Not specified";

        await _emailService.SendRideCancelledAsync(ride.Passenger.Email, ride.Passenger.FullName, ride.RideReference, reason);
        await _smsService.SendSmsAsync(ride.PassengerId, ride.Passenger.PhoneNumber,
            SmsUtils.RideCancelled(ride.RideReference, reason));

        if (ride.DriverProfile is not null)
        {
            var driverUser = ride.DriverProfile.User;
            await _emailService.SendRideCancelledAsync(driverUser.Email, driverUser.FullName, ride.RideReference, reason);
            await _smsService.SendSmsAsync(driverUser.Id, driverUser.PhoneNumber,
                SmsUtils.RideCancelled(ride.RideReference, reason));
        }
    }
}
