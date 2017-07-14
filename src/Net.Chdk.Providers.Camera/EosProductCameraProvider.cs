using Microsoft.Extensions.Logging;
using Net.Chdk.Meta.Model.Camera.Eos;
using Net.Chdk.Model.Camera;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Net.Chdk.Providers.Camera
{
    sealed class EosProductCameraProvider : ProductCameraProvider<EosCameraData, EosCameraModelData, EosCardData, Version>
    {
        public EosProductCameraProvider(string productName, ILoggerFactory loggerFactory)
            : base(productName, loggerFactory.CreateLogger<EosProductCameraProvider>())
        {
        }

        protected override string GetRevision(CameraInfo cameraInfo, EosCameraModelData model)
        {
            var versionStr = cameraInfo.Canon.FirmwareVersion.ToString();
            return model.Versions.TryGetValue(versionStr, out VersionData version)
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
            return model.Versions.ToDictionary(GetKey, GetValue);
        }

        private static string GetKey(KeyValuePair<string, VersionData> kvp)
        {
            return kvp.Value.Version;
        }

        private static Version GetValue(KeyValuePair<string, VersionData> kvp)
        {
            return Version.Parse(kvp.Key);
        }
    }
}
