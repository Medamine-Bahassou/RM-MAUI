// ViewModels/ReservationDetailViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using newRestaurant.Models;
using newRestaurant.Services;
using newRestaurant.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics; // Keep for Debug.WriteLine
using System.Linq; // Add for FirstOrDefault
using System.Threading.Tasks;

namespace newRestaurant.ViewModels
{
    // Assuming BaseViewModel has IsBusy as an ObservableProperty or handles INotifyPropertyChanged
    // If not, you might need to add [ObservableProperty] private bool _isBusy; here.
    [QueryProperty(nameof(ReservationId), "ReservationId")]
    public partial class ReservationDetailViewModel : BaseViewModel
    {
        private int _reservationId;
        private bool _isInitialLoad = true;
        private int _reservationOwnerUserId = -1; // Initialize to indicate not set

        [ObservableProperty] private Table _selectedTable;
        [ObservableProperty] private ObservableCollection<Table> _tables = new();
        [ObservableProperty] private DateTime _selectedDate = DateTime.Today;
        [ObservableProperty] private TimeSpan _startTime = DateTime.Now.TimeOfDay;
        [ObservableProperty] private TimeSpan _endTime = DateTime.Now.AddHours(1).TimeOfDay;
        [ObservableProperty] private ReservationStatus _status = ReservationStatus.Pending;
        [ObservableProperty] private string _userName = "Loading...";
        [ObservableProperty] private bool _isExistingReservation;
        [ObservableProperty] private bool _isStaffOrAdmin;

        // Add NotifyCanExecuteChangedFor for IsBusy as well, if BaseViewModel doesn't handle it for this property
        // Or ensure BaseViewModel's IsBusy properly notifies changes.
        // Assuming IsBusy property is in BaseViewModel and supports notification.
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveReservationCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteReservationCommand))]
        private bool _canManageThisReservation; // This already triggers command updates

        private readonly IAuthService _authService;
        private readonly IReservationService _reservationService;
        private readonly ITableService _tableService;
        private readonly IUserService _userService; // This dependency is not used in the provided code, consider removing if truly unused.
        private readonly INavigationService _navigationService;

        // Make ReservationId notify CanExecute changes if it affects permissions *directly*
        // (Though permissions are mainly based on IsExistingReservation and OwnerId)
        public int ReservationId
        {
            get => _reservationId;
            set
            {
                if (SetProperty(ref _reservationId, value))
                {
                    IsExistingReservation = value > 0;
                    _isInitialLoad = true;
                    // No need to notify CanExecute here, as permissions depend on user/owner,
                    // which are set in InitializeAsync after ReservationId is known.
                }
            }
        }

        public ReservationDetailViewModel(
                                  IReservationService reservationService,
                                  ITableService tableService,
                                  IUserService userService, // Unused?
                                  INavigationService navigationService,
                                  IAuthService authService)
        {
            _reservationService = reservationService;
            _tableService = tableService;
            _userService = userService; // Unused?
            _navigationService = navigationService;
            _authService = authService;
            Title = "Reservation Details";

            // React to auth state changes dynamically
            _authService.PropertyChanged += AuthService_PropertyChanged;

            // Initial permission check based on role (owner ID is not known yet)
            Debug.WriteLine("VM Constructor: Calling UpdateBaseRolePermissions");
            UpdateBaseRolePermissions();

            // Initial state: cannot manage until data is loaded and permissions are checked
            CanManageThisReservation = false; // Explicitly set default state
            Debug.WriteLine($"VM Constructor: Initial CanManageThisReservation = {CanManageThisReservation}");

            // Manually trigger initial CanExecute check for commands
            // This is good practice after setting initial permission state
            SaveReservationCommand.NotifyCanExecuteChanged();
            DeleteReservationCommand.NotifyCanExecuteChanged();
        }

        // TODO: Implement IDisposable or equivalent pattern to unsubscribe
        // Consider adding this to BaseViewModel if it manages auth service subscription
        public void Dispose()
        {
            if (_authService != null)
            {
                _authService.PropertyChanged -= AuthService_PropertyChanged;
                Debug.WriteLine("Unsubscribed from AuthService.PropertyChanged");
            }
            // Optionally call base.Dispose() if BaseViewModel has one
        }


        private void AuthService_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IAuthService.CurrentUser))
            {
                Debug.WriteLine($"AuthService.CurrentUser changed. New user: {_authService.CurrentUser?.Username ?? "null"} (Role: {_authService.CurrentUser?.Role})");
                // Update base role permissions immediately
                UpdateBaseRolePermissions();
                // Re-evaluate detailed permissions based on the *current* known owner ID
                // This handles cases where user logs in/out while on the detail page
                UpdateDetailedPermissions(_reservationOwnerUserId);

                // Manually trigger CanExecute check after permission update
                SaveReservationCommand.NotifyCanExecuteChanged();
                DeleteReservationCommand.NotifyCanExecuteChanged();
            }
        }

        private void UpdateBaseRolePermissions()
        {
            var user = _authService.CurrentUser;
            bool oldIsStaffOrAdmin = IsStaffOrAdmin;
            IsStaffOrAdmin = user != null && (user.Role == UserRole.Staff || user.Role == UserRole.Admin);
            Debug.WriteLine($"UpdateBaseRolePermissions: User null? {user == null}, Role: {user?.Role}, IsStaffOrAdmin: {IsStaffOrAdmin}");
            if (oldIsStaffOrAdmin != IsStaffOrAdmin)
            {
                // If IsStaffOrAdmin changes, it affects CanManageThisReservation, which is notified
                // by its own attribute. No extra notification needed here just for IsStaffOrAdmin.
            }
        }

        private void UpdateDetailedPermissions(int ownerUserId)
        {
            var currentUser = _authService.CurrentUser;
            bool oldCanManage = CanManageThisReservation;

            if (currentUser == null)
            {
                CanManageThisReservation = false;
                Debug.WriteLine("UpdateDetailedPermissions: CurrentUser is null. CanManageThisReservation = false.");
            }
            else
            {
                // Determine if current user is the owner of THIS reservation
                bool isOwner = (ownerUserId > 0 && currentUser.Id == ownerUserId);

                CanManageThisReservation = IsStaffOrAdmin || isOwner;
                Debug.WriteLine($"UpdateDetailedPermissions: IsStaffOrAdmin={IsStaffOrAdmin}, IsOwner={isOwner} (CurrentUser ID={currentUser.Id}, Owner ID={ownerUserId}). CanManageThisReservation={CanManageThisReservation}");
            }

            // The [NotifyCanExecuteChangedFor] on CanManageThisReservation should handle this,
            // but explicit calls here add robustness and make it clear.
            if (oldCanManage != CanManageThisReservation)
            {
                Debug.WriteLine($"CanManageThisReservation changed from {oldCanManage} to {CanManageThisReservation}. Notifying commands.");
                SaveReservationCommand.NotifyCanExecuteChanged();
                DeleteReservationCommand.NotifyCanExecuteChanged();
            }
        }


        public async Task InitializeAsync()
        {
            // Added check for IsBusy at the start to prevent re-entry while busy
            if (IsBusy || !_isInitialLoad) return;

            Debug.WriteLine($"InitializeAsync started. ReservationId: {_reservationId}, IsInitialLoad: {_isInitialLoad}");
            IsBusy = true;
            CanManageThisReservation = false; // Reset state during load
            Debug.WriteLine($"InitializeAsync: IsBusy = true, CanManageThisReservation = false. Notifying commands.");
            SaveReservationCommand.NotifyCanExecuteChanged();
            DeleteReservationCommand.NotifyCanExecuteChanged();


            try
            {
                // Fetch tables first
                Tables.Clear();
                var tableList = await _tableService.GetTablesAsync();
                if (tableList != null)
                {
                    foreach (var table in tableList) Tables.Add(table);
                    Debug.WriteLine($"Fetched {Tables.Count} tables.");
                }
                else
                {
                    Debug.WriteLine("Failed to fetch tables.");
                    await Shell.Current.DisplayAlert("Error", "Failed to load tables.", "OK");
                    // Decide if you should return or continue without tables (probably return)
                    // return; // If tables are essential, uncomment this line
                }


                if (_reservationId > 0) // Existing Reservation
                {
                    Debug.WriteLine($"Loading existing reservation with ID: {_reservationId}");
                    var reservation = await _reservationService.GetReservationAsync(_reservationId);

                    if (reservation != null)
                    {
                        Debug.WriteLine($"Reservation loaded. User ID: {reservation.UserId}, Table ID: {reservation.TableId}");
                        _reservationOwnerUserId = reservation.UserId;
                        // Update permissions *after* getting the owner ID
                        UpdateDetailedPermissions(_reservationOwnerUserId);

                        SelectedTable = Tables.FirstOrDefault(t => t.Id == reservation.TableId);
                        if (SelectedTable == null) Debug.WriteLine($"Warning: Table with ID {reservation.TableId} not found among fetched tables.");

                        SelectedDate = reservation.TimeStart.Date;
                        StartTime = reservation.TimeStart.TimeOfDay;
                        EndTime = reservation.TimeEnd.TimeOfDay;
                        Status = reservation.Status;
                        UserName = reservation.User?.Username ?? "Unknown";
                        IsExistingReservation = true;
                        Title = CanManageThisReservation ? "Edit Reservation" : "View Reservation";
                        Debug.WriteLine($"Existing reservation loaded. Title: {Title}, CanManageThisReservation: {CanManageThisReservation}");
                    }
                    else
                    {
                        Debug.WriteLine($"Reservation with ID {_reservationId} not found.");
                        await Shell.Current.DisplayAlert("Error", "Reservation not found.", "OK");
                        // If reservation not found, user cannot manage it. Permissions are already false.
                        // Navigate back after informing the user.
                        await GoBackAsync();
                        return; // Exit initialization after navigating back
                    }
                }
                else // New Reservation
                {
                    Debug.WriteLine("Initializing for new reservation.");
                    var currentUser = _authService.CurrentUser;
                    if (currentUser == null)
                    {
                        Debug.WriteLine("Error: Cannot create new reservation, user not logged in.");
                        await Shell.Current.DisplayAlert("Error", "You must be logged in to create a reservation.", "OK");
                        await GoBackAsync();
                        return; // Exit initialization after navigating back
                    }

                    _reservationOwnerUserId = currentUser.Id;
                    // Update permissions immediately for new reservation (owner is current user)
                    UpdateDetailedPermissions(_reservationOwnerUserId);

                    Title = "Add New Reservation";
                    SelectedTable = Tables.FirstOrDefault(); // Default to first table if available
                    if (SelectedTable == null) Debug.WriteLine("Warning: No tables available to pre-select for new reservation.");

                    // Sensible defaults for new reservation time
                    SelectedDate = DateTime.Today.AddDays(1); // Default to tomorrow
                    StartTime = TimeSpan.FromHours(18);      // Default to 6:00 PM
                    EndTime = StartTime.Add(TimeSpan.FromHours(2)); // Default to 2 hours later

                    Status = ReservationStatus.Pending; // New reservations always start as Pending
                    IsExistingReservation = false;
                    UserName = currentUser.Username;
                    Debug.WriteLine($"New reservation initialized for user: {UserName}. CanManageThisReservation: {CanManageThisReservation}");

                    // Explicitly check if the current user is allowed to *create* reservations
                    // Based on your logic, UpdateDetailedPermissions already did this.
                    // If UpdateDetailedPermissions set CanManageThisReservation to false for a new reservation,
                    // it means the current user ID was <= 0, which was already checked above.
                    // This check below is redundant if the above null check is sufficient, but keeping
                    // it for clarity based on your original code's intent.
                    if (!CanManageThisReservation)
                    {
                        Debug.WriteLine("Error: CanManageThisReservation is false for new reservation despite being logged in. (Should not happen based on UpdateDetailedPermissions logic if user is logged in with Id > 0)");
                        // This scenario should ideally not happen if the user is logged in with a valid ID > 0
                        // and UpdateDetailedPermissions logic is correct.
                        // Re-checking here might indicate an issue with the user object from AuthService.
                        await Shell.Current.DisplayAlert("Error", "You do not have permission to create a reservation.", "OK");
                        await GoBackAsync();
                        return; // Exit initialization
                    }
                }
                _isInitialLoad = false; // Mark initialization complete
                Debug.WriteLine("InitializeAsync completed successfully.");
            }
            catch (Exception ex)
            {
                // Log the exception properly
                Debug.WriteLine($"FATAL ERROR during ReservationDetailViewModel InitializeAsync: {ex}");
                // Consider displaying a more informative error to the user or logging service
                await Shell.Current.DisplayAlert("Error", "An error occurred while loading reservation details.", "OK");

                // Decide if you should navigate back on error or leave user on blank page
                // await GoBackAsync(); // Uncomment if you want to go back on *any* load error
            }
            finally
            {
                IsBusy = false; // Ensure IsBusy is always set to false when initialization ends
                Debug.WriteLine($"InitializeAsync finally block: IsBusy = false. Notifying commands.");
                // Explicitly notify commands that IsBusy state has changed
                SaveReservationCommand.NotifyCanExecuteChanged();
                DeleteReservationCommand.NotifyCanExecuteChanged();
                // Also notify GoBackCommand if its CanExecute depends on IsBusy
                GoBackCommand.NotifyCanExecuteChanged();

                // After initialization and setting IsBusy=false, force a final permission re-check
                // to ensure CanManageThisReservation is correct given the final loaded state
                // and IsBusy state is now false. This can help resolve potential timing issues.
                // Although UpdateDetailedPermissions was called during load, calling it again now
                // that IsBusy is false ensures the commands re-evaluate under the final state.
                UpdateDetailedPermissions(_reservationOwnerUserId); // This already notifies commands
            }
        }

        // CanSave logic remains the same, it relies on the properties
        private bool CanSave()
        {
            // Add debugging output here to see why it's false
            bool canSave = CanManageThisReservation && !IsBusy;
            Debug.WriteLine($"CanSave(): CanManageThisReservation={CanManageThisReservation}, IsBusy={IsBusy}. Result: {canSave}");
            return canSave;
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveReservationAsync()
        {
            // Double-check permission before starting the async operation
            if (!CanManageThisReservation)
            {
                Debug.WriteLine("SaveReservationAsync attempted without CanManageThisReservation permission.");
                await Shell.Current.DisplayAlert("Permission Denied", "You do not have permission to save this reservation.", "OK");
                return; // Prevent execution if permission is lost somehow between CanExecute and execution
            }

            Debug.WriteLine("SaveReservationAsync started.");
            var currentUserId = _authService.CurrentUser?.Id;
            int userIdToSave = IsExistingReservation ? _reservationOwnerUserId : currentUserId ?? -1;

            if (userIdToSave <= 0)
            {
                Debug.WriteLine("SaveReservationAsync: User ID to save is invalid.");
                await Shell.Current.DisplayAlert("Error", "User information is missing for saving.", "OK");
                return;
            }

            DateTime startDateTime = SelectedDate.Date + StartTime;
            DateTime endDateTime = SelectedDate.Date + EndTime;

            // More detailed validation messages
            if (SelectedTable == null)
            {
                await Shell.Current.DisplayAlert("Validation Error", "Please select a table.", "OK");
                return;
            }
            if (endDateTime <= startDateTime)
            {
                await Shell.Current.DisplayAlert("Validation Error", "End time must be after start time.", "OK");
                return;
            }
            if (!IsExistingReservation && startDateTime < DateTime.Now)
            {
                await Shell.Current.DisplayAlert("Validation Error", "Cannot create a new reservation in the past.", "OK");
                return;
            }
            Debug.WriteLine($"Validation passed. Saving reservation for Table ID: {SelectedTable.Id}, User ID: {userIdToSave}, Start: {startDateTime}, End: {endDateTime}");


            IsBusy = true; // Set IsBusy BEFORE the async operation
            Debug.WriteLine($"SaveReservationAsync: IsBusy = true. Notifying commands.");
            SaveReservationCommand.NotifyCanExecuteChanged();
            DeleteReservationCommand.NotifyCanExecuteChanged(); // Also disable delete while saving


            bool success = false;
            try
            {
                // Determine the status to save
                ReservationStatus statusToSave;
                if (IsExistingReservation && IsStaffOrAdmin)
                {
                    // Staff/Admin can change status
                    statusToSave = Status;
                    Debug.WriteLine($"Saving existing reservation as Staff/Admin. Status: {statusToSave}");
                }
                else if (IsExistingReservation)
                {
                    // Regular user viewing existing reservation: Keep original status
                    // Need to refetch status if the ViewModel's Status might have been changed in UI
                    // (Which it shouldn't be if CanManageThisReservation is false)
                    // A safer approach might be to load the original reservation's status here
                    var originalReservation = await _reservationService.GetReservationAsync(_reservationId);
                    statusToSave = originalReservation?.Status ?? Status; // Fallback to current VM status if refetch fails
                    Debug.WriteLine($"Saving existing reservation as non-Staff/Admin. Keeping original status: {statusToSave}");
                }
                else
                {
                    // New reservation always starts as Pending
                    statusToSave = ReservationStatus.Pending;
                    Debug.WriteLine($"Saving new reservation. Status: {statusToSave}");
                }


                Reservation reservationToSave = new Reservation
                {
                    Id = _reservationId, // Will be 0 for new, actual ID for existing
                    TableId = SelectedTable.Id,
                    UserId = userIdToSave,
                    TimeStart = startDateTime,
                    TimeEnd = endDateTime,
                    Status = statusToSave // Use the determined status
                };

                if (IsExistingReservation)
                {
                    Debug.WriteLine("Calling UpdateReservationAsync...");
                    success = await _reservationService.UpdateReservationAsync(reservationToSave);
                    Debug.WriteLine($"UpdateReservationAsync result: {success}");
                }
                else
                {
                    Debug.WriteLine("Calling AddReservationAsync...");
                    success = await _reservationService.AddReservationAsync(reservationToSave);
                    Debug.WriteLine($"AddReservationAsync result: {success}");
                }

                if (success)
                {
                    await Shell.Current.DisplayAlert("Success", "Reservation saved.", "OK");
                    await GoBackAsync();
                }
                else
                {
                    // Provide more specific feedback if possible (e.g., from service)
                    await Shell.Current.DisplayAlert("Error", "Failed to save reservation. Please check for time overlaps or other issues.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FATAL ERROR saving reservation: {ex}");
                await Shell.Current.DisplayAlert("Error", "An unexpected error occurred while saving the reservation.", "OK");
            }
            finally
            {
                IsBusy = false; // Ensure IsBusy is false after operation finishes
                Debug.WriteLine($"SaveReservationAsync finally block: IsBusy = false. Notifying commands.");
                // Explicitly notify commands
                SaveReservationCommand.NotifyCanExecuteChanged();
                DeleteReservationCommand.NotifyCanExecuteChanged();
                GoBackCommand.NotifyCanExecuteChanged();
                // Re-evaluate permissions just in case (e.g., role changed during save)
                UpdateDetailedPermissions(_reservationOwnerUserId); // This already notifies commands
            }
        }

        // CanDelete logic remains the same, it relies on properties
        private bool CanDelete()
        {
            bool canDelete = CanManageThisReservation && IsExistingReservation && !IsBusy;
            Debug.WriteLine($"CanDelete(): CanManageThisReservation={CanManageThisReservation}, IsExistingReservation={IsExistingReservation}, IsBusy={IsBusy}. Result: {canDelete}");
            return canDelete;
        }


        [RelayCommand(CanExecute = nameof(CanDelete))]
        private async Task DeleteReservationAsync()
        {
            // Double-check permission
            if (!CanManageThisReservation || !IsExistingReservation)
            {
                Debug.WriteLine("DeleteReservationAsync attempted without CanManageThisReservation or IsExistingReservation permission.");
                await Shell.Current.DisplayAlert("Permission Denied", "You do not have permission to delete this reservation.", "OK");
                return;
            }
            Debug.WriteLine($"DeleteReservationAsync started for ID: {_reservationId}");

            bool confirm = await Shell.Current.DisplayAlert("Confirm Delete", "Are you sure you want to delete this reservation?", "Yes", "No");
            if (!confirm)
            {
                Debug.WriteLine("Delete confirmation declined.");
                return;
            }

            IsBusy = true; // Set IsBusy BEFORE the async operation
            Debug.WriteLine($"DeleteReservationAsync: IsBusy = true. Notifying commands.");
            SaveReservationCommand.NotifyCanExecuteChanged(); // Disable save while deleting
            DeleteReservationCommand.NotifyCanExecuteChanged();


            bool success = false;
            try
            {
                Debug.WriteLine($"Calling DeleteReservationAsync for ID: {_reservationId}...");
                success = await _reservationService.DeleteReservationAsync(_reservationId);
                Debug.WriteLine($"DeleteReservationAsync result: {success}");

                if (success)
                {
                    await Shell.Current.DisplayAlert("Success", "Reservation deleted.", "OK");
                    await GoBackAsync();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", "Failed to delete reservation.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FATAL ERROR deleting reservation: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "An unexpected error occurred while deleting the reservation.", "OK");
            }
            finally
            {
                IsBusy = false; // Ensure IsBusy is false
                Debug.WriteLine($"DeleteReservationAsync finally block: IsBusy = false. Notifying commands.");
                // Explicitly notify commands
                SaveReservationCommand.NotifyCanExecuteChanged();
                DeleteReservationCommand.NotifyCanExecuteChanged();
                GoBackCommand.NotifyCanExecuteChanged();
                // Re-evaluate permissions
                UpdateDetailedPermissions(_reservationOwnerUserId); // This already notifies commands
            }
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            // Add CanExecute logic if needed, e.g., !IsBusy
            // if (IsBusy) return; // Already checked at start of method, but good to have here too
            Debug.WriteLine("Navigating back...");
            await _navigationService.GoBackAsync();
        }

        // TODO: Unsubscribe _authService.PropertyChanged -= AuthService_PropertyChanged;
        // Implement IDisposable and call Dispose() when the VM is no longer needed (e.g., in page's OnDisappearing)
    }
}