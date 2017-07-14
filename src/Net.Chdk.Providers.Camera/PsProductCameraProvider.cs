using Microsoft.Extensions.Logging;
using Net.Chdk.Meta.Model.Camera.Ps;
using Net.Chdk.Model.Camera;
using Net.Chdk.Model.CameraModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Net.Chdk.Providers.Camera
{
    sealed class PsProductCameraProvider : ProductCameraProvider<PsCameraData, PsCameraModelData, PsCardData, uint>
    {
        public PsProductCameraProvider(string productName, ILoggerFactory loggerFactory)
            : base(productName, loggerFactory.CreateLogger<PsProductCameraProvider>())
        {
        }

        protected override string GetRevision(CameraInfo cameraInfo, PsCameraModelData model)
        {
            var revisionStr = $"0x{cameraInfo.Canon.FirmwareRevision:x}";
            return model.Revisions.TryGetValue(revisionStr, out RevisionData revision)
                ? revision.Revision
                : null;
        }

        protected override bool IsInvalid(CameraInfo cameraInfo)
        {
            return cameraInfo.Canon?.ModelId == null || cameraInfo.Canon?.FirmwareRevision == 0;
        }

        protected override CanonInfo CreateCanonInfo(ReverseCameraData camera, uint revision)
        {
            return new CanonInfo
            {
                ModelId = camera.ModelId,
                FirmwareRevision = revision
            };
        }

        protected override Dictionary<string, uint> GetVersions(PsCameraModelData model)
        {
            return model.Revisions.ToDictionary(GetKey, GetValue);
        }

        protected override CameraModelsInfo GetCameraModels(PsCameraData camera, CameraModelInfo[] models)
        {
            var cameraModels = base.GetCameraModels(camera, models);
            cameraModels.AltButton = camera.Alt?.Button;
            cameraModels.AltButtons = camera.Alt?.Buttons;
            return cameraModels;
        }

        private static string GetKey(KeyValuePair<string, RevisionData> kvp)
        {
            var revision = GetValue(kvp);
            return GetFirmwareRevision(revision);
        }

        private static uint GetValue(KeyValuePair<string, RevisionData> kvp)
        {
            return Convert.ToUInt32(kvp.Key, 16);
        }

        private static string GetFirmwareRevision(uint revision)
        {
            return new string(new[] {
                (char)(((revision >> 24) & 0x0f) + 0x30),
                (char)(((revision >> 20) & 0x0f) + 0x30),
                (char)(((revision >> 16) & 0x0f) + 0x30),
                (char)(((revision >>  8) & 0x7f) + 0x60)
            });
        }
    }
}
