using Net.Chdk.Model.Camera;
using Net.Chdk.Model.CameraModel;
using System.Collections.Generic;
using System.Linq;

namespace Net.Chdk.Providers.CameraModel
{
    sealed class CameraModelProvider : ICameraModelProvider
    {
        IEnumerable<IProductCameraModelProvider> CameraModelProviders { get; }

        public CameraModelProvider(IEnumerable<IProductCameraModelProvider> cameraModelProviders)
        {
            CameraModelProviders = cameraModelProviders;
        }

        public CameraModelInfo[] GetCameraModels(CameraInfo cameraInfo)
        {
            return CameraModelProviders
                .Select(p => p.GetCameraModels(cameraInfo))
                .FirstOrDefault(c => c != null);
        }
    }
}
