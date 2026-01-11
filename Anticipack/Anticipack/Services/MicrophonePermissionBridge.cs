using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace Anticipack.Services
{
    /// <summary>
    /// Blazor-compatible bridge for microphone permissions
    /// Can be injected into Razor components and called from JavaScript
    /// </summary>
    public class MicrophonePermissionBridge
    {
        private readonly IMicrophonePermissionService _permissionService;

        public MicrophonePermissionBridge(IMicrophonePermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        [JSInvokable("CheckMicrophonePermission")]
        public async Task<bool> CheckPermissionAsync()
        {
            return await _permissionService.CheckPermissionAsync();
        }

        [JSInvokable("RequestMicrophonePermission")]
        public async Task<bool> RequestPermissionAsync()
        {
            return await _permissionService.RequestPermissionAsync();
        }
    }
}
