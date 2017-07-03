using Net.Chdk.Model.Camera;

namespace Net.Chdk.Providers.CameraModel
{
    public interface IProductCameraModelProvider
    {
        CameraModelsInfo GetCameraModels(CameraInfo cameraInfo);
    }
}
