// ViewModels/UserProfileViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using newRestaurant.Models;
using newRestaurant.Services.Interfaces;
using newRestaurant.Services;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui.Controls; // For Shell

namespace newRestaurant.ViewModels
{
    public partial class UserProfileViewModel : BaseViewModel // Assuming BaseViewModel exists
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly INavigationService _navigationService; // Maybe needed?

        private User _originalUser; // To compare changes

        // Bindable properties for the UI
        [ObservableProperty]
        
        private string _username;

        [ObservableProperty]
        
        private string _email;

        [ObservableProperty]
        private string _userRoleDisplay; // Display role name

        [ObservableProperty]
        private string _errorMessage;

        [ObservableProperty]
        private bool _hasError;

        public UserProfileViewModel(
            IAuthService authService,
            IUserService userService,
            INavigationService navigationService)
        {
            _authService = authService;
            _userService = userService;
            _navigationService = navigationService;
            Title = "My Profile";
            _authService.PropertyChanged += AuthService_PropertyChanged;
        }

        private void AuthService_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Reload profile if the logged-in user changes (e.g., after logout/login)
            if (e.PropertyName == nameof(IAuthService.CurrentUser))
            {
                LoadUserProfile();
            }
        }

        // Call this from page OnAppearing
        public void LoadUserProfile()
        {
            if (IsBusy) return; // Prevent multiple loads if already busy

            var currentUser = _authService.CurrentUser;
            if (currentUser == null)
            {
                // Should not happen if page is protected, but handle defensively
                Username = string.Empty;
                Email = string.Empty;
                UserRoleDisplay = "Not Logged In";
                _originalUser = null;
                HasError = true;
                ErrorMessage = "User not logged in.";
                SaveProfileCommand.NotifyCanExecuteChanged(); // Update save button state
                return;
            }

            IsBusy = true; // Use IsBusy flag if loading involves async operations
            HasError = false;
            ErrorMessage = string.Empty;

            try
            {
                // Store the original for comparison on save
                _originalUser = currentUser; // Assuming AuthService holds the latest User object

                // Set bindable properties from the current user
                Username = currentUser.Username;
                Email = currentUser.Email;
                UserRoleDisplay = currentUser.Role.ToString(); // Display the role name

                // Update CanExecute state based on loaded data
                SaveProfileCommand.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading profile: {ex.Message}");
                HasError = true;
                ErrorMessage = "Failed to load profile.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanSaveProfile()
        {
            if (IsBusy || _originalUser == null) return false;

            // Check if there are actual changes
            bool hasChanges = (_originalUser.Username != Username?.Trim() ||
                               _originalUser.Email != Email?.Trim().ToLower());

            // Check for basic validation
            bool isValid = !string.IsNullOrWhiteSpace(Username) &&
                           !string.IsNullOrWhiteSpace(Email) &&
                           IsValidEmailFormat(Email); // Add simple email check

            return hasChanges && isValid;
        }

        [RelayCommand(CanExecute = nameof(CanSaveProfile))]
        private async Task SaveProfileAsync()
        {
            if (!CanSaveProfile()) return; // Guard

            // More robust validation before saving
            if (string.IsNullOrWhiteSpace(Username) || Username.Trim().Length < 3)
            {
                await Shell.Current.DisplayAlert("Validation Error", "Username must be at least 3 characters.", "OK");
                return;
            }
            if (string.IsNullOrWhiteSpace(Email) || !IsValidEmailFormat(Email))
            {
                await Shell.Current.DisplayAlert("Validation Error", "Please enter a valid email address.", "OK");
                return;
            }

            // Check email uniqueness if it changed
            if (_originalUser.Email != Email.Trim().ToLower())
            {
                bool emailExists = await _userService.DoesEmailExistForAnotherUserAsync(Email.Trim().ToLower(), _originalUser.Id);
                if (emailExists)
                {
                    await Shell.Current.DisplayAlert("Validation Error", "This email address is already in use by another account.", "OK");
                    return;
                }
            }

            IsBusy = true;
            HasError = false;
            ErrorMessage = string.Empty;

            try
            {
                // Create a temporary user object with updated values
                // IMPORTANT: Only include fields that UserService.UpdateUserAsync modifies
                User updatedUser = new User
                {
                    Id = _originalUser.Id,
                    Username = Username.Trim(),
                    Email = Email.Trim().ToLower(),
                    // DO NOT pass PasswordHash or Role unless UpdateUserAsync is designed to handle them
                };

                bool success = await _userService.UpdateUserAsync(updatedUser);

                if (success)
                {
                    // IMPORTANT: Refresh the user in AuthService if update was successful
                    // This ensures the rest of the app sees the changes
                    _authService.CurrentUser.Username = updatedUser.Username; // Update manually
                    _authService.CurrentUser.Email = updatedUser.Email;       // Update manually
                                                                              // Or, ideally, re-fetch the user if AuthService allows:
                                                                              // await _authService.RefreshCurrentUser(); // Hypothetical method

                    // Reload local state to reflect saved data and reset _originalUser
                    LoadUserProfile();

                    await Shell.Current.DisplayAlert("Success", "Profile updated successfully.", "OK");
                }
                else
                {
                    HasError = true;
                    ErrorMessage = "Failed to update profile. Username might exist or an error occurred.";
                    await Shell.Current.DisplayAlert("Error", ErrorMessage, "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving profile: {ex.Message}");
                HasError = true;
                ErrorMessage = $"An error occurred: {ex.Message}";
                await Shell.Current.DisplayAlert("Error", ErrorMessage, "OK");
            }
            finally
            {
                IsBusy = false;
                // Re-evaluate CanSave after operation
                SaveProfileCommand.NotifyCanExecuteChanged();
            }
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            if (IsBusy) return;
            // Logout is handled by AuthService, which includes navigation
            await _authService.LogoutAsync();
        }

        // Simple email format check (consider using Regex or a library for more robust validation)
        private bool IsValidEmailFormat(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                // Use System.Net.Mail.MailAddress for basic validation
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email.Trim();
            }
            catch
            {
                return false;
            }
        }

        // Remember to implement IDisposable or similar to unsubscribe PropertyChanged
    }
}