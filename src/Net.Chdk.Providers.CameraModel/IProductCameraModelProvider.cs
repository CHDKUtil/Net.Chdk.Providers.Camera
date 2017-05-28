using Net.Chdk.Model.Camera;
using Net.Chdk.Model.CameraModel;

namespace Net.Chdk.Providers.CameraModel
{
    public interface IProductCameraModelProvider
    {
        CameraModelInfo[] GetCameraModels(CameraInfo cameraInfo);
    }
}
