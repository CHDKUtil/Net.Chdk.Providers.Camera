using Net.Chdk.Meta.Model.Camera.Eos;
using Net.Chdk.Model.Camera;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Net.Chdk.Providers.CameraModel
{
    public abstract class EosCameraModelProvider : ProductCameraModelProvider<EosCameraData, EosCameraModelData, EosCardData, Version>
    {
        protected EosCameraModelProvider(string productName)
            : base(productName)
        {
        }

        protected override string GetRevision(CameraInfo cameraInfo, EosCameraModelData model)
        {
            var versionStr = cameraInfo.Canon.FirmwareVersion.ToString();
            VersionData version;
            return model.Versions.TryGetValue(versionStr, out version)
                ? version.Version
                : null;
        }

        protected override bool IsInvalid(CameraInfo cameraInfo)
        {
            return cameraInfo.Canon?.ModelId == null || cameraInfo.Canon?.FirmwareVersion == null;
        }

        protected override CanonInfo CreateCanonInfo(ReverseCameraData camera, Version version)
        {
            return new CanonInfo
            {
                ModelId = camera.ModelId,
                FirmwareVersion = version
            };
        }

        protected override Dictionary<string, Version> GetVersions(EosCameraModelData model)
        {
            return model.Versions.ToDictionary(
                kvp => kvp.Value.Version,
                kvp => Version.Parse(kvp.Key));
        }
    }
}
