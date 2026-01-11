using System.Threading.Tasks;

namespace Anticipack.Services
{
    /// <summary>
    /// Service for requesting and checking microphone permissions
    /// </summary>
    public interface IMicrophonePermissionService
    {
        /// <summary>
        /// Check if microphone permission is granted
        /// </summary>
        Task<bool> CheckPermissionAsync();

        /// <summary>
        /// Request microphone permission from the user
        /// </summary>
        Task<bool> RequestPermissionAsync();
    }
}
