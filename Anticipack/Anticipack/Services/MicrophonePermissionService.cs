using System.Threading.Tasks;

namespace Anticipack.Services
{
    /// <summary>
    /// Platform-specific implementation of microphone permission service
    /// Uses .NET MAUI's built-in Permissions API
    /// </summary>
    public class MicrophonePermissionService : IMicrophonePermissionService
    {
        public async Task<bool> CheckPermissionAsync()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            return status == PermissionStatus.Granted;
        }

        public async Task<bool> RequestPermissionAsync()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            
            if (status == PermissionStatus.Granted)
                return true;

            if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
            {
                // On iOS, if denied, we need to direct user to settings
                // We'll return false and let the calling code handle showing a message
                return false;
            }

            // Request permission
            status = await Permissions.RequestAsync<Permissions.Microphone>();
            
            return status == PermissionStatus.Granted;
        }
    }
}
