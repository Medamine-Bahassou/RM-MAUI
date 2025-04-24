using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using newRestaurant.Models; // Needed for UserRole
using newRestaurant.Services;
using newRestaurant.Services.Interfaces;
using System.Collections.Generic; // Needed for List
using System.Linq; // Needed for Enum manipulation
using System.Threading.Tasks;
using System;

namespace newRestaurant.ViewModels
{
    public partial class RegisterViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private string _username;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private string _email;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private string _password;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private string _confirmPassword;

        [ObservableProperty] private string _errorMessage;
        [ObservableProperty] private bool _hasError;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private UserRole _selectedRole = UserRole.Customer;

        public List<UserRole> AvailableRoles { get; private set; }

        public RegisterViewModel(IAuthService authService, INavigationService navigationService)
        {
            _authService = authService;
            _navigationService = navigationService;
            Title = "Register";

            AvailableRoles = Enum.GetValues(typeof(UserRole))
                                 .Cast<UserRole>()
                                 .Where(role => role != UserRole.Admin) // Prevent self-registering as Admin
                                 .ToList();
        }

        private bool CanRegister() =>
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(Email) &&
            !string.IsNullOrWhiteSpace(Password) && Password.Length >= 6 &&
            Password == ConfirmPassword &&
            Enum.IsDefined(typeof(UserRole), SelectedRole) &&
            !IsBusy;

        [RelayCommand(CanExecute = nameof(CanRegister))]
        private async Task RegisterAsync()
        {
            IsBusy = true;
            HasError = false;
            ErrorMessage = string.Empty;

            try
            {
                // Debug logs
                System.Diagnostics.Debug.WriteLine($"Trying to register: {Username}, {Email}, {SelectedRole}");

                if (Password != ConfirmPassword)
                {
                    ErrorMessage = "Passwords do not match.";
                    HasError = true;
                    return;
                }

                if (!Enum.IsDefined(typeof(UserRole), SelectedRole))
                {
                    ErrorMessage = "Selected role is not valid.";
                    HasError = true;
                    return;
                }

                bool success = await _authService.RegisterAsync(Username, Email, Password, SelectedRole);

                if (success)
                {
                    try
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await Shell.Current.DisplayAlert("Success", "Registration successful! Please log in.", "OK");
                        });
                    }
                    catch (Exception uiEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"UI Error: {uiEx.Message}");
                    }

                    await GoBackAsync(); // Navigate to previous page
                }
                else
                {
                    ErrorMessage = "Registration failed. Username or email might already exist.";
                    HasError = true;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred: {ex.Message}";
                HasError = true;
                System.Diagnostics.Debug.WriteLine($"Registration Error: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            if (IsBusy) return;
            await _navigationService.GoBackAsync();
        }
    }
}
